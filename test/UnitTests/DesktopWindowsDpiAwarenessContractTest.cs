// keep: FluentAssertions assertions
using FluentAssertions;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop Windows 실행 앱의 Per-Monitor V2 DPI 인식 매니페스트 선언 및 프로젝트 참조 계약을 검증합니다.
/// </summary>
public sealed class DesktopWindowsDpiAwarenessContractTest
{
    /// <summary>
    /// Windows 애플리케이션 매니페스트가 PerMonitorV2와 레거시 true/PM 폴백을 모두 선언하는지 검증합니다.
    /// </summary>
    [Fact]
    public void GivenWindowsManifestWhenInspectingDpiSettingsThenDeclaresPerMonitorV2()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string manifestPath = Path.Combine(repoRoot, "src/Platforms/Windows/app.manifest");

        // When
        bool exists = File.Exists(manifestPath);
        string content = exists ? File.ReadAllText(manifestPath) : string.Empty;

        // Then
        // ✅ PerMonitorV2와 true/PM 폴백 및 Windows 10/11 supportedOS GUID가 모두 선언되어야 한다
        exists.Should().BeTrue();
        content.Should().Contain(">PerMonitorV2</dpiAwareness>");
        content.Should().Contain(">true/PM</dpiAware>");
        content.Should().Contain("{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}");
    }

    /// <summary>
    /// Orchid.Desktop 프로젝트 파일이 Windows 타깃에서 Platforms\Windows\app.manifest를 명시적으로 참조하는지 검증합니다.
    /// </summary>
    [Fact]
    public void GivenOrchidDesktopProjectWhenInspectingWindowsTargetThenReferencesApplicationManifest()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string csprojPath = Path.Combine(repoRoot, "src/Orchid.Desktop.csproj");

        // When
        string content = File.ReadAllText(csprojPath);

        // Then
        // ✅ Windows TFM 조건 아래 ApplicationManifest로 Platforms\Windows\app.manifest를 지정해야 한다
        content.Should().Contain("Condition=\"$(TargetFramework.Contains('-windows'))\"");
        content.Should().Contain("<ApplicationManifest>Platforms\\Windows\\app.manifest</ApplicationManifest>");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Orchid.Desktop.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Orchid.Desktop.slnx not found.");
    }
}
