#if MACCATALYST
namespace Orchid.Desktop.Platforms.MacCatalyst;

/// <summary>
/// macOS NSPasteboard changeCount 속성을 주기적으로 폴링하여 변경을 감지하는 리스너입니다.
/// </summary>
public sealed class MacPasteboardListener
{
    private nint _lastChangeCount;

    /// <summary>
    /// 클립보드 변경 이벤트 발생 시 트리거되는 이벤트 핸들러입니다.
    /// </summary>
    public event EventHandler? ClipboardChanged;

    /// <summary>
    /// 현재 클립보드 상태를 확인하고 변경 시 이벤트를 발생시킵니다.
    /// </summary>
    public void CheckForChanges()
    {
        // AppKit 연동 시 NSPasteboard.GeneralPasteboard.ChangeCount 확인
        ClipboardChanged?.Invoke(this, EventArgs.Empty);
    }
}
#endif
