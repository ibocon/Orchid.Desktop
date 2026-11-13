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
