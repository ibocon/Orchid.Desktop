using FluentAssertions;
using Orchid.Desktop.Service;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Orchid.Desktop.UnitTests;

public sealed class DesktopStartupLogTest
{
    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.RenderTree.Renderer")]
    [InlineData("Microsoft.Extensions.Localization.ResourceManagerStringLocalizer")]
    public void GivenFrameworkDebugEventWhenLoggingPolicyIsAppliedThenItIsNotPersisted(string sourceContext)
    {
        // Given
        CollectingSink sink = new();
        using Logger logger = DesktopStartupLog
            .Configure(new LoggerConfiguration())
            .WriteTo.Sink(sink)
            .CreateLogger();

        // When
        logger
            .ForContext("SourceContext", sourceContext)
            .Debug("framework diagnostic");

        // Then
        sink.Events.Should().BeEmpty();
    }

    [Fact]
    public void GivenLoggingPolicyWhenResolvedThenItUsesBoundedAppJsonRetention()
    {
        // Given / When
        long fileSizeLimitBytes = DesktopStartupLog.AppJsonFileSizeLimitBytes;
        int retainedFileCountLimit = DesktopStartupLog.AppJsonRetainedFileCountLimit;

        // Then
        fileSizeLimitBytes.Should().Be(10 * 1024 * 1024);
        retainedFileCountLimit.Should().Be(14);
    }

    [Fact]
    public void GivenDefaultDirectoryWhenResolvedThenItUsesLocalApplicationData()
    {
        // Given / When
        string directory = DesktopStartupLog.DefaultLogDirectory;

        // Then
        directory.Should().Be(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BotanicSoft",
            "Orchid",
            "logs"));
    }

    [Fact]
    public void GivenStartupFailureWhenRecordedThenPhaseAndExceptionTypeArePersistedWithoutExceptionMessage()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Orchid-DesktopStartupLog-" + Guid.NewGuid().ToString("N"));
        const string secret = "secret-value-must-not-be-logged";
        try
        {
            // Given
            DesktopStartupLog.Initialize(directory);

            // When
            DesktopStartupLog.RecordFatal("maui-app-xaml-initialize", new InvalidOperationException(secret));
            DesktopStartupLog.Flush();
            string log = File.ReadAllText(Directory.GetFiles(directory, "startup-*.log", SearchOption.TopDirectoryOnly).Single());

            // Then
            log.Should().Contain("maui-app-xaml-initialize");
            log.Should().Contain(typeof(InvalidOperationException).FullName);
            log.Should().NotContain(secret);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void GivenInitializedStartupLogWhenPhaseRecordedThenCompactJsonAppLogIncludesProcessIdAndMessageTemplate()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Orchid-DesktopAppLog-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Given
            DesktopStartupLog.Initialize(directory);

            // When
            DesktopStartupLog.RecordPhase("maui-app-built");
            DesktopStartupLog.Flush();
            string jsonLogPath = Directory.GetFiles(directory, "app*.log", SearchOption.TopDirectoryOnly).Single();
            string jsonContent = File.ReadAllText(jsonLogPath);

            // Then
            jsonContent.Should().Contain("\"ProcessId\":");
            jsonContent.Should().Contain("maui-app-built");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
