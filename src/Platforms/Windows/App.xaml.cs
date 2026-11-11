namespace Orchid.Desktop.WinUI;

/// <summary>
/// Windows 환경의 WinUI 네이티브 애플리케이션 수명주기 진입점입니다.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// <see cref="App"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }
}
