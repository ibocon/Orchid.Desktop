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

    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;
    private const int MaxClipboardOpenAttempts = 10;
    private const int RetryDelayMilliseconds = 15;

    /// <summary>
    /// 백그라운드 상태 및 잠금 경합을 고려하여 Win32 시스템 클립보드에서 유니코드 텍스트를 읽어옵니다.
    /// </summary>
    /// <returns>읽어온 클립보드 텍스트 또는 텍스트가 없을 경우 <see langword="null"/></returns>
    public static async Task<string?> GetUnicodeTextAsync()
    {
        for (int attempt = 0; attempt < MaxClipboardOpenAttempts; attempt++)
        {
            if (!IsClipboardFormatAvailable(CfUnicodeText))
            {
                return null;
            }

            if (OpenClipboard(nint.Zero))
            {
                try
                {
                    nint handle = GetClipboardData(CfUnicodeText);
                    if (handle == nint.Zero)
                    {
                        return null;
                    }

                    nint pointer = GlobalLock(handle);
                    if (pointer == nint.Zero)
                    {
                        return null;
                    }

                    try
                    {
                        return Marshal.PtrToStringUni(pointer);
                    }
                    finally
                    {
                        GlobalUnlock(handle);
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }

            await
                Task.Delay(RetryDelayMilliseconds)
                .ConfigureAwait(false);
        }

        return null;
    }

    /// <summary>
    /// 잠금 경합 재시도를 포함하여 Win32 시스템 클립보드에 유니코드 텍스트를 기록합니다.
    /// </summary>
    /// <param name="text">클립보드에 설정할 문자열</param>
    /// <returns>비동기 작업</returns>
    public static async Task SetUnicodeTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (int attempt = 0; attempt < MaxClipboardOpenAttempts; attempt++)
        {
            if (OpenClipboard(nint.Zero))
            {
                try
                {
                    EmptyClipboard();
                    int byteCount = (text.Length + 1) * sizeof(char);
                    nint hGlobal = GlobalAlloc(GmemMoveable, (nuint)byteCount);
                    if (hGlobal == nint.Zero)
                    {
                        return;
                    }

                    nint pointer = GlobalLock(hGlobal);
                    if (pointer == nint.Zero)
                    {
                        GlobalFree(hGlobal);
                        return;
                    }

                    try
                    {
                        Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                        Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }

                    if (SetClipboardData(CfUnicodeText, hGlobal) == nint.Zero)
                    {
                        GlobalFree(hGlobal);
                    }

                    return;
                }
                finally
                {
                    CloseClipboard();
                }
            }

            await
                Task.Delay(RetryDelayMilliseconds)
                .ConfigureAwait(false);
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalFree(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);
}
#endif
