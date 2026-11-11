#if MACCATALYST
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Orchid.Desktop;

/// <summary>
/// MacCatalyst 애플리케이션 수명주기 델리게이트입니다.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
#endif
