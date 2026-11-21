using MudBlazor.Services;
using Garden.Razor;
using Microsoft.Extensions.Logging;
using Orchid.Core.Abstraction;
using Orchid.Core.Cryptography;
using Orchid.Core.Storage;
using System.Reflection;
using Orchid.Core.Sync;
using Orchid.Core.Sync.Relay;
using Orchid.Desktop.Service;
using Serilog;

namespace Orchid.Desktop;

/// <summary>
/// .NET MAUI 데스크톱 애플리케이션 진입점 및 의존성 주입을 구성합니다.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// MAUI 앱 호스트를 빌드하고 초기화합니다.
    /// </summary>
    /// <returns>구성된 <see cref="MauiApp"/> 인스턴스</returns>
    public static MauiApp CreateMauiApp()
    {
        DesktopStartupLog.Initialize();
        DesktopStartupLog.RecordPhase("maui-builder-create");
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddGardenRazor();
        builder.Services.AddDesignSystem();
        builder.ConfigureLocalization();
        builder.ConfigureIdentity();
        builder.Logging
            .AddFilter(
                "Microsoft.AspNetCore.Components.RenderTree.Renderer",
                LogLevel.Warning)
            .AddFilter(
                "Microsoft.Extensions.Localization.ResourceManagerStringLocalizer",
                LogLevel.Warning)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddSerilog(Log.Logger, dispose: false);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddSingleton<ISystemClipboard, DesktopClipboardAdapter>();
        builder.Services.AddSingleton<IAesGcmCryptoService, AesGcmCryptoService>();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "orchid.db");
        builder.Services.AddSingleton(_ => new ObservableClipboardRepository(new SqliteClipboardRepository(dbPath)));
        builder.Services.AddSingleton<IClipboardRepository>(sp => sp.GetRequiredService<ObservableClipboardRepository>());
        builder.Services.AddSingleton<ISyncEngine, SyncEngine>();

        string relayUrl = typeof(MauiProgram).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "OrchidRelayUrl").Value!;
        builder.Services.AddSingleton(new RelayOptions(new Uri(new Uri(relayUrl.TrimEnd('/') + "/"), "hubs/clipboard")));
        builder.Services.AddSingleton<IClipboardRelayClient, SignalRClipboardRelayClient>();
#if WINDOWS
        builder.Services.AddSingleton<IClipboardChangeSource, Orchid.Desktop.Platforms.Windows.WindowsClipboardChangeSource>();
#else
        builder.Services.AddSingleton<IClipboardChangeSource, NullClipboardChangeSource>();
#endif
        builder.Services.AddSingleton<ClipboardSyncCoordinator>();

        MauiApp app = builder.Build();
        DesktopStartupLog.RecordPhase("maui-app-built");
        return app;
    }
}
