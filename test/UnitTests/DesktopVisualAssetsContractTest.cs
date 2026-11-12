// keep: BinaryPrimitives for PNG IHDR parsing
using System.Buffers.Binary;
// keep: FluentAssertions assertions
using FluentAssertions;
// keep: xUnit Fact and Theory attributes
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop의 앱 아이콘(SVG/ICO) 및 Windows Store MSIX 시각 자산 규격을 검증하는 단위 테스트입니다.
/// </summary>
public sealed class DesktopVisualAssetsContractTest
{
    [Theory]
    [InlineData("src/Assets/StoreLogo.png", 50, 50)]
    [InlineData("src/Assets/Square44x44Logo.png", 44, 44)]
    [InlineData("src/Assets/Square150x150Logo.png", 150, 150)]
    [InlineData("src/Assets/Wide310x150Logo.png", 310, 150)]
    [InlineData("docs/release/windows-store/store-logo-300x300.png", 300, 300)]
    [InlineData("docs/release/windows-store/screenshots/desktop-main.png", 1366, 768)]
    public void PngAssetsShouldExistWithExactRequiredDimensions(string relativePath, int expectedWidth, int expectedHeight)
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string fullPath = Path.Combine(repoRoot, relativePath);

        // When
        File.Exists(fullPath).Should().BeTrue($"asset {relativePath} must exist");
        byte[] bytes = File.ReadAllBytes(fullPath);
        int width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));

        // Then
        // ✅ PNG IHDR 헤더의 가로·세로 픽셀 크기가 스토어 매니페스트 규격과 정확히 일치한다
        width.Should().Be(expectedWidth);
        height.Should().Be(expectedHeight);
    }

    [Fact]
    public void SvgAndIcoAppIconAssetsShouldBeConfiguredInCsproj()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string csproj = File.ReadAllText(Path.Combine(repoRoot, "src/Orchid.Desktop.csproj"));

        // When & Then
        // ✅ SVG 원본과 .ico 파일이 존재하고 csproj에 MauiIcon 및 ApplicationIcon으로 연결된다
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.svg")).Should().BeTrue();
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appiconfg.svg")).Should().BeTrue();
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.ico")).Should().BeTrue();
        csproj.Should().Contain("MauiIcon Include=\"Resources\\AppIcon\\appicon.svg\"");
        csproj.Should().Contain("<ApplicationIcon>Resources\\AppIcon\\appicon.ico</ApplicationIcon>");
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
