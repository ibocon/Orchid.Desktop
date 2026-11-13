using System.Text;
using Garden.Razor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Orchid.Desktop.Component;

/// <summary>
/// 데스크톱 환경을 위한 Garden 디자인 시스템 및 MudBlazor Light·Dark 테마 동기화 프로바이더입니다.
/// </summary>
public partial class OrchidDesktopDesignSystemProvider : DesignSystemProvider<DesignSystem>, IAsyncDisposable
{
    [Inject]
    internal DesignSystem DsgnSys { get; set; } = default!;

    /// <summary>
    /// 현재 적용된 테마 컨텍스트(Light 또는 Dark)를 가져옵니다.
    /// </summary>
    public SchemeContext CurrentSchemeContext => DsgnSys.Color.SchemeSet.Context;

    /// <summary>
    /// 현재 컨텍스트가 다크 테마인지 여부를 가져옵니다.
    /// </summary>
    public bool IsDarkMode => DsgnSys.Color.SchemeSet.Context == SchemeContext.Dark;

    /// <summary>
    /// Garden Scheme과 연동된 현재 MudBlazor 테마를 가져옵니다.
    /// </summary>
    public MudTheme CurrentMudTheme { get; private set; } = new();

    /// <summary>
    /// Light 테마와 Dark 테마를 전환합니다.
    /// </summary>
    public void ToggleTheme()
    {
        SchemeContext next = IsDarkMode ? SchemeContext.Light : SchemeContext.Dark;
        SetTheme(next);
    }

    /// <summary>
    /// 지정한 테마 컨텍스트를 적용하고 호스트 앱 테마를 동기화합니다.
    /// </summary>
    /// <param name="context">적용할 테마 컨텍스트</param>
    public void SetTheme(SchemeContext context)
    {
        DsgnSys.Color.SchemeSet.ChangeContext(context);
        if (Microsoft.Maui.Controls.Application.Current is not null)
        {
            Microsoft.Maui.Controls.Application.Current.UserAppTheme =
                context == SchemeContext.Dark ? AppTheme.Dark : AppTheme.Light;
        }
    }

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
        DsgnSys.Color.SchemeSet.ChangeContext(SchemeContext.Light);
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
            PaletteLight = BuildMudPaletteLight(scheme),
            PaletteDark = BuildMudPaletteDark(scheme),
        };
    }

    private static PaletteLight BuildMudPaletteLight(Scheme scheme)
    {
        return new PaletteLight
        {
            Primary = new MudBlazor.Utilities.MudColor(scheme.Primary.ToHex()),
            PrimaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnPrimary.ToHex()),
            Secondary = new MudBlazor.Utilities.MudColor(scheme.Secondary.ToHex()),
            SecondaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnSecondary.ToHex()),
            Tertiary = new MudBlazor.Utilities.MudColor(scheme.Tertiary.ToHex()),
            TertiaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnTertiary.ToHex()),
            Error = new MudBlazor.Utilities.MudColor(scheme.Error.ToHex()),
            ErrorContrastText = new MudBlazor.Utilities.MudColor(scheme.OnError.ToHex()),
            Surface = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
            Background = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
            BackgroundGray = new MudBlazor.Utilities.MudColor(scheme.SurfaceContainerHighest.ToHex()),
            AppbarBackground = new MudBlazor.Utilities.MudColor(scheme.Background.ToHex()),
            TextPrimary = new MudBlazor.Utilities.MudColor(scheme.OnSurface.ToHex()),
            TextSecondary = new MudBlazor.Utilities.MudColor(scheme.OnSurfaceVariant.ToHex()),
            TextDisabled = new MudBlazor.Utilities.MudColor(scheme.Outline.ToHex()),
            ActionDefault = new MudBlazor.Utilities.MudColor(scheme.OnSurfaceVariant.ToHex()),
            ActionDisabled = new MudBlazor.Utilities.MudColor(scheme.Outline.ToHex()),
            Divider = new MudBlazor.Utilities.MudColor(scheme.OutlineVariant.ToHex()),
            LinesDefault = new MudBlazor.Utilities.MudColor(scheme.OutlineVariant.ToHex()),
            DrawerBackground = new MudBlazor.Utilities.MudColor(scheme.SurfaceContainer.ToHex()),
            DrawerText = new MudBlazor.Utilities.MudColor(scheme.OnSurface.ToHex()),
        };
    }

    private static PaletteDark BuildMudPaletteDark(Scheme scheme)
    {
        return new PaletteDark
        {
            Primary = new MudBlazor.Utilities.MudColor(scheme.Primary.ToHex()),
            PrimaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnPrimary.ToHex()),
            Secondary = new MudBlazor.Utilities.MudColor(scheme.Secondary.ToHex()),
            SecondaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnSecondary.ToHex()),
            Tertiary = new MudBlazor.Utilities.MudColor(scheme.Tertiary.ToHex()),
            TertiaryContrastText = new MudBlazor.Utilities.MudColor(scheme.OnTertiary.ToHex()),
            Error = new MudBlazor.Utilities.MudColor(scheme.Error.ToHex()),
            ErrorContrastText = new MudBlazor.Utilities.MudColor(scheme.OnError.ToHex()),
            Surface = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
            Background = new MudBlazor.Utilities.MudColor(scheme.Surface.ToHex()),
            BackgroundGray = new MudBlazor.Utilities.MudColor(scheme.SurfaceContainerHighest.ToHex()),
            AppbarBackground = new MudBlazor.Utilities.MudColor(scheme.Background.ToHex()),
            TextPrimary = new MudBlazor.Utilities.MudColor(scheme.OnSurface.ToHex()),
            TextSecondary = new MudBlazor.Utilities.MudColor(scheme.OnSurfaceVariant.ToHex()),
            TextDisabled = new MudBlazor.Utilities.MudColor(scheme.Outline.ToHex()),
            ActionDefault = new MudBlazor.Utilities.MudColor(scheme.OnSurfaceVariant.ToHex()),
            ActionDisabled = new MudBlazor.Utilities.MudColor(scheme.Outline.ToHex()),
            Divider = new MudBlazor.Utilities.MudColor(scheme.OutlineVariant.ToHex()),
            LinesDefault = new MudBlazor.Utilities.MudColor(scheme.OutlineVariant.ToHex()),
            DrawerBackground = new MudBlazor.Utilities.MudColor(scheme.SurfaceContainer.ToHex()),
            DrawerText = new MudBlazor.Utilities.MudColor(scheme.OnSurface.ToHex()),
        };
    }
}
