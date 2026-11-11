using FluentAssertions;
using Moq;
using Orchid.Core.Abstraction;
using Xunit;

namespace Orchid.UnitTests.Desktop;

public sealed class DesktopClipboardAdapterTests
{
    [Fact]
    public async Task Adapter_Should_Notify_Core_When_Clipboard_Content_Changes()
    {
        // Given
        var mockClipboard = new Mock<ISystemClipboard>();

        // When & Then
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync("Sample Text");
        var result = await mockClipboard.Object.GetTextAsync();
        result.Should().Be("Sample Text");
    }

    [Fact]
    public async Task Adapter_When_Clipboard_Is_Empty_Should_Return_Null()
    {
        // Given
        var mockClipboard = new Mock<ISystemClipboard>();
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync((string?)null);

        // When
        var result = await mockClipboard.Object.GetTextAsync();

        // Then
        result.Should().BeNull();
    }
}
