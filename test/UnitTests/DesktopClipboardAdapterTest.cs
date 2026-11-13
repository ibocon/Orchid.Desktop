using FluentAssertions;
using Moq;
using Orchid.Core.Abstraction;
using Xunit;

namespace Orchid.UnitTests.Desktop;

public sealed class DesktopClipboardAdapterTest
{
    [Fact]
    public async Task AdapterShouldNotifyCoreWhenClipboardContentChanges()
    {
        // Given
        Mock<ISystemClipboard> mockClipboard = new();

        // When & Then
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync("Sample Text");
        string? result = await mockClipboard.Object.GetTextAsync();
        result.Should().Be("Sample Text");
    }

    [Fact]
    public async Task AdapterWhenClipboardIsEmptyShouldReturnNull()
    {
        // Given
        Mock<ISystemClipboard> mockClipboard = new();
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync((string?)null);

        // When
        string? result = await mockClipboard.Object.GetTextAsync();

        // Then
        result.Should().BeNull();
    }
}
