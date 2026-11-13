namespace Orchid.Desktop.Service;

/// <summary>
/// 데스크톱 클라이언트의 Keycloak OIDC 인증 상태와 로그인·로그아웃 동작을 제공하는 계약입니다.
/// </summary>
public interface IOrchidDesktopAuthService
{
    /// <summary>
    /// 현재 사용자가 인증되었는지 여부를 가져옵니다.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 현재 로그인한 사용자의 표시 이름을 가져옵니다.
    /// </summary>
    string CurrentUserName { get; }

    /// <summary>
    /// 현재 로그인한 사용자의 이메일 주소를 가져옵니다.
    /// </summary>
    string CurrentUserEmail { get; }

    /// <summary>
    /// 현재 로그인한 사용자의 아바타 이니셜을 가져옵니다.
    /// </summary>
    string CurrentUserInitial { get; }

    /// <summary>
    /// Keycloak OIDC Authority URL을 가져옵니다.
    /// </summary>
    string AuthorityUrl { get; }

    /// <summary>
    /// 지정한 ID 공급자(google, kakao, microsoft)로 Keycloak OIDC 로그인을 수행합니다.
    /// </summary>
    /// <param name="provider">ID 공급자 식별자</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task SignInWithProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// 시스템 브라우저와 루프백 HttpListener를 사용하여 대화형 Keycloak OIDC PKCE 로그인을 수행합니다.
    /// </summary>
    /// <param name="provider">ID 공급자 식별자</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task SignInInteractiveAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// 진행 중인 대화형 로그인 대기를 취소하고 사용자를 미인증 상태로 되돌립니다.
    /// </summary>
    void CancelSignIn();

    /// <summary>
    /// 현재 인증 세션을 종료하고 로그아웃합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 지정한 ID 공급자에 대한 Keycloak OIDC 인가 요청 URI를 생성합니다.
    /// </summary>
    /// <param name="provider">ID 공급자 식별자</param>
    /// <param name="redirectUri">루프백 콜백 URI</param>
    /// <returns>Keycloak OIDC 인가 URI</returns>
    Uri BuildAuthorizationUrl(string provider, string redirectUri);
}
