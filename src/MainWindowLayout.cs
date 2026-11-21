namespace Orchid.Desktop;

/// <summary>
/// Oxygen.Desktop의 MainWindowLyt 규격을 따르는 데스크톱 메인 창 초기·최소 크기 상수입니다.
/// </summary>
public static class MainWindowLayout
{
    /// <summary>
    /// 메인 창의 초기 너비(DIU)입니다.
    /// </summary>
    public const double Width = 780;

    /// <summary>
    /// 메인 창의 초기 높이(DIU)입니다. 로그인 화면이 스크롤 없이 표시되는 높이입니다.
    /// </summary>
    public const double Height = 720;

    /// <summary>
    /// 메인 창의 최소 너비(DIU)입니다.
    /// </summary>
    public const double MinimumWidth = 780;

    /// <summary>
    /// 메인 창의 최소 높이(DIU)입니다.
    /// </summary>
    public const double MinimumHeight = 585;

    /// <summary>
    /// 창 크기가 차지할 수 있는 표시 영역 비율의 상한입니다. 작업 표시줄과 타이틀바 여유를 확보합니다.
    /// </summary>
    public const double MaximumDisplayRatio = 0.9;
}
