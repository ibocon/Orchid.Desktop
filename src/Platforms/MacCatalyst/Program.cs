#if MACCATALYST
using UIKit;

namespace Orchid.Desktop;

/// <summary>
/// MacCatalyst 애플리케이션의 메인 진입점입니다.
/// </summary>
public static class Program
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
#endif
