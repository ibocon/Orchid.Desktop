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
    public void DesignSystemSchemeSetShouldSwitchBetweenLightAndDarkContexts()
    {
        // Given
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDesignSystem();
        using ServiceProvider provider = services.BuildServiceProvider();
        DesignSystem designSystem = provider.GetRequiredService<DesignSystem>();
        List<SchemeContext> raisedContexts = [];
        designSystem.Color.SchemeSet.ContextChanged += (_, eventArgs) => raisedContexts.Add(eventArgs.SchemeContext);

        // When
        SchemeContext initial = designSystem.Color.SchemeSet.Context;
        string lightSurface = designSystem.Color.SchemeSet.Current.Surface.ToHex();

        designSystem.Color.SchemeSet.ChangeContext(SchemeContext.Dark);
        SchemeContext toggled = designSystem.Color.SchemeSet.Context;
        string darkSurface = designSystem.Color.SchemeSet.Current.Surface.ToHex();

        designSystem.Color.SchemeSet.ChangeContext(SchemeContext.Light);
        SchemeContext restored = designSystem.Color.SchemeSet.Context;
        string restoredSurface = designSystem.Color.SchemeSet.Current.Surface.ToHex();

        // Then
        // ✅ 초기 문맥이 Light이고 Dark 전환 시 Surface 색상이 달라지며 ContextChanged가 발생한다
        initial.Should().Be(SchemeContext.Light);
        toggled.Should().Be(SchemeContext.Dark);
        lightSurface.Should().NotBe(darkSurface);
        // ✅ Light 복귀 시 문맥과 Surface 색상이 원복된다
        restored.Should().Be(SchemeContext.Light);
        restoredSurface.Should().Be(lightSurface);
        raisedContexts.Should().Equal(SchemeContext.Dark, SchemeContext.Light);
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
