// keep: BinaryPrimitives for PNG IHDR parsing
using System.Buffers.Binary;
// keep: FluentAssertions assertions
using FluentAssertions;
// keep: xUnit Fact and Theory attributes
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop의 보태니컬 브랜드 로고(SVG/ICO) 디자인 토큰 정합성 및 Windows Store MSIX 시각 자산 규격을 검증하는 단위 테스트입니다.
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
    public void SvgAndIcoAppIconAssetsShouldBeConfiguredWithBotanicCreamColorInCsproj()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string csproj = File.ReadAllText(Path.Combine(repoRoot, "src/Orchid.Desktop.csproj"));
        string appIconSvg = File.ReadAllText(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.svg"));

        // When & Then
        // ✅ SVG 원본과 .ico 파일이 존재하고 csproj에 BotanicSoft/Peony 공통 크림 배경(#F1E4CA)으로 연결된다
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.svg")).Should().BeTrue();
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appiconfg.svg")).Should().BeTrue();
        File.Exists(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.ico")).Should().BeTrue();
        appIconSvg.Should().Contain("#F1E4CA");
        appIconSvg.Should().NotContain("linearGradient");
        csproj.Should().Contain("MauiIcon Include=\"Resources\\AppIcon\\appicon.svg\"");
        csproj.Should().Contain("Color=\"#F1E4CA\"");
        csproj.Should().NotContain("#2E1065");
        csproj.Should().Contain("<ApplicationIcon>Resources\\AppIcon\\appicon.ico</ApplicationIcon>");
    }

    [Theory]
    [InlineData("src/Resources/AppIcon/appiconfg.svg")]
    [InlineData("src/wwwroot/logo-login.svg")]
    public void BotanicalOrchidForegroundAndLoginLogoShouldUseFamilyTokensAndTransparentBackground(string relativePath)
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string svg = File.ReadAllText(Path.Combine(repoRoot, relativePath));

        // When & Then
        // ✅ Peony.Mobile 공통 잉크(#6D5443)·잎(#78D978) 토큰과 카틀레야/호접란 5화피·프릴 설판·화통 토큰(#EADDFF, #B69DF8, #9A82DB, #6750A4, #F6C667, #FFE2E4)을 사용하며 불투명 배경이나 레거시 그라데이션이 없다
        svg.Should().Contain("#6D5443");
        svg.Should().Contain("#78D978");
        svg.Should().Contain("#EADDFF");
        svg.Should().Contain("#B69DF8");
        svg.Should().Contain("#9A82DB");
        svg.Should().Contain("#6750A4");
        svg.Should().Contain("#F6C667");
        svg.Should().Contain("#FFE2E4");
        svg.Should().Contain("sepal-lower-left");
        svg.Should().Contain("sepal-lower-right");
        svg.Should().Contain("labellum-throat");
        svg.Should().NotContain("linearGradient");
        svg.Should().NotContain("radialGradient");
        svg.Should().NotContain("#1E1B4B");
        svg.Should().NotContain("#7C3AED");
        svg.Should().NotContain("fill:#F1E4CA");
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
