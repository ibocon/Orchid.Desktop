using FluentAssertions;
using Orchid.Desktop;
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Oxygen.Desktop의 MainWindowLytTests를 참고하여 Orchid.Desktop 메인 창의 초기·최소 크기 계약을 검증합니다.
/// </summary>
public sealed class DesktopMainWindowLayoutContractTest
{
    [Fact]
    public void GivenMainWindowLayoutWhenInspectingConstantsThenInitialHeightFitsLoginAndMinimumIsPreserved()
    {
        // Given
        const double expectedWidth = 780;
        const double expectedInitialHeight = 720;
        const double expectedMinimumHeight = 585;

        // When & Then
        MainWindowLayout.Width.Should().Be(expectedWidth);
        MainWindowLayout.Height.Should().Be(expectedInitialHeight);
        MainWindowLayout.MinimumWidth.Should().Be(expectedWidth);
        MainWindowLayout.MinimumHeight.Should().Be(expectedMinimumHeight);
    }

    [Fact]
    public void GivenAppCreateWindowWhenInspectingSourceThenRestoresAndPersistsWindowSize()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string appXamlCs = File.ReadAllText(Path.Combine(repoRoot, "src/App.xaml.cs"));

        // When & Then
        appXamlCs.Should().Contain("MainWindowSizePolicy.Resolve(PreferencesWindowSizeStore.Load()");
        appXamlCs.Should().Contain("PreferencesWindowSizeStore.Save(size)");
        appXamlCs.Should().Contain("WindowRestoreState.IsRestored(window)");
        appXamlCs.Should().Contain("window.Deactivated");
        appXamlCs.Should().Contain("window.Destroying");
        appXamlCs.Should().Contain("MinimumWidth = MainWindowLayout.MinimumWidth");
        appXamlCs.Should().Contain("MinimumHeight = MainWindowLayout.MinimumHeight");
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
