using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Orchid.Desktop.Service;

/// <summary>
/// MAUI 의존성 주입이 준비되기 전후의 데스크톱 시작 수명주기를 파일에 기록합니다.
/// </summary>
internal static class DesktopStartupLog
{
    private const string StartupLogFileName = "startup-.log";
    private const string AppJsonLogFileName = "app.log";
    internal const long AppJsonFileSizeLimitBytes = 10 * 1024 * 1024;
    internal const int AppJsonRetainedFileCountLimit = 14;
    private static readonly Lock Sync = new();

    /// <summary>
    /// 기본 구조화 로그 디렉터리를 가져옵니다.
    /// </summary>
    internal static string DefaultLogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BotanicSoft",
        "Orchid",
        "logs");

    /// <summary>
    /// 파일 기반 bootstrap logger를 초기화합니다.
    /// </summary>
    /// <param name="logDirectory">테스트 또는 진단을 위한 선택 로그 디렉터리입니다.</param>
    [SuppressMessage(
        "Design",
        "CA1031:Consider catching more specific exceptions",
        Justification = "Bootstrap logging must never replace the original application startup failure.")]
    internal static void Initialize(string? logDirectory = null)
    {
        string directory = logDirectory ?? DefaultLogDirectory;
        try
        {
            Directory.CreateDirectory(directory);
            lock (Sync)
            {
                Log.CloseAndFlush();
                Log.Logger = Configure(new LoggerConfiguration())
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                    .WriteTo.File(
                        Path.Combine(directory, StartupLogFileName),
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        formatProvider: CultureInfo.InvariantCulture,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        shared: true)
                    .WriteTo.File(
                        new CompactJsonFormatter(),
                        Path.Combine(directory, AppJsonLogFileName),
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: AppJsonFileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: AppJsonRetainedFileCountLimit,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromMilliseconds(250))
                    .CreateLogger();
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Orchid startup logging initialization failed: {exception.GetType().FullName}");
        }
    }

    /// <summary>
    /// 프레임워크 진단 로그를 제한하는 Serilog 기본 구성을 적용합니다.
    /// </summary>
    /// <param name="loggerConfiguration">적용할 logger 구성입니다.</param>
    /// <returns>로그 수준 정책이 적용된 구성입니다.</returns>
    internal static LoggerConfiguration Configure(LoggerConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override(
                "Microsoft.AspNetCore.Components.RenderTree.Renderer",
                LogEventLevel.Warning)
            .MinimumLevel.Override(
                "Microsoft.Extensions.Localization.ResourceManagerStringLocalizer",
                LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
    }

    /// <summary>
    /// 시작 수명주기 단계를 기록합니다.
    /// </summary>
    /// <param name="phase">완료된 시작 단계입니다.</param>
    internal static void RecordPhase(string phase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        Log.Information("Orchid startup phase {StartupPhase}", phase);
    }

    /// <summary>
    /// 시작 실패 단계와 예외 형식만 기록합니다.
    /// </summary>
    /// <param name="phase">실패한 시작 단계입니다.</param>
    /// <param name="exception">원인이 된 예외입니다.</param>
    internal static void RecordFatal(string phase, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        ArgumentNullException.ThrowIfNull(exception);
        Log.Error(
            "Orchid startup failed during {StartupPhase}; exception type {ExceptionType}",
            phase,
            exception.GetType().FullName);
    }

    /// <summary>
    /// 보류 중인 로그를 디스크로 flush합니다.
    /// </summary>
    internal static void Flush()
    {
        Log.CloseAndFlush();
    }
}
