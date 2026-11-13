// keep: FluentAssertions assertions
using FluentAssertions;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop의 모든 Razor 컴포넌트가 원시 MudBlazor 태그 대신 Garden.Razor 컴포넌트를 사용하는지 검증합니다.
/// </summary>
public sealed class DesktopGardenRazorComponentAdoptionTest
{
    [Fact]
    public void DesktopRazorComponentsShouldUseGardenRazorComponentsInsteadOfRawMudUiTags()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));
        string pairingPanel = File.ReadAllText(Path.Combine(repoRoot, "src/Component/PairingPanel/PairingPanel.razor"));
        string clipboardList = File.ReadAllText(Path.Combine(repoRoot, "src/Component/ClipboardList/ClipboardList.razor"));

        // When & Then
        // ✅ 모든 데스크톱 UI 컴포넌트가 Garden.Razor 컴포넌트로 구성되고 원시 Mud* UI 태그를 사용하지 않는다
        desktopApp.Should().Contain("<GardenSnackbarProvider");
        desktopApp.Should().Contain("<GardenTopAppBar");
        desktopApp.Should().Contain("<GardenIconButton");
        desktopApp.Should().NotContain("<MudSnackbarProvider");
        desktopApp.Should().NotContain("<MudIconButton");

        pairingPanel.Should().Contain("<GardenCard");
        pairingPanel.Should().Contain("<GardenTypography");
        pairingPanel.Should().Contain("<GardenButton");
        pairingPanel.Should().Contain("<GardenTextField");
        pairingPanel.Should().NotContain("<MudPaper");
        pairingPanel.Should().NotContain("<MudText");
        pairingPanel.Should().NotContain("<MudButton");
        pairingPanel.Should().NotContain("<MudTextField");

        clipboardList.Should().Contain("<GardenTextField");
        clipboardList.Should().Contain("<GardenList");
        clipboardList.Should().Contain("<GardenListItem");
        clipboardList.Should().Contain("<GardenChipAssist");
        clipboardList.Should().NotContain("<MudPaper");
        clipboardList.Should().NotContain("<MudTextField");
        clipboardList.Should().NotContain("<MudList");
        clipboardList.Should().NotContain("<MudCard");
        clipboardList.Should().NotContain("<MudChip");
        clipboardList.Should().NotContain("<MudText");
    }

    [Fact]
    public void LoginPanelShouldUsePeonyBrandLayoutWithoutLegacyCardOrRawMudUiTags()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string loginPanel = File.ReadAllText(Path.Combine(repoRoot, "src/Component/LoginPanel/LoginPanel.razor"));

        // When & Then
        // ✅ LoginPanel은 Peony.Mobile 브랜드 레이아웃으로 개편되어 기존 카드형 레이아웃과 원시 Mud* UI 태그를 사용하지 않는다
        loginPanel.Should().NotContain("<GardenCard");
        loginPanel.Should().NotContain("LockPerson");
        loginPanel.Should().NotContain("<MudIcon");
        loginPanel.Should().NotContain("<MudPaper");
        loginPanel.Should().NotContain("<MudCard");
        loginPanel.Should().NotContain("<MudButton");
        loginPanel.Should().NotContain("<MudText");
    }

    [Fact]
    public void DesktopAppBarTrailingContentShouldRenderGardenIconButtonThemeToggle()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));
        string trailingContent = desktopApp[
            desktopApp.IndexOf("<TrailingContent>", StringComparison.Ordinal)..desktopApp.IndexOf("</TrailingContent>", StringComparison.Ordinal)];

        // When & Then
        // ✅ 상단 앱바 TrailingContent에 GardenIconButton 기반 Light/Dark 전환 아이콘이 배치된다
        trailingContent.Should().Contain("<GardenIconButton");
        trailingContent.Should().Contain("GardenIconButtonVariant.Standard");
        trailingContent.Should().Contain("Icons.Material.Filled.LightMode");
        trailingContent.Should().Contain("Icons.Material.Filled.DarkMode");
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
