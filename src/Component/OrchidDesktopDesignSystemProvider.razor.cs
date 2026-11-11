using System.Text;
using Garden.Razor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Orchid.Desktop.Component;

/// <summary>
/// 데스크톱 환경을 위한 Garden 디자인 시스템 및 MudBlazor 테마 동기화 프로바이더입니다.
/// </summary>
public partial class OrchidDesktopDesignSystemProvider : DesignSystemProvider<DesignSystem>, IAsyncDisposable
{
    [Inject]
    internal DesignSystem DsgnSys { get; set; } = default!;

    /// <summary>
    /// Garden Scheme과 연동된 현재 MudBlazor 테마를 가져옵니다.
    /// </summary>
    public MudTheme CurrentMudTheme { get; private set; } = new();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        DsgnSys.Color.SchemeSet.ContextChanged -= OnSchemeContextChanged;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        DsgnSys.Color.SchemeSet.ChangeContext(SchemeContext.Dark);
        ApplyMudThemeFromDesignSystem();
        DsgnSys.Color.SchemeSet.ContextChanged += OnSchemeContextChanged;
    }

    /// <inheritdoc />
    protected override void BuildCustom(StringBuilder builder)
    {
        builder
            .AppendLine("body {")
            .AppendTab().AppendLine("background-color: var(--gdn-sys-color-surface);")
            .AppendTab().AppendLine("color: var(--gdn-sys-color-on-surface);")
            .AppendLine("}");
    }

    private void OnSchemeContextChanged(object? sender, SchemeContextChangedEventArgs e)
    {
        ApplyMudThemeFromDesignSystem();
        InvokeAsync(StateHasChanged);
    }

    private void ApplyMudThemeFromDesignSystem()
    {
        Scheme scheme = DsgnSys.Color.SchemeSet.Current;
        CurrentMudTheme = new MudTheme
        {
            PaletteDark = new PaletteDark
            {
                Primary = new MudBlazor.Utilities.MudColor(scheme.Primary.ToHex()),
                Surface = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
                Background = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
                TextPrimary = new MudBlazor.Utilities.MudColor(scheme.OnSurface.ToHex()),
                TextSecondary = new MudBlazor.Utilities.MudColor(scheme.OnSurfaceVariant.ToHex())
            }
        };
    }
}
