namespace VoltStream.WPF.Commons.Utils;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public static class MonitorMaximizeHelper
{
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

    public static void Enable(Window window)
        => window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(Hook);
        };

    private static nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WM_GETMINMAXINFO) return nint.Zero;

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor != nint.Zero)
        {
            var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            GetMonitorInfo(monitor, ref info);

            var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            mmi.ptMaxPosition.x = info.rcWork.left - info.rcMonitor.left;
            mmi.ptMaxPosition.y = info.rcWork.top - info.rcMonitor.top;
            mmi.ptMaxSize.x = info.rcWork.right - info.rcWork.left;
            mmi.ptMaxSize.y = info.rcWork.bottom - info.rcWork.top;
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        handled = true;
        return nint.Zero;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int left; public int top; public int right; public int bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }
}
