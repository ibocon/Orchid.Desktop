namespace Orchid.Desktop.Service;

/// <summary>
/// 창이 최대화·최소화되지 않은 복원 상태인지 판별합니다.
/// </summary>
public static class WindowRestoreState
{
    /// <summary>
    /// 창이 복원 상태이면 <see langword="true"/>를 반환합니다. Windows가 아닌 플랫폼은 항상 <see langword="true"/>입니다.
    /// </summary>
    /// <param name="window">검사할 MAUI 창.</param>
    /// <returns>복원 상태 여부.</returns>
    public static bool IsRestored(Window window)
    {
#if WINDOWS
        if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window native
            && native.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            return presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Restored;
        }
#endif
        return true;
    }
}
