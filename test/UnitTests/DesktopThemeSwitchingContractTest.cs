using FluentAssertions;
using Garden.Razor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// Orchid.Desktop의 Light 기본 테마 및 Light/Dark 테마 전환 계약을 검증합니다.
/// </summary>
public sealed class DesktopThemeSwitchingContractTest
{
    [Fact]
    public void DesignSystemSchemeSetShouldSupportSwitchingBetweenLightAndDarkContexts()
    {
        // Given
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDesignSystem();
        using ServiceProvider provider = services.BuildServiceProvider();
        DesignSystem designSystem = provider.GetRequiredService<DesignSystem>();

        // When
        designSystem.Color.SchemeSet.ChangeContext(SchemeContext.Light);
        SchemeContext initial = designSystem.Color.SchemeSet.Context;
        string lightSurface = designSystem.Color.SchemeSet.Current.Surface.ToHex();

        designSystem.Color.SchemeSet.ChangeContext(SchemeContext.Dark);
        SchemeContext toggled = designSystem.Color.SchemeSet.Context;
        string darkSurface = designSystem.Color.SchemeSet.Current.Surface.ToHex();

        // Then
        initial.Should().Be(SchemeContext.Light);
        toggled.Should().Be(SchemeContext.Dark);
        lightSurface.Should().NotBe(darkSurface);
    }

    [Fact]
    public void DesktopThemeFilesShouldDefaultToLightAndProvideThemeToggleButton()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string providerCs = File.ReadAllText(Path.Combine(repoRoot, "src/Component/OrchidDesktopDesignSystemProvider/OrchidDesktopDesignSystemProvider.razor.cs"));
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));
        string windowsAppXaml = File.ReadAllText(Path.Combine(repoRoot, "src/Platforms/Windows/App.xaml"));
        string appCss = File.ReadAllText(Path.Combine(repoRoot, "src/wwwroot/css/app.css"));

        // When & Then
        providerCs.Should().Contain("ChangeContext(SchemeContext.Light)");
        providerCs.Should().Contain("PaletteLight");
        providerCs.Should().Contain("PaletteDark");
        providerCs.Should().Contain("ToggleTheme");
        desktopApp.Should().Contain("ToggleTheme");
        desktopApp.Should().NotContain("IsDarkMode=\"true\"");
        windowsAppXaml.Should().Contain("RequestedTheme=\"Light\"");
        appCss.Should().NotContain("#121212");
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
