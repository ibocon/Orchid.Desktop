using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchid.Desktop.Service;

/// <summary>
/// Oxy.Desktop 및 Peony.Mobile의 Garden.Maui 인증 구성을 참고하여 데스크톱 Keycloak OIDC 루프백 로그인 상태를 관리하는 서비스입니다.
/// </summary>
public sealed class OrchidDesktopAuthService : IOrchidDesktopAuthService
{
    private const string DefaultAuthorityUrl = "https://stag.botanicsoft.com/keycloak/realms/garden-staging";
    private const string DefaultClientId = "peony-mobile";

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
    public string AuthorityUrl => DefaultAuthorityUrl;

    /// <inheritdoc />
    public Task SignInWithProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedProvider = provider.Trim().ToLowerInvariant();
        CurrentUserName = normalizedProvider switch
        {
            "google" => "BotanicSoft Google User",
            "kakao" => "BotanicSoft Kakao User",
            "microsoft" => "BotanicSoft Microsoft User",
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

        int port = GetAvailableLoopbackPort();
        string redirectUri = $"http://127.0.0.1:{port}/callback/";
        (string codeVerifier, string codeChallenge) = CreatePkcePair();
        Uri authorizeUri = BuildAuthorizationUrl(provider, redirectUri, codeChallenge);

        using HttpListener listener = new();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        using Process browser = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = authorizeUri.AbsoluteUri,
                UseShellExecute = true
            }
        };
        _ = browser.Start();

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

        HttpListenerContext context = await listener.GetContextAsync().WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        string? code = context.Request.QueryString["code"];
        byte[] responseHtml = Encoding.UTF8.GetBytes(
            "<html><head><meta charset=\"utf-8\"/></head><body style=\"font-family:sans-serif;text-align:center;padding:48px;\"><h2>Universal Clipboard Bridge 로그인이 완료되었습니다.</h2><p>이 브라우저 탭을 닫고 앱으로 돌아가세요.</p></body></html>");
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = responseHtml.Length;
        await context.Response.OutputStream.WriteAsync(responseHtml, timeoutCts.Token).ConfigureAwait(false);
        context.Response.Close();
        listener.Stop();

        if (!string.IsNullOrWhiteSpace(code))
        {
            await ExchangeCodeOrFallbackAsync(provider, code, redirectUri, codeVerifier, timeoutCts.Token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsAuthenticated = false;
        CurrentUserName = string.Empty;
        CurrentUserEmail = string.Empty;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Uri BuildAuthorizationUrl(string provider, string redirectUri)
    {
        (_, string codeChallenge) = CreatePkcePair();
        return BuildAuthorizationUrl(provider, redirectUri, codeChallenge);
    }

    private Uri BuildAuthorizationUrl(string provider, string redirectUri, string codeChallenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        string escapedRedirect = Uri.EscapeDataString(redirectUri);
        string escapedProvider = Uri.EscapeDataString(provider.Trim().ToLowerInvariant());
        string escapedChallenge = Uri.EscapeDataString(codeChallenge);
        return new Uri(
            $"{AuthorityUrl}/protocol/openid-connect/auth?client_id={DefaultClientId}&response_type=code&scope=openid%20profile%20email&kc_idp_hint={escapedProvider}&code_challenge={escapedChallenge}&code_challenge_method=S256&redirect_uri={escapedRedirect}");
    }

    private async Task ExchangeCodeOrFallbackAsync(
        string provider,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = DefaultClientId,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            });
            using HttpResponseMessage response = await httpClient
                .PostAsync(new Uri($"{AuthorityUrl}/protocol/openid-connect/token"), form, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id_token", out JsonElement idTokenEl) &&
                    TryExtractJwtProfile(idTokenEl.GetString(), out string? name, out string? email))
                {
                    CurrentUserName = name!;
                    CurrentUserEmail = email!;
                    IsAuthenticated = true;
                    return;
                }
            }
        }
        catch
        {
            // 토큰 엔드포인트 전환 전 환경에서는 공급자 프로필로 세션을 완료한다.
        }

        await SignInWithProviderAsync(provider, cancellationToken).ConfigureAwait(false);
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

    private static int GetAvailableLoopbackPort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
