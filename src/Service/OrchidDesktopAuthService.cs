using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Garden.Share;
using Microsoft.Extensions.Options;

namespace Orchid.Desktop.Service;

/// <summary>
/// Oxy.Desktop 및 Peony.Mobile의 Garden.Maui 인증 구성을 참고하여 데스크톱 Keycloak OIDC 루프백 로그인 상태를 관리하는 서비스입니다.
/// </summary>
public sealed partial class OrchidDesktopAuthService : IOrchidDesktopAuthService
{
    private const string DefaultKeycloakBaseAddress = "https://stag.botanicsoft.com/keycloak";
    private const string DefaultRealm = "garden-staging";
    private const string DefaultAuthorityUrl = "https://stag.botanicsoft.com/keycloak/realms/garden-staging";
    private const string BotanicSoftAuthorityUrl = "https://stag.botanicsoft.com/keycloak/realms/botanicsoft";
    private const string DefaultClientId = "peony-mobile";
    private const string DefaultValidAudience = "peony-api-gateway";
    private const string BotanicSoftLoopbackClientId = "grafana-provisioner";
    private const string DefaultScope = "openid profile email";
    private const string BotanicSoftScope = "openid";
    private const int GardenStagingLoopbackPort = 43717;
    private const int BotanicSoftLoopbackPort = 8251;
    private const string CallbackPath = "/callback";
    private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan WindowMonitorPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan WindowReactivationGraceDelay = TimeSpan.FromSeconds(1.2);

    private readonly IdentityOptions _identityOptions;
    private readonly Func<Uri, Process?> _browserLauncher;
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly Lock _activeSignInLock = new();
    private CancellationTokenSource? _activeSignInCts;
    private TcpListener? _activeListener;

    /// <summary>
    /// 기본 <see cref="IdentityOptions"/>와 시스템 브라우저 실행기를 사용하여 <see cref="OrchidDesktopAuthService"/>를 초기화합니다.
    /// </summary>
    public OrchidDesktopAuthService()
        : this(null, null)
    {
    }

    /// <summary>
    /// DI 컨테이너에서 주입된 <see cref="IOptions{IdentityOptions}"/>를 사용하여 <see cref="OrchidDesktopAuthService"/>를 초기화합니다.
    /// </summary>
    /// <param name="identityOptions">Peony.Mobile 및 Garden.Share 규약의 Identity 설정 옵션</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public OrchidDesktopAuthService(IOptions<IdentityOptions> identityOptions)
        : this(identityOptions, null)
    {
    }

    /// <summary>
    /// 지정한 브라우저 실행기로 <see cref="OrchidDesktopAuthService"/>를 초기화합니다.
    /// </summary>
    /// <param name="browserLauncher">인가 URI를 받아 시스템 브라우저를 여는 실행기 (null이면 기본 실행기 사용)</param>
    public OrchidDesktopAuthService(Func<Uri, Process?>? browserLauncher)
        : this(null, browserLauncher)
    {
    }

    /// <summary>
    /// 지정한 <see cref="IOptions{IdentityOptions}"/>와 브라우저 실행기로 <see cref="OrchidDesktopAuthService"/>를 초기화합니다.
    /// </summary>
    /// <param name="identityOptions">Peony.Mobile 및 Garden.Share 규약의 Identity 설정 옵션</param>
    /// <param name="browserLauncher">인가 URI를 받아 시스템 브라우저를 여는 실행기 (null이면 기본 실행기 사용)</param>
    public OrchidDesktopAuthService(
        IOptions<IdentityOptions>? identityOptions,
        Func<Uri, Process?>? browserLauncher = null)
        : this(identityOptions, browserLauncher, null)
    {
    }

    internal OrchidDesktopAuthService(
        IOptions<IdentityOptions>? identityOptions,
        Func<Uri, Process?>? browserLauncher,
        Func<HttpClient>? httpClientFactory)
    {
        _identityOptions = identityOptions?.Value ?? CreateDefaultIdentityOptions();
        _browserLauncher = browserLauncher ?? OpenSystemBrowser;
        _httpClientFactory = httpClientFactory ?? CreateHttpClient;
    }

    /// <inheritdoc />
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc />
    public string CurrentUserName { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string CurrentUserEmail { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string CurrentUserInitial =>
        string.IsNullOrWhiteSpace(CurrentUserName)
            ? "O"
            : CurrentUserName[..1].ToUpperInvariant();

    /// <inheritdoc />
    public string AuthorityUrl => ResolveDefaultAuthorityUrl(_identityOptions);

    internal static IdentityOptions CreateDefaultIdentityOptions()
    {
        return new IdentityOptions
        {
            BaseAddress = new Uri(DefaultKeycloakBaseAddress),
            Realm = DefaultRealm,
            ClientId = DefaultClientId,
            RedirectUrl = new Uri($"http://127.0.0.1:{GardenStagingLoopbackPort}{CallbackPath}"),
            ValidAudience = DefaultValidAudience,
        };
    }

    /// <inheritdoc />
    public void CancelSignIn()
    {
        CancellationTokenSource? cts;
        TcpListener? listener;
        lock (_activeSignInLock)
        {
            cts = _activeSignInCts;
            listener = _activeListener;
        }

        StopListener(listener);
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    /// <inheritdoc />
    public Task SignInWithProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedProvider = provider.Trim().ToLowerInvariant();
        CurrentUserName = normalizedProvider switch
        {
            IdentityProvider.Google => "BotanicSoft Google User",
            "kakao" => "BotanicSoft Kakao User",
            IdentityProvider.Microsoft => "BotanicSoft Microsoft User",
            _ => $"BotanicSoft {normalizedProvider} User"
        };
        CurrentUserEmail = $"{normalizedProvider}@botanicsoft.com";
        IsAuthenticated = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SignInInteractiveAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        cancellationToken.ThrowIfCancellationRequested();

        CancelSignIn();

        OidcProviderEndpoint endpoint = ResolveProviderEndpoint(provider);
        string redirectUri = endpoint.RedirectUri;
        (string codeVerifier, string codeChallenge) = CreatePkcePair();
        Uri authorizeUri = BuildAuthorizationUrl(provider, redirectUri, codeChallenge);

        TcpListener? listener = await TryStartLoopbackListenerAsync(endpoint.LoopbackPort, cancellationToken).ConfigureAwait(false);
        if (listener is null)
        {
            return;
        }

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(SignInTimeout);
        using CancellationTokenSource monitorCts = new();
        lock (_activeSignInLock)
        {
            _activeSignInCts = linkedCts;
            _activeListener = listener;
        }

        Process? browserProcess = null;
        try
        {
            browserProcess = _browserLauncher(authorizeUri);
            Task monitorTask = MonitorWindowReactivationAsync(monitorCts.Token);
            Task<OidcLoopbackCallbackResult?> callbackTask = WaitForActiveLoopbackCallbackAsync(linkedCts.Token);

            OidcLoopbackCallbackResult? callbackResult;
            try
            {
                if (await Task.WhenAny(callbackTask, monitorTask).ConfigureAwait(false) != callbackTask)
                {
                    // 사용자가 브라우저를 닫고 앱 창으로 되돌아온 경우: 진행 중인 루프백 리다이렉트를 유예 시간만큼 마저 기다린다.
                    Task graceTask = Task.Delay(WindowReactivationGraceDelay, linkedCts.Token);
                    if (await Task.WhenAny(callbackTask, graceTask).ConfigureAwait(false) != callbackTask)
                    {
                        return;
                    }
                }

                callbackResult = await callbackTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested || _activeSignInCts?.IsCancellationRequested == true)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            monitorCts.Cancel();

            if (callbackResult is null ||
                !string.IsNullOrWhiteSpace(callbackResult.Error) ||
                string.IsNullOrWhiteSpace(callbackResult.Code))
            {
                return;
            }

            await ExchangeCodeOrFallbackAsync(
                    endpoint,
                    callbackResult.Code,
                    redirectUri,
                    codeVerifier,
                    linkedCts.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            lock (_activeSignInLock)
            {
                if (ReferenceEquals(_activeSignInCts, linkedCts))
                {
                    _activeSignInCts = null;
                }

                if (ReferenceEquals(_activeListener, listener))
                {
                    _activeListener = null;
                }
            }

            monitorCts.Cancel();
            StopListener(listener);
            browserProcess?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClearAuthentication();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Uri BuildAuthorizationUrl(string provider, string redirectUri)
    {
        (_, string codeChallenge) = CreatePkcePair();
        return BuildAuthorizationUrl(provider, redirectUri, codeChallenge);
    }

    /// <summary>
    /// 공급자별로 Keycloak에 등록된 고정 루프백 리다이렉트 URI를 사용하여 인가 URL을 생성합니다.
    /// </summary>
    /// <param name="provider">로그인 공급자 식별자 (google, kakao, microsoft)</param>
    /// <returns>Keycloak OIDC 인가 엔드포인트 URI</returns>
    public Uri BuildAuthorizationUrl(string provider)
    {
        OidcProviderEndpoint endpoint = ResolveProviderEndpoint(provider);
        return BuildAuthorizationUrl(provider, endpoint.RedirectUri);
    }

    private static string ResolveDefaultAuthorityUrl(IdentityOptions identityOptions)
    {
        string baseUrl = identityOptions.BaseAddress?.AbsoluteUri.TrimEnd('/') ?? DefaultKeycloakBaseAddress;
        string realm = string.IsNullOrWhiteSpace(identityOptions.Realm) ? DefaultRealm : identityOptions.Realm.Trim('/');
        if (baseUrl.EndsWith($"/realms/{realm}", StringComparison.OrdinalIgnoreCase))
        {
            return baseUrl;
        }

        return $"{baseUrl}/realms/{realm}";
    }

    private OidcProviderEndpoint ResolveProviderEndpoint(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        string normalizedProvider = provider.Trim().ToLowerInvariant();
        if (string.Equals(normalizedProvider, IdentityProvider.Microsoft, StringComparison.Ordinal))
        {
            return new OidcProviderEndpoint(
                BotanicSoftAuthorityUrl,
                BotanicSoftLoopbackClientId,
                BotanicSoftScope,
                BotanicSoftLoopbackPort,
                $"http://127.0.0.1:{BotanicSoftLoopbackPort}{CallbackPath}");
        }

        string authorityUrl = ResolveDefaultAuthorityUrl(_identityOptions);
        string clientId = string.IsNullOrWhiteSpace(_identityOptions.ClientId)
            ? DefaultClientId
            : _identityOptions.ClientId;
        Uri configuredRedirect = _identityOptions.RedirectUrl is { IsAbsoluteUri: true } uri &&
                                 string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            ? uri
            : new Uri($"http://127.0.0.1:{GardenStagingLoopbackPort}{CallbackPath}");
        int loopbackPort = configuredRedirect.Port > 0 ? configuredRedirect.Port : GardenStagingLoopbackPort;
        string callbackPath = string.IsNullOrWhiteSpace(configuredRedirect.AbsolutePath) || configuredRedirect.AbsolutePath == "/"
            ? CallbackPath
            : configuredRedirect.AbsolutePath.TrimEnd('/');
        string redirectUri = $"{configuredRedirect.Scheme}://{configuredRedirect.Host}:{loopbackPort}{callbackPath}";

        return new OidcProviderEndpoint(
            authorityUrl,
            clientId,
            DefaultScope,
            loopbackPort,
            redirectUri);
    }

    private static async Task<TcpListener?> TryStartLoopbackListenerAsync(int port, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            TcpListener listener = new(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                return listener;
            }
            catch (SocketException) when (attempt == 0)
            {
                listener.Stop();
                await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                listener.Stop();
                return null;
            }
        }

        return null;
    }

    private async Task<OidcLoopbackCallbackResult?> WaitForActiveLoopbackCallbackAsync(
        CancellationToken cancellationToken)
    {
        TcpListener? listener;
        lock (_activeSignInLock)
        {
            listener = _activeListener;
        }

        if (listener is null)
        {
            return null;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            client.ReceiveTimeout = 5000;
            client.SendTimeout = 5000;

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            string? requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            await DrainHttpHeadersAsync(reader, cancellationToken).ConfigureAwait(false);
            if (!TryParseHttpRequestTarget(requestLine, out string requestPath, out Dictionary<string, string> queryParams) ||
                !string.Equals(requestPath, CallbackPath, StringComparison.Ordinal))
            {
                await WriteHttpResponseAsync(
                        client,
                        stream,
                        statusCode: 404,
                        reasonPhrase: "Not Found",
                        bodyHtml: "<!doctype html><meta charset=\"utf-8\"><title>Not Found</title><p>Not Found</p>",
                        cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            queryParams.TryGetValue("code", out string? code);
            queryParams.TryGetValue("error", out string? error);

            string html = string.IsNullOrWhiteSpace(error)
                ? "<html><head><meta charset=\"utf-8\"/></head><body style=\"font-family:sans-serif;text-align:center;padding:48px;\"><h2>모두클립 로그인이 완료되었습니다.</h2><p>이 브라우저 탭을 닫고 앱으로 돌아가세요.</p></body></html>"
                : "<html><head><meta charset=\"utf-8\"/></head><body style=\"font-family:sans-serif;text-align:center;padding:48px;\"><h2>로그인이 취소되었거나 실패했습니다.</h2><p>이 브라우저 탭을 닫고 앱에서 다시 시도하세요.</p></body></html>";

            await WriteHttpResponseAsync(
                    client,
                    stream,
                    statusCode: 200,
                    reasonPhrase: "OK",
                    bodyHtml: html,
                    cancellationToken)
                .ConfigureAwait(false);

            return new OidcLoopbackCallbackResult(code, error);
        }

        return null;
    }

    private static async Task DrainHttpHeadersAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                string? headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(headerLine))
                {
                    break;
                }
            }
        }
        catch (IOException)
        {
        }
    }

    private static bool TryParseHttpRequestTarget(
        string? requestLine,
        out string path,
        out Dictionary<string, string> queryParams)
    {
        path = string.Empty;
        queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return false;
        }

        string[] parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        string target = parts[1];
        int queryIndex = target.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            path = target;
            return true;
        }

        path = target[..queryIndex];
        string query = target[(queryIndex + 1)..];
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eqIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (eqIndex <= 0)
            {
                continue;
            }

            string key = Uri.UnescapeDataString(pair[..eqIndex].Replace('+', ' '));
            string value = Uri.UnescapeDataString(pair[(eqIndex + 1)..].Replace('+', ' '));
            queryParams[key] = value;
        }

        return true;
    }

    private static async Task WriteHttpResponseAsync(
        TcpClient client,
        Stream stream,
        int statusCode,
        string reasonPhrase,
        string bodyHtml,
        CancellationToken cancellationToken)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyHtml);
        byte[] headerBytes = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} {reasonPhrase}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            client.Client.Shutdown(SocketShutdown.Send);
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task MonitorWindowReactivationAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            bool lostForeground = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                bool isForegroundOwnedByApp = IsForegroundWindowOwnedByCurrentProcess();
                if (isForegroundOwnedByApp && lostForeground)
                {
                    return;
                }

                lostForeground = !isForegroundOwnedByApp;
                await Task.Delay(WindowMonitorPollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsForegroundWindowOwnedByCurrentProcess()
    {
        nint foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == nint.Zero)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(foregroundWindow, out uint processId);
        return processId == Environment.ProcessId;
    }

    private static void StopListener(TcpListener? listener)
    {
        if (listener is null)
        {
            return;
        }

        try
        {
            listener.Stop();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private static Process? OpenSystemBrowser(Uri authorizeUri)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = authorizeUri.AbsoluteUri,
            UseShellExecute = true
        });
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    private Uri BuildAuthorizationUrl(string provider, string redirectUri, string codeChallenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        OidcProviderEndpoint endpoint = ResolveProviderEndpoint(provider);
        string escapedRedirect = Uri.EscapeDataString(redirectUri);
        string escapedProvider = Uri.EscapeDataString(provider.Trim().ToLowerInvariant());
        string escapedChallenge = Uri.EscapeDataString(codeChallenge);
        string escapedScope = Uri.EscapeDataString(endpoint.Scope);
        return new Uri(
            $"{endpoint.AuthorityUrl}/protocol/openid-connect/auth?client_id={endpoint.ClientId}&response_type=code&scope={escapedScope}&kc_idp_hint={escapedProvider}&code_challenge={escapedChallenge}&code_challenge_method=S256&redirect_uri={escapedRedirect}");
    }

    private async Task ExchangeCodeOrFallbackAsync(
        OidcProviderEndpoint endpoint,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient httpClient = _httpClientFactory();
            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = endpoint.ClientId,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            });
            using HttpResponseMessage response = await httpClient
                .PostAsync(new Uri($"{endpoint.AuthorityUrl}/protocol/openid-connect/token"), form, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (TryExtractProfileFromTokenResponse(doc.RootElement, out string? name, out string? email))
                {
                    CurrentUserName = name!;
                    CurrentUserEmail = email ?? string.Empty;
                    IsAuthenticated = true;
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or FormatException)
        {
            ClearAuthentication();
            return;
        }

        ClearAuthentication();
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    private void ClearAuthentication()
    {
        IsAuthenticated = false;
        CurrentUserName = string.Empty;
        CurrentUserEmail = string.Empty;
    }

    private static bool TryExtractProfileFromTokenResponse(
        JsonElement tokenRoot,
        out string? name,
        out string? email)
    {
        name = null;
        email = null;
        if (tokenRoot.TryGetProperty("id_token", out JsonElement idTokenEl) &&
            TryExtractJwtProfile(idTokenEl.GetString(), out name, out email))
        {
            return true;
        }

        if (tokenRoot.TryGetProperty("access_token", out JsonElement accessTokenEl) &&
            TryExtractJwtProfile(accessTokenEl.GetString(), out name, out email))
        {
            return true;
        }

        return false;
    }

    private static bool TryExtractJwtProfile(string? jwt, out string? name, out string? email)
    {
        name = null;
        email = null;
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return false;
        }

        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };

        byte[] bytes = Convert.FromBase64String(payload);
        using JsonDocument claims = JsonDocument.Parse(bytes);
        email = claims.RootElement.TryGetProperty("email", out JsonElement emailEl) ? emailEl.GetString() : null;
        name = claims.RootElement.TryGetProperty("name", out JsonElement nameEl)
            ? nameEl.GetString()
            : claims.RootElement.TryGetProperty("preferred_username", out JsonElement prefEl)
                ? prefEl.GetString()
                : email;
        return !string.IsNullOrWhiteSpace(name);
    }

    private static (string Verifier, string Challenge) CreatePkcePair()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string verifier = Base64UrlEncode(bytes);
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        string challenge = Base64UrlEncode(hash);
        return (verifier, challenge);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record OidcProviderEndpoint(
        string AuthorityUrl,
        string ClientId,
        string Scope,
        int LoopbackPort,
        string RedirectUri);

    private sealed record OidcLoopbackCallbackResult(
        string? Code,
        string? Error);
}
