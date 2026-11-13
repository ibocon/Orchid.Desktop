namespace Orchid.Desktop.Service;

/// <summary>
/// Oxy.Desktop 및 Peony.Mobile의 Garden.Maui 인증 구성을 참고하여 데스크톱 Keycloak OIDC 로그인 상태를 관리하는 서비스입니다.
/// </summary>
public sealed class OrchidDesktopAuthService : IOrchidDesktopAuthService
{
    private const string DefaultAuthorityUrl = "https://stag.botanicsoft.com/keycloak/realms/garden-staging";
    private const string DefaultClientId = "orchid-desktop";

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
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        string escapedRedirect = Uri.EscapeDataString(redirectUri);
        string escapedProvider = Uri.EscapeDataString(provider.Trim().ToLowerInvariant());
        return new Uri(
            $"{AuthorityUrl}/protocol/openid-connect/auth?client_id={DefaultClientId}&response_type=code&scope=openid%20profile%20email&kc_idp_hint={escapedProvider}&redirect_uri={escapedRedirect}");
    }
}
