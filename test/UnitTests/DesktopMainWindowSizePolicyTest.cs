using FluentAssertions;
using Orchid.Desktop;
using Orchid.Desktop.Service;
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// 메인 창 크기 보정 정책과 저장 가능 판정을 검증합니다.
/// </summary>
public sealed class DesktopMainWindowSizePolicyTest
{
    private static readonly WindowSize LargeDisplay = new(1920, 1080);

    [Fact]
    public void GivenNoSavedSizeWhenResolvingThenReturnsDefaultSize()
    {
        // Given
        WindowSize? saved = null;

        // When
        WindowSize actual = MainWindowSizePolicy.Resolve(saved, LargeDisplay);

        // Then
        actual.Should().Be(new WindowSize(MainWindowLayout.Width, MainWindowLayout.Height));
    }

    [Fact]
    public void GivenSavedSizeWithinBoundsWhenResolvingThenReturnsSavedSize()
    {
        // Given
        WindowSize saved = new(1000, 800);

        // When
        WindowSize actual = MainWindowSizePolicy.Resolve(saved, LargeDisplay);

        // Then
        actual.Should().Be(saved);
    }

    [Fact]
    public void GivenSavedSizeSmallerThanMinimumWhenResolvingThenReturnsMinimumSize()
    {
        // Given
        WindowSize saved = new(100, 100);

        // When
        WindowSize actual = MainWindowSizePolicy.Resolve(saved, LargeDisplay);

        // Then
        actual.Should().Be(new WindowSize(MainWindowLayout.MinimumWidth, MainWindowLayout.MinimumHeight));
    }

    [Fact]
    public void GivenSavedSizeLargerThanDisplayWhenResolvingThenClampsToDisplayRatio()
    {
        // Given
        WindowSize saved = new(5000, 5000);
        WindowSize display = new(1366, 768);

        // When
        WindowSize actual = MainWindowSizePolicy.Resolve(saved, display);

        // Then
        actual.Width.Should().BeApproximately(1366 * MainWindowLayout.MaximumDisplayRatio, 0.001);
        actual.Height.Should().BeApproximately(768 * MainWindowLayout.MaximumDisplayRatio, 0.001);
    }

    [Fact]
    public void GivenDisplaySmallerThanMinimumWhenResolvingThenNeverGoesBelowMinimum()
    {
        // Given
        WindowSize display = new(400, 300);

        // When
        WindowSize actual = MainWindowSizePolicy.Resolve(saved: null, display);

        // Then
        actual.Width.Should().BeGreaterThanOrEqualTo(MainWindowLayout.MinimumWidth);
        actual.Height.Should().BeGreaterThanOrEqualTo(MainWindowLayout.MinimumHeight);
    }

    [Theory]
    [InlineData(double.NaN, 700)]
    [InlineData(800, double.PositiveInfinity)]
    [InlineData(779, 700)]
    [InlineData(800, 584)]
    public void GivenInvalidSizeWhenCheckingPersistableThenReturnsFalse(double width, double height)
    {
        // Given
        WindowSize size = new(width, height);

        // When
        bool actual = MainWindowSizePolicy.IsPersistable(size);

        // Then
        actual.Should().BeFalse();
    }

    [Fact]
    public void GivenMinimumSizeWhenCheckingPersistableThenReturnsTrue()
    {
        // Given
        WindowSize size = new(MainWindowLayout.MinimumWidth, MainWindowLayout.MinimumHeight);

        // When
        bool actual = MainWindowSizePolicy.IsPersistable(size);

        // Then
        actual.Should().BeTrue();
    }
}
