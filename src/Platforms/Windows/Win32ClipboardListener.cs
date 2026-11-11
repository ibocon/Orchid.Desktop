#if WINDOWS
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Orchid.Desktop.Platforms.Windows;

/// <summary>
/// Win32 API를 호출하여 클립보드 변경 이벤트를 실시간으로 수신하는 네이티브 리스너입니다.
/// </summary>
public static partial class Win32ClipboardListener
{
    /// <summary>
    /// 지정된 윈도우 핸들을 시스템 클립보드 포맷 리스너 체인에 등록합니다.
    /// </summary>
    /// <param name="hwnd">메시지를 수신할 윈도우 핸들</param>
    /// <returns>등록 성공 여부</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(nint hwnd);

    /// <summary>
    /// 지정된 윈도우 핸들을 시스템 클립보드 포맷 리스너 체인에서 제거합니다.
    /// </summary>
    /// <param name="hwnd">등록을 해제할 윈도우 핸들</param>
    /// <returns>해제 성공 여부</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(nint hwnd);

    /// <summary>
    /// 클립보드 내용이 갱신되었을 때 발생하는 Windows 메시지 식별자입니다.
    /// </summary>
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Win32 message identifier")]
    public const int WM_CLIPBOARDUPDATE = 0x031D;
}
#endif
