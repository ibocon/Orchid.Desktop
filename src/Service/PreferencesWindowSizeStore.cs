namespace Orchid.Desktop.Service;

/// <summary>
/// 마지막 메인 창 크기를 MAUI <see cref="Preferences"/>에 저장하고 읽습니다.
/// </summary>
public static class PreferencesWindowSizeStore
{
    private const string WidthKey = "MainWindow.Width";
    private const string HeightKey = "MainWindow.Height";

    /// <summary>
    /// 저장된 창 크기를 읽습니다. 값이 없거나 손상되었으면 <see langword="null"/>을 반환합니다.
    /// </summary>
    /// <returns>저장된 크기 또는 <see langword="null"/>.</returns>
    public static WindowSize? Load()
    {
        double width = Preferences.Default.Get(WidthKey, double.NaN);
        double height = Preferences.Default.Get(HeightKey, double.NaN);
        WindowSize size = new(width, height);

        return MainWindowSizePolicy.IsPersistable(size) ? size : null;
    }

    /// <summary>
    /// 창 크기를 저장합니다. 저장 불가능한 값은 무시합니다.
    /// </summary>
    /// <param name="size">저장할 크기.</param>
    public static void Save(WindowSize size)
    {
        if (!MainWindowSizePolicy.IsPersistable(size))
        {
            return;
        }

        Preferences.Default.Set(WidthKey, size.Width);
        Preferences.Default.Set(HeightKey, size.Height);
    }
}
