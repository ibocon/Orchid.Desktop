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
        if (Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.HasText)
        {
            return await Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.GetTextAsync().ConfigureAwait(false);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        await Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.SetTextAsync(text).ConfigureAwait(false);
    }
}
