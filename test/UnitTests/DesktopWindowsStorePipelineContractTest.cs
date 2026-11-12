using FluentAssertions;
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Windows Store MSIX 패키징 설정 및 GitHub Actions 배포 워크플로우 계약을 검증하는 단위 테스트입니다.
/// </summary>
public sealed class DesktopWindowsStorePipelineContractTest
{
    [Fact]
    public void CsprojManifestAndWorkflowShouldSupportMsixWindowsStorePublish()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string csproj = File.ReadAllText(Path.Combine(repoRoot, "src/Orchid.Desktop.csproj"));
        string manifest = File.ReadAllText(Path.Combine(repoRoot, "src/Platforms/Windows/Package.appxmanifest"));
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github/workflows/publish-desktop-windows-store.yml"));

        // When & Then
        csproj.Should().Contain("WindowsPackageType Condition=\"'$(WindowsPackageType)' == ''\"");
        csproj.Should().Contain("<ApplicationId>BotanicSoft.UniversalClipboardBridge</ApplicationId>");
        manifest.Should().Contain("Name=\"BotanicSoft.UniversalClipboardBridge\"");
        manifest.Should().Contain("Publisher=\"CN=97E323EE-349D-4541-B8DF-CEDEFF6EE455\"");
        manifest.Should().Contain("runFullTrust");
        workflow.Should().Contain("-p:WindowsPackageType=MSIX");
        workflow.Should().Contain("msstore publish");
        workflow.Should().Contain("https://stag.botanicsoft.com/orchid");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Orchid.Desktop.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Orchid.Desktop.slnx not found.");
        }

        return dir.FullName;
    }
}
