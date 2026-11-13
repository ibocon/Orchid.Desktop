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
        loginPanel.Should().Contain("<GoogleLoginButton");
        loginPanel.Should().Contain("<KakaoLoginButton");
        loginPanel.Should().Contain("<MicrosoftLoginButton");
    }

    [Fact]
    public void LoginPanelAssetsShouldFollowPeonyMobileBrandLayoutContract()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string logoPath = Path.Combine(repoRoot, "src/wwwroot/logo-login.svg");
        string loginPanelPath = Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor");
        string loginPanelCssPath = Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor.css");

        // When & Then
        // ✅ logo-login.svg 브랜드 심볼이 존재하고 유효한 SVG 마크업을 포함한다
        File.Exists(logoPath).Should().BeTrue(because: "로그인 브랜드 로고 SVG가 존재해야 한다.");
        File.ReadAllText(logoPath).Should().Contain("<svg");

        // ✅ LoginPanel.razor는 Peony.Mobile과 동일한 브랜드 레이아웃 구조를 따른다
        string loginPanel = File.ReadAllText(loginPanelPath);
        loginPanel.Should().Contain("class=\"root\"");
        loginPanel.Should().Contain("class=\"login-brand\"");
        loginPanel.Should().Contain("logo-login.svg");
        loginPanel.Should().Contain("class=\"login-wordmark\"");
        loginPanel.Should().Contain("모두클립");
        loginPanel.Should().Contain("class=\"login-cta\"");
        loginPanel.Should().Contain("<GoogleLoginButton");
        loginPanel.Should().Contain("<KakaoLoginButton");
        loginPanel.Should().Contain("<MicrosoftLoginButton");
        loginPanel.Should().Contain("class=\"login-progress\"");
        loginPanel.Should().Contain("IStringLocalizer<LoginPanelResource>");

        // ✅ LoginPanel.razor.css가 존재하고 Garden 디자인 토큰을 사용한다
        File.Exists(loginPanelCssPath).Should().BeTrue(because: "LoginPanel 스코프드 CSS가 존재해야 한다.");
        string loginPanelCss = File.ReadAllText(loginPanelCssPath);
        loginPanelCss.Should().Contain("--gdn-sys-color-on-surface");
        loginPanelCss.Should().Contain("--gdn-sys-color-on-surface-variant");
        loginPanelCss.Should().Contain("--gdn-sys-spacing-");
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

        // ✅ 네이티브 배경과 전역 CSS가 다크 HEX 하드코딩 없이 디자인 토큰을 사용한다
        mainPageXaml.Should().NotContain("#121212");
        mainPageXaml.Should().NotContain("#1e1e1e");
        appCss.Should().NotContain("#121212");
        appCss.Should().NotContain("#1e1e1e");
        appCss.Should().Contain("var(--gdn-sys-color-surface)");
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
