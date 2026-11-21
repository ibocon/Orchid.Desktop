using Orchid.Core.Abstraction;

namespace Orchid.Desktop.Service;

/// <summary>
/// .NET MAUI 데스크톱 환경의 시스템 클립보드 어댑터 구현체입니다.
/// </summary>
public sealed class DesktopClipboardAdapter : ISystemClipboard
{
    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
    {
#if WINDOWS
        return await
            Orchid.Desktop.Platforms.Windows.Win32ClipboardListener.GetUnicodeTextAsync()
            .ConfigureAwait(false);
#else
        if (Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.HasText)
        {
            return await
                Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.GetTextAsync()
                .ConfigureAwait(false);
        }

        return null;
#endif
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
#if WINDOWS
        await
            Orchid.Desktop.Platforms.Windows.Win32ClipboardListener.SetUnicodeTextAsync(text)
            .ConfigureAwait(false);
#else
        await
            Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.SetTextAsync(text)
            .ConfigureAwait(false);
#endif
    }
}
