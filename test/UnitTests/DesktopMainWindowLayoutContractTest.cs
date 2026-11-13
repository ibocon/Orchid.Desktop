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
    public void GivenMainWindowLayoutWhenInspectingConstantsThenMatchesDesktopBaseline()
    {
        // Given
        const double expectedWidth = 780;
        const double expectedHeight = 585;

        // When & Then
        MainWindowLayout.Width.Should().Be(expectedWidth);
        MainWindowLayout.Height.Should().Be(expectedHeight);
        MainWindowLayout.MinimumWidth.Should().Be(expectedWidth);
        MainWindowLayout.MinimumHeight.Should().Be(expectedHeight);
    }

    [Fact]
    public void GivenAppCreateWindowWhenInspectingSourceThenAppliesMainWindowLayoutBounds()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string appXamlCs = File.ReadAllText(Path.Combine(repoRoot, "src/App.xaml.cs"));

        // When & Then
        appXamlCs.Should().Contain("double windowWidth = MainWindowLayout.Width");
        appXamlCs.Should().Contain("double windowHeight = MainWindowLayout.Height");
        appXamlCs.Should().Contain("Width = windowWidth");
        appXamlCs.Should().Contain("Height = windowHeight");
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
