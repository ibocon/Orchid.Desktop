using Orchid.Desktop.Service;

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
        DesktopStartupLog.Initialize();
        DesktopStartupLog.RecordPhase("windows-app-constructed");
        try
        {
            InitializeComponent();
        }
        catch (Exception exception)
        {
            DesktopStartupLog.RecordFatal("windows-xaml-initialize", exception);
            throw;
        }
    }

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
    {
        DesktopStartupLog.RecordPhase("windows-create-maui-app");
        try
        {
            return MauiProgram.CreateMauiApp();
        }
        catch (Exception exception)
        {
            DesktopStartupLog.RecordFatal("windows-create-maui-app", exception);
            throw;
        }
    }
}
