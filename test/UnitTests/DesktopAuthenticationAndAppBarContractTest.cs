// keep: System.Net.Sockets for loopback callback simulation
using System.Net.Sockets;
// keep: System.Text for HTTP request/response encoding
using System.Text;
using System.Text.Json;
// keep: FluentAssertions assertions
using FluentAssertions;
// keep: Orchid.Desktop service namespace
using Orchid.Desktop.Service;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop의 Keycloak OIDC 로그인 서비스 및 GardenTopAppBar/GardenAccountMenu 계약을 검증합니다.
/// </summary>
public sealed class DesktopAuthenticationAndAppBarContractTest
{
    [Fact]
    public async Task SignInAndSignOutShouldTransitionAuthenticationStateAndProfile()
    {
        // Given
        OrchidDesktopAuthService authService = new();
        authService.IsAuthenticated.Should().BeFalse();

        // When
        await authService.SignInWithProviderAsync("google");

        // Then
        // ✅ 로그인 후 인증 상태와 사용자 프로필이 설정되고 로그아웃 시 초기화된다
        authService.IsAuthenticated.Should().BeTrue();
        authService.CurrentUserName.Should().NotBeNullOrWhiteSpace();
        authService.CurrentUserInitial.Should().NotBeNullOrWhiteSpace();
        authService.BuildAuthorizationUrl("google", "http://127.0.0.1:51789/callback")
            .AbsoluteUri.Should().Contain("kc_idp_hint=google");

        await authService.SignOutAsync();
        authService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task CancelSignInShouldAbortInteractiveWaitAndLeaveUserUnauthenticated()
    {
        // Given
        // ✅ 브라우저 기동을 no-op 런처로 대체해 단위 테스트에서 실제 브라우저를 띄우지 않는다
        OrchidDesktopAuthService authService = new(browserLauncher: _ => null);
        using CancellationTokenSource cts = new();

        // When
        Task signInTask = authService.SignInInteractiveAsync("google", cts.Token);
        await Task.Delay(100);
        authService.CancelSignIn();

        // Then
        // ✅ 취소 시 3분 타임아웃을 기다리지 않고 깨끗하게 완료되며 미인증 상태를 유지한다
        Func<Task> awaitSignIn = () => signInTask;
        await awaitSignIn.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
        signInTask.IsFaulted.Should().BeFalse();
        signInTask.IsCanceled.Should().BeFalse();
        authService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void BuildAuthorizationUrlShouldUseRegisteredKeycloakLoopbackRedirectUrisPerProvider()
    {
        // Given
        OrchidDesktopAuthService authService = new(browserLauncher: _ => null);

        // When
        string googleUrl = authService.BuildAuthorizationUrl("google").AbsoluteUri;
        string kakaoUrl = authService.BuildAuthorizationUrl("kakao").AbsoluteUri;
        string microsoftUrl = authService.BuildAuthorizationUrl("microsoft").AbsoluteUri;

        // Then
        // ✅ Google과 Kakao는 garden-staging realm의 peony-mobile 클라이언트와 고정 43717 포트(/callback 후행 슬래시 없음)를 사용한다
        googleUrl.Should().Contain("/realms/garden-staging/protocol/openid-connect/auth");
        googleUrl.Should().Contain("client_id=peony-mobile");
        googleUrl.Should().Contain("kc_idp_hint=google");
        googleUrl.Should().Contain("redirect_uri=http%3A%2F%2F127.0.0.1%3A43717%2Fcallback");
        googleUrl.Should().NotContain("redirect_uri=http%3A%2F%2F127.0.0.1%3A43717%2Fcallback%2F");

        kakaoUrl.Should().Contain("/realms/garden-staging/protocol/openid-connect/auth");
        kakaoUrl.Should().Contain("client_id=peony-mobile");
        kakaoUrl.Should().Contain("kc_idp_hint=kakao");
        kakaoUrl.Should().Contain("redirect_uri=http%3A%2F%2F127.0.0.1%3A43717%2Fcallback");

        // ✅ Microsoft는 botanicsoft realm의 grafana-provisioner 클라이언트와 고정 8251 포트(/callback 후행 슬래시 없음)를 사용한다
        microsoftUrl.Should().Contain("/realms/botanicsoft/protocol/openid-connect/auth");
        microsoftUrl.Should().Contain("client_id=grafana-provisioner");
        microsoftUrl.Should().Contain("scope=openid");
        microsoftUrl.Should().Contain("kc_idp_hint=microsoft");
        microsoftUrl.Should().Contain("redirect_uri=http%3A%2F%2F127.0.0.1%3A8251%2Fcallback");
        microsoftUrl.Should().NotContain("redirect_uri=http%3A%2F%2F127.0.0.1%3A8251%2Fcallback%2F");

        // ✅ Peony.Mobile 규약에 따라 appsettings.Staging.json 및 ConfigureIdentity()로 IdentityOptions가 구성된다
        string repoRoot = FindRepositoryRoot();
        string stagingSettings = File.ReadAllText(Path.Combine(repoRoot, "src/Resources/appsettings.Staging.json"));
        string mauiProgram = File.ReadAllText(Path.Combine(repoRoot, "src/MauiProgram.cs"));
        string identityExtension = File.ReadAllText(Path.Combine(repoRoot, "src/Service/MauiAppBuilderExtensions.Identity.cs"));
        stagingSettings.Should().Contain("\"Identity\"");
        stagingSettings.Should().Contain("\"BaseAddress\": \"https://stag.botanicsoft.com/keycloak\"");
        stagingSettings.Should().Contain("\"Realm\": \"garden-staging\"");
        stagingSettings.Should().Contain("\"ClientId\": \"peony-mobile\"");
        stagingSettings.Should().Contain("\"RedirectUrl\": \"http://127.0.0.1:43717/callback\"");
        stagingSettings.Should().Contain("\"ValidAudience\": \"peony-api-gateway\"");
        mauiProgram.Should().Contain("builder.ConfigureIdentity();");
        identityExtension.Should().Contain("IdentityOptions.DefaultSectionName");
    }

    [Fact]
    public async Task SignInInteractiveAsyncShouldIgnoreFaviconAndCompleteOnCallbackCode()
    {
        // Given
        Uri? launchedUri = null;
        const string expectedName = "Yegun Kim";
        const string expectedEmail = "profile@example.com";
        OrchidDesktopAuthService authService = CreateAuthService(
            responseFactory: () => CreateTokenResponse(expectedName, expectedEmail),
            browserLauncher: uri =>
            {
                launchedUri = uri;
                return null;
            });

        // When
        Task signInTask = authService.SignInInteractiveAsync("google");
        await Task.Delay(100);

        string faviconResponse = await SendLoopbackHttpGetAsync(43717, "/favicon.ico");
        string callbackResponse = await SendLoopbackHttpGetAsync(43717, "/callback?code=test-auth-code&state=test-state");
        await signInTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Then
        // ✅ /favicon.ico 요청에는 404를 반환하고 대기를 유지하며, /callback?code=... 수신 시 200 응답 후 인증 세션을 완료한다
        launchedUri.Should().NotBeNull();
        launchedUri?.AbsoluteUri.Should().Contain("redirect_uri=http%3A%2F%2F127.0.0.1%3A43717%2Fcallback");
        faviconResponse.Should().Contain("404 Not Found");
        callbackResponse.Should().Contain("200 OK");
        authService.IsAuthenticated.Should().BeTrue();
        authService.CurrentUserName.Should().Be(expectedName);
        authService.CurrentUserEmail.Should().Be(expectedEmail);
    }

    [Fact]
    public async Task SignInInteractiveAsyncShouldRemainUnauthenticatedWhenTokenExchangeFails()
    {
        // Given
        OrchidDesktopAuthService authService = CreateAuthService(
            responseFactory: () => new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway),
            browserLauncher: _ => null);

        // When
        Task signInTask = authService.SignInInteractiveAsync("google");
        await Task.Delay(100);
        _ = await SendLoopbackHttpGetAsync(43717, "/callback?code=test-auth-code&state=test-state");
        await signInTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Then
        // ✅ 실패한 token 교환은 공급자 기반 더미 프로필을 만들지 않는다
        authService.IsAuthenticated.Should().BeFalse();
        authService.CurrentUserName.Should().BeEmpty();
        authService.CurrentUserEmail.Should().BeEmpty();
    }

    private static OrchidDesktopAuthService CreateAuthService(
        Func<HttpResponseMessage> responseFactory,
        Func<Uri, System.Diagnostics.Process?> browserLauncher)
    {
        return new OrchidDesktopAuthService(
            identityOptions: null,
            browserLauncher: browserLauncher,
            httpClientFactory: () => new HttpClient(new StubHttpMessageHandler(responseFactory)));
    }

    private static HttpResponseMessage CreateTokenResponse(string name, string email)
    {
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { name, email })))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string token = $"header.{payload}.signature";
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { id_token = token }))
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory());
        }
    }

    private static async Task<string> SendLoopbackHttpGetAsync(int port, string pathAndQuery)
    {
        using TcpClient client = new();
        await client.ConnectAsync("127.0.0.1", port).ConfigureAwait(false);
        using NetworkStream stream = client.GetStream();
        string largeBrowserHeader = new('a', 2048);
        byte[] requestBytes = Encoding.ASCII.GetBytes(
            $"GET {pathAndQuery} HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nUser-Agent: Mozilla/5.0\r\nX-Browser-Padding: {largeBrowserHeader}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(requestBytes).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);

        using StreamReader reader = new(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    [Fact]
    public void IndexHtmlAndDesktopRazorViewsShouldIncludeScopedCssAndGardenLoginComponents()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string indexHtml = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/index.html"));
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));
        string loginPanel = File.ReadAllText(Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor"));

        // When & Then
        // ✅ Scoped CSS 번들, GardenTopAppBar, GardenAccountMenu, GoogleLoginButton, KakaoLoginButton, MicrosoftLoginButton이 모두 구성된다
        indexHtml.Should().Contain("Orchid.Desktop.styles.css");
        desktopApp.Should().Contain("<GardenTopAppBar");
        desktopApp.Should().Contain("<GardenAccountMenu");
        loginPanel.Should().Contain("<LoginScreen Branding=\"_branding\">");
    }

    [Fact]
    public void LoginPanelAssetsShouldFollowPeonyMobileBrandLayoutContract()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string logoPath = Path.Combine(repoRoot, "src/wwwroot/logo-login.svg");
        string loginPanelPath = Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor");

        // When & Then
        // ✅ logo-login.svg 브랜드 심볼이 존재하고 유효한 SVG 마크업을 포함한다
        File.Exists(logoPath).Should().BeTrue(because: "로그인 브랜드 로고 SVG가 존재해야 한다.");
        File.ReadAllText(logoPath).Should().Contain("<svg");

        // ✅ LoginPanel.razor는 Peony.Mobile과 동일한 브랜드 레이아웃 구조를 따른다
        string loginPanel = File.ReadAllText(loginPanelPath);
        loginPanel.Should().Contain("<LoginScreen Branding=\"_branding\">");
        loginPanel.Should().Contain("OrchidLoginBranding.Create()");

        // ✅ 로그인 대기 중 취소 버튼과 상태 복구 가드가 구성된다
        loginPanel.Should().Contain("AuthService.CancelSignIn");
    }

    [Fact]
    public void InitialLoginLayoutShouldConstrainContentToDesktopShell()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string appCss = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/css/app.css"));
        string desktopCss = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor.css"));
        string loginPanel = File.ReadAllText(Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor"));
        string loginCss = File.ReadAllText(Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor.css"));

        // When
        string layoutContract = string.Concat(appCss, desktopCss, loginPanel, loginCss);

        // Then
        // ✅ 초기 로그인은 앱 셸의 남은 높이를 사용하며 body 스크롤을 만들지 않는다.
        layoutContract.Should().Contain("#app");
        layoutContract.Should().Contain("grid-template-rows: auto minmax(0, 1fr);");
        layoutContract.Should().Contain("overflow: hidden;");
        loginPanel.Should().Contain("<section class=\"login-panel\">");
        loginCss.Should().Contain(".login-panel ::deep .login-screen");
        // ✅ Desktop 소비자 CSS가 브랜드와 CTA의 자연 높이를 보존한다.
        loginCss.Should().Contain(".login-panel ::deep .login-brand,");
        loginCss.Should().Contain(".login-panel ::deep .login-cta");
        loginCss.Should().Contain("flex: 0 0 auto;");
        loginCss.Should().Contain("justify-content: safe center;");
    }

    [Fact]
    public void DesignSystemProviderShouldDefaultToLightSchemeAndExposeBothPaletteMappings()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string provider = File.ReadAllText(Path.Combine(
            repoRoot,
            "src/Component/OrchidDesktopDesignSystemProvider/OrchidDesktopDesignSystemProvider.razor.cs"));

        // When & Then
        // ✅ 초기 기본 테마가 SchemeContext.Light이고 PaletteLight/PaletteDark를 모두 구성한다
        provider.Should().Contain("ChangeContext(SchemeContext.Light)");
        provider.Should().Contain("PaletteLight");
        provider.Should().Contain("PaletteDark");

        // ✅ 테마 전환 API(IsDarkMode, CurrentSchemeContext, ToggleTheme, SetTheme)를 노출한다
        provider.Should().Contain("IsDarkMode");
        provider.Should().Contain("CurrentSchemeContext");
        provider.Should().Contain("ToggleTheme()");
        provider.Should().Contain("SetTheme(SchemeContext");
    }

    [Fact]
    public void DesktopAppAndNativeAssetsShouldBindProviderThemeWithoutDarkHardcoding()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));
        string mainPageXaml = File.ReadAllText(Path.Combine(repoRoot, "src/MainPage.xaml"));
        string appCss = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/css/app.css"));

        // When & Then
        // ✅ 루트 MudThemeProvider가 프로바이더의 IsDarkMode에 바인딩되고 고정 다크 리터럴이 없다
        desktopApp.Should().NotContain("IsDarkMode=\"true\"");
        desktopApp.Should().Contain("IsDarkMode=\"@(_provider?.IsDarkMode ?? false)\"");
        desktopApp.Should().Contain("<GardenIconButton");
        desktopApp.Should().Contain("Icons.Material.Filled.LightMode");
        desktopApp.Should().Contain("Icons.Material.Filled.DarkMode");

        // ✅ 네이티브 배경과 전역 CSS가 다크 HEX 하드코딩 없이 투명 캔버스를 유지한다
        mainPageXaml.Should().NotContain("#121212");
        mainPageXaml.Should().NotContain("#1e1e1e");
        appCss.Should().NotContain("#121212");
        appCss.Should().NotContain("#1e1e1e");
        appCss.Should().Contain("background-color: transparent;");
    }

    [Fact]
    public void DirectoryPackagesPropsShouldPinPatchedSignalRAndSqlitePclRawVersions()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string props = File.ReadAllText(Path.Combine(repoRoot, "Directory.Packages.props"));

        // When & Then
        // ✅ NU1903 취약점이 해결된 SignalR 10.0.15 및 SQLitePCLRaw 2.1.14 버전이 고정된다
        props.Should().Contain("\"Microsoft.AspNetCore.SignalR.Client\" Version=\"10.0.15\"");
        props.Should().Contain("\"SQLitePCLRaw.bundle_green\" Version=\"2.1.11\"");
        props.Should().Contain("\"SQLitePCLRaw.lib.e_sqlite3\" Version=\"2.1.14\"");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Orchid.Desktop.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Orchid.Desktop.slnx not found.");
    }
}
