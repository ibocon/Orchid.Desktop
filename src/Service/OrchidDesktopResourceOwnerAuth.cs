using System.Security.Claims;
using Garden.Share;

namespace Orchid.Desktop.Service;

/// <summary>데스크톱 인증 서비스를 Garden 공통 로그인 계약으로 감쌉니다.</summary>
public sealed class OrchidDesktopResourceOwnerAuth(IOrchidDesktopAuthService authService) : IResourceOwnerAuth
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    public ClaimsPrincipal ClaimsPrincipal => authService.IsAuthenticated ? new(new ClaimsIdentity([new(ClaimTypes.Name, authService.CurrentUserName), new(ClaimTypes.Email, authService.CurrentUserEmail)], "Orchid")) : Anonymous;
    public Task<bool> IsLoggedInAsync()
    {
        return Task.FromResult(authService.IsAuthenticated);
    }
    public async Task<ClaimsPrincipal> LoginAsync(string identityProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityProvider);
        try
        {
            await authService.SignInInteractiveAsync(identityProvider).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Anonymous;
        }

        return ClaimsPrincipal;
    }
    public Task<bool> TryRefreshAsync()
    {
        return Task.FromResult(false);
    }
    public async Task LogoutAsync()
    {
        authService.CancelSignIn();
        if (authService.IsAuthenticated)
        {
            await authService.SignOutAsync().ConfigureAwait(false);
        }
    }
    public AuthenticationProfile GetProfile()
    {
        return !authService.IsAuthenticated ? AuthenticationProfile.Anonymous : new() { Email = authService.CurrentUserEmail, Name = authService.CurrentUserName, Avatar = authService.CurrentUserInitial };
    }
}
