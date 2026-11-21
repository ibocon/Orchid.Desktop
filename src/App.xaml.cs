using System.Globalization;
using Microsoft.Extensions.Localization;
using Orchid.Desktop.Component;
using Orchid.Desktop.Service;

namespace Orchid.Desktop;

/// <summary>
/// .NET MAUI 애플리케이션 진입 클래스입니다.
/// </summary>
public partial class App : Application
{
    private readonly IStringLocalizer<ResourceShared> _localizer;

    /// <summary>
    /// <see cref="App"/> 클래스의 새 인스턴스를 초기화합니다.
    /// OS 설치 언어에 맞춰 스레드 컬처를 먼저 적용합니다.
    /// </summary>
    /// <param name="localizer">공통 리소스 지역화 접근자.</param>
    public App(IStringLocalizer<ResourceShared> localizer)
    {
        LanguageCultureResolver.Apply(Language.System, CultureInfo.InstalledUICulture);
        _localizer = localizer;
        try
        {
            InitializeComponent();
            DesktopStartupLog.RecordPhase("maui-app-constructed");
        }
        catch (Exception exception)
        {
            DesktopStartupLog.RecordFatal("maui-app-xaml-initialize", exception);
            throw;
        }

        UserAppTheme = AppTheme.Light;
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;
        WindowSize usableDisplay = new(display.Width / display.Density, display.Height / display.Density);
        WindowSize initial = MainWindowSizePolicy.Resolve(PreferencesWindowSizeStore.Load(), usableDisplay);

        DesktopStartupLog.RecordPhase("main-window-create");
        Window window = new(new MainPage())
        {
            Title = _localizer[nameof(ResourceShared.WindowTitle)],
            Width = initial.Width,
            Height = initial.Height,
            MinimumWidth = MainWindowLayout.MinimumWidth,
            MinimumHeight = MainWindowLayout.MinimumHeight
        };

        WindowSize? latest = null;
        window.SizeChanged += (_, _) =>
        {
            WindowSize current = new(window.Width, window.Height);
            if (WindowRestoreState.IsRestored(window) && MainWindowSizePolicy.IsPersistable(current))
            {
                latest = current;
            }
        };

        void PersistLatest()
        {
            if (latest is { } size)
            {
                PreferencesWindowSizeStore.Save(size);
            }
        }

        window.Deactivated += (_, _) => PersistLatest();
        window.Destroying += (_, _) =>
        {
            PersistLatest();
            DesktopStartupLog.Flush();
        };

        return window;
    }
}
