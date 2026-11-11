#if WINDOWS
// keep: Marshal.GetFunctionPointerForDelegate와 LibraryImport 마샬링에 사용한다
using System.Runtime.InteropServices;
using Orchid.Core.Abstraction;

namespace Orchid.Desktop.Platforms.Windows;

/// <summary>
/// <c>WM_CLIPBOARDUPDATE</c>를 윈도우 서브클래싱으로 받아 변경을 알리는 구현체입니다.
/// </summary>
public sealed partial class WindowsClipboardChangeSource : IClipboardChangeSource
{
    private readonly SubclassProc _proc;

    /// <summary>
    /// <see cref="WindowsClipboardChangeSource"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public WindowsClipboardChangeSource()
    {
        _proc = OnMessage;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint SubclassProc(nint hwnd, uint msg, nint wParam, nint lParam, nuint id, nuint data);

    /// <inheritdoc />
    public event Func<Task>? Changed;

    /// <inheritdoc />
    public void Start()
    {
        Microsoft.UI.Xaml.Window window = Microsoft.Maui.Controls.Application.Current?.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? throw new InvalidOperationException("Main window handle is unavailable.");
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        if (!SetWindowSubclass(hwnd, Marshal.GetFunctionPointerForDelegate(_proc), 1, 0))
        {
            throw new InvalidOperationException($"SetWindowSubclass failed. Win32 error: {Marshal.GetLastWin32Error()}");
        }

        if (!Win32ClipboardListener.AddClipboardFormatListener(hwnd))
        {
            throw new InvalidOperationException($"AddClipboardFormatListener failed. Win32 error: {Marshal.GetLastWin32Error()}");
        }
    }

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(nint hwnd, nint proc, nuint id, nuint data);

    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint hwnd, uint msg, nint wParam, nint lParam);

    private nint OnMessage(nint hwnd, uint msg, nint wParam, nint lParam, nuint id, nuint data)
    {
        if (msg == Win32ClipboardListener.WM_CLIPBOARDUPDATE)
        {
            _ = NotifyChangedAsync();
        }

        return DefSubclassProc(hwnd, msg, wParam, lParam);
    }

    private async Task NotifyChangedAsync()
    {
        Func<Task>? handler = Changed;
        if (handler is null)
        {
            return;
        }

        try
        {
            await
                handler()
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Clipboard change handling failed: {exception}");
        }
    }
}
#endif
