using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Orla
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public static class Native
    {
        public delegate bool EnumProc(IntPtr hwnd, IntPtr lparam);
        public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data);
        public delegate void WinEventProc(IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread,
                                          uint time);

        public const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        public const int WS_CHILD = 0x40000000, WS_POPUP = unchecked((int)0x80000000), WS_CLIPSIBLINGS = 0x04000000,
                         WS_CLIPCHILDREN = 0x02000000;
        public const int WS_EX_TOPMOST = 0x8, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
        public static readonly IntPtr HWND_TOP = IntPtr.Zero, HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10,
                          SWP_SHOWWINDOW = 0x40;
        public const uint GW_HWNDNEXT = 2, GW_HWNDPREV = 3, GW_CHILD = 5;
        public const uint GA_PARENT = 1, GA_ROOT = 2;
        public const int SW_HIDE = 0, SW_SHOW = 5, SW_SHOWNA = 8;
        public const uint EVENT_OBJECT_DESTROY = 0x8001, EVENT_OBJECT_REORDER = 0x8004,
                          EVENT_OBJECT_PARENTCHANGE = 0x800F;
        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const int WM_HOTKEY = 0x0312, WM_SETTINGCHANGE = 0x001A, WM_DISPLAYCHANGE = 0x007E,
                         WM_DPICHANGED = 0x02E0;
        public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8, MOD_NOREPEAT = 0x4000;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_SYSTEMBACKDROP_TYPE = 38,
                         DWMWA_EXCLUDED_FROM_PEEK = 12;

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumProc cb, IntPtr arg);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr h);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        static extern IntPtr SetWindowLongPtr(IntPtr h, int index, IntPtr value);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr h, ref POINT p);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc cb, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX info);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT p, uint flags);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromRect(ref RECT r, uint flags);
        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr dll, WinEventProc proc, uint pid,
                                                    uint tid, uint flags);
        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string s);
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr h);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr h);

        public static string windowClass(IntPtr h)
        {
            var b = new StringBuilder(128);
            GetClassName(h, b, b.Capacity);
            return b.ToString();
        }

        public static uint processOf(IntPtr h)
        {
            GetWindowThreadProcessId(h, out uint pid);
            return pid;
        }

        public static int style(IntPtr h) => GetWindowLongPtr(h, GWL_STYLE).ToInt32();
        public static int exStyle(IntPtr h) => GetWindowLongPtr(h, GWL_EXSTYLE).ToInt32();
        public static void setExStyle(IntPtr h, int value) => SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr(value));

        public static RECT rect(IntPtr h)
        {
            GetWindowRect(h, out RECT r);
            return r;
        }

        // Children of a window from the top of the z-order down.
        public static List<IntPtr> children(IntPtr parent)
        {
            var list = new List<IntPtr>();
            for (IntPtr c = GetWindow(parent, GW_CHILD); c != IntPtr.Zero && list.Count < 4096;
                 c = GetWindow(c, GW_HWNDNEXT))
                list.Add(c);
            return list;
        }

        public static void setDwm(IntPtr h, int attribute, int value)
        {
            try
            {
                DwmSetWindowAttribute(h, attribute, ref value, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
        }
    }
}
