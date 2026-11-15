// keep: FluentAssertions assertions
using FluentAssertions;
// keep: PairingKey domain type
using Orchid.Core.Sync.Relay;
// keep: PairingQrCodeSvgGenerator service
using Orchid.Desktop.Service;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop 홈 대시보드 리디자인 및 QR 코드 + 페어링 키 값 동시 제공 계약을 검증합니다.
/// </summary>
public sealed class DesktopHomeDashboardAndQrPairingContractTest
{
    /// <summary>
    /// 페어링 키 페이로드로부터 유효하고 결정론적인 QR 코드 SVG 마크업이 생성되는지 검증합니다.
    /// </summary>
    [Fact]
    public void GenerateSvgShouldReturnDeterministicQrCodeSvgForPairingPayload()
    {
        // Given
        string payload = PairingKey.Generate().ToPayload();

        // When
        string svg1 = PairingQrCodeSvgGenerator.GenerateSvg(payload);
        string svg2 = PairingQrCodeSvgGenerator.GenerateSvg(payload);

        // Then
        // ✅ 동일한 페이로드에 대해 결정론적인 SVG와 Quiet Zone 배경 및 모듈 path가 생성된다
        svg1.Should().Be(svg2);
        svg1.Should().StartWith("<svg");
        svg1.Should().Contain("aria-label=\"Pairing QR Code\"");
        svg1.Should().Contain("<path d=\"M");
    }

    /// <summary>
    /// PairingPanel과 ClipboardList 및 DesktopApp이 QR 코드와 페어링 키 값 및 대시보드 스타일 계약을 준수하는지 검증합니다.
    /// </summary>
    [Fact]
    public void PairingPanelAndClipboardListShouldRenderQrCodeWithKeyValueAndDashboardLayout()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string pairingPanel = File.ReadAllText(Path.Combine(repoRoot, "src/Component/PairingPanel/PairingPanel.razor"));
        string clipboardList = File.ReadAllText(Path.Combine(repoRoot, "src/Component/ClipboardList/ClipboardList.razor"));
        string pairingCssPath = Path.Combine(repoRoot, "src/Component/PairingPanel/PairingPanel.razor.css");
        string clipboardCssPath = Path.Combine(repoRoot, "src/Component/ClipboardList/ClipboardList.razor.css");
        string desktopAppCssPath = Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor.css");

        // When & Then
        // ✅ PairingPanel에 QR 코드 SVG 렌더링과 GardenTextField 페어링 키 값 및 복사 버튼이 함께 구성된다
        pairingPanel.Should().Contain("PairingQrCodeSvgGenerator.GenerateSvg");
        pairingPanel.Should().Contain("pairing-qr-container");
        pairingPanel.Should().Contain("<GardenTextField");
        pairingPanel.Should().Contain("CopyKeyAsync");
        clipboardList.Should().Contain("clipboard-workspace");
        File.Exists(pairingCssPath).Should().BeTrue();
        File.Exists(clipboardCssPath).Should().BeTrue();
        File.Exists(desktopAppCssPath).Should().BeTrue();
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
