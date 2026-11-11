namespace Orchid.Desktop;

/// <summary>
/// .NET MAUI 애플리케이션 진입 클래스입니다.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// <see cref="App"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage())
        {
            Title = "Orchid - Universal E2EE Clipboard"
        };
    }
}
