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

    [Fact]
    public void AppIconForegroundShouldUseSameIllustrationAsLoginMark()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string foreground = File.ReadAllText(Path.Combine(repoRoot, "src/Resources/AppIcon/appiconfg.svg"));
        string generator = File.ReadAllText(Path.Combine(repoRoot, "scripts/New-OrchidBrandVisualAssets.ps1"));

        // When & Then
        // ✅ 작업표시줄 아이콘과 앱 내 로고는 동일한 다색 일러스트를 사용한다.
        foreground.Should().Contain("id=\"petal-top\"");
        foreground.Should().Contain("id=\"orchid-throat\"");
        foreground.Should().Contain("stroke=\"#6D5443\"");
        foreground.Should().NotContain("id=\"icon-tile\"");
        foreground.Should().NotContain("taskbar-orchid");
        generator.Should().NotContain("Draw-TaskbarOrchidGlyph");
        generator.Should().Contain("Draw-CompactOrchidMark -G $G");
        generator.Should().Contain("Read-OrchidMarkSvg");
        generator.Should().NotContain("FromArgb(255, 168, 121, 106)");
    }

    [Fact]
    public void ForegroundAndLoginMarkShouldShareThickInkOutlineAndPaths()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string foreground = File.ReadAllText(Path.Combine(repoRoot, "src/Resources/AppIcon/appiconfg.svg"));
        string loginMark = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/logo-login.svg"));

        // When & Then
        // ✅ 두 SVG는 같은 윤곽 굵기·색과 같은 꽃잎 경로를 사용한다.
        foreground.Should().Contain("stroke-width=\"30\"").And.Contain("fill=\"#E58FA0\"").And.Contain("fill=\"#BFA3DA\"");
        foreach (string id in PathIds)
        {
            ExtractPathData(foreground, id).Should().Be(ExtractPathData(loginMark, id));
        }
    }

    [Fact]
    public void IcoShouldContainAllRequiredSizes()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        byte[] ico = File.ReadAllBytes(Path.Combine(repoRoot, "src/Resources/AppIcon/appicon.ico"));

        // When
        int count = BitConverter.ToUInt16(ico, 4);
        int[] sizes = Enumerable.Range(0, count).Select(i => ico[6 + (16 * i)] == 0 ? 256 : ico[6 + (16 * i)]).ToArray();

        // Then
        sizes.Should().BeEquivalentTo([16, 24, 32, 48, 64, 256]);
    }

    private static readonly string[] PathIds = ["petal-top", "petal-left", "petal-right", "orchid-core", "orchid-throat"];

    private static string ExtractPathData(string svg, string id)
    {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
            svg,
            $"id=\"{id}\"[^>]* d=\"([^\"]+)\"");
        match.Success.Should().BeTrue();
        return match.Groups[1].Value;
    }

    [Fact]
    public void LoginMarkAndDesktopCanvasShouldRemainSimpleAndTransparent()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string svg = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/logo-login.svg"));
        string appCss = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/css/app.css"));

        // When & Then
        // ✅ 로그인 마크는 세 꽃잎과 작은 중심부만 남기고 데스크톱 캔버스는 투명하다.
        svg.Should().Contain("#A96C8E");
        svg.Should().Contain("#E58FA0");
        svg.Should().Contain("#BFA3DA");
        svg.Should().Contain("stroke=\"#6D5443\"");
        svg.Should().Contain("stroke-width=\"30\"");
        svg.Should().Contain("id=\"petal-top\"");
        svg.Should().Contain("id=\"petal-left\"");
        svg.Should().Contain("id=\"petal-right\"");
        svg.Should().Contain("id=\"orchid-core\"");
        svg.Should().Contain("id=\"orchid-throat\"");
        svg.Should().NotContain("id=\"stem\"");
        svg.Should().NotContain("vein");
        appCss.Should().Contain("background-color: transparent;");
        appCss.Should().NotContain("background-color: var(--gdn-sys-color-surface);");
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
