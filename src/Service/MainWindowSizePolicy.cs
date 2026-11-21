namespace Orchid.Desktop.Service;

/// <summary>
/// 창 너비와 높이(DIU)를 나타냅니다.
/// </summary>
/// <param name="Width">너비(DIU).</param>
/// <param name="Height">높이(DIU).</param>
public readonly record struct WindowSize(double Width, double Height);

/// <summary>
/// 저장된 창 크기를 최소 크기와 표시 영역에 맞게 보정하는 순수 정책입니다.
/// </summary>
public static class MainWindowSizePolicy
{
    /// <summary>
    /// 저장값(없으면 기본 크기)을 최소 크기 이상, 표시 영역 상한 이하로 보정합니다.
    /// </summary>
    /// <param name="saved">마지막으로 저장한 크기. 없으면 <see langword="null"/>.</param>
    /// <param name="display">표시 영역 크기(DIU).</param>
    /// <returns>창에 적용할 크기.</returns>
    public static WindowSize Resolve(WindowSize? saved, WindowSize display)
    {
        WindowSize requested = saved ?? new WindowSize(MainWindowLayout.Width, MainWindowLayout.Height);
        double maximumWidth = Math.Max(MainWindowLayout.MinimumWidth, display.Width * MainWindowLayout.MaximumDisplayRatio);
        double maximumHeight = Math.Max(MainWindowLayout.MinimumHeight, display.Height * MainWindowLayout.MaximumDisplayRatio);

        return new WindowSize(
            Math.Clamp(requested.Width, MainWindowLayout.MinimumWidth, maximumWidth),
            Math.Clamp(requested.Height, MainWindowLayout.MinimumHeight, maximumHeight));
    }

    /// <summary>
    /// 저장해도 되는 크기인지 판단합니다. 유한하고 최소 크기 이상이어야 합니다.
    /// </summary>
    /// <param name="size">검사할 크기.</param>
    /// <returns>저장 가능하면 <see langword="true"/>.</returns>
    public static bool IsPersistable(WindowSize size)
    {
        return double.IsFinite(size.Width)
            && double.IsFinite(size.Height)
            && size.Width >= MainWindowLayout.MinimumWidth
            && size.Height >= MainWindowLayout.MinimumHeight;
    }
}
