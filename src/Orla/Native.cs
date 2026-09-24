using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace Orla
{
    public static class Native
    {
        public delegate bool EnumProc(IntPtr window, IntPtr lparam);
        public delegate void EventProc(IntPtr hook, uint ev, IntPtr hwnd, int obj, int child, uint thread,
                                       uint time);
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumProc cb, IntPtr arg);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr h, uint command);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht,
                                               uint flags);
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr dll, EventProc proc, uint pid,
                                                    uint tid, uint flags);
        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr h);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SHGetFileInfo(string path, uint attrs, out SHFILEINFO info, uint size,
                                           uint flags);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEINFO
        {
            public IntPtr Icon;
            public int Index;
            public uint Attrs;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string Name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string Type;
        }
        static Dictionary<string, ImageSource> icons =
            new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        public static bool shellPath(string path)
        {
            return path == "shell:MyComputerFolder" || path == "shell:RecycleBinFolder" ||
                   path == "shell:Downloads";
        }
        public static bool exists(string path)
        {
            return shellPath(path) || File.Exists(path) || Directory.Exists(path);
        }
        public static ImageSource icon(string path)
        {
            ImageSource image;
            if (icons.TryGetValue(path, out image))
                return image;
            SHFILEINFO info;
            if (SHGetFileInfo(path, 0, out info, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), 0x100) ==
                    IntPtr.Zero ||
                info.Icon == IntPtr.Zero)
                return null;
            try
            {
                image = Imaging.CreateBitmapSourceFromHIcon(info.Icon, Int32Rect.Empty,
                                                            BitmapSizeOptions.FromWidthAndHeight(40, 40));
                image.Freeze();
                if (icons.Count < 600)
                    icons[path] = image;
                return image;
            }
            finally
            {
                DestroyIcon(info.Icon);
            }
        }
        public static IntPtr desktopList()
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows(delegate(IntPtr h, IntPtr a) {
                IntPtr view = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero)
                {
                    result = FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
                    if (result != IntPtr.Zero)
                        return false;
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }
        public static string windowClass(IntPtr h)
        {
            var b = new StringBuilder(128);
            GetClassName(h, b, b.Capacity);
            return b.ToString();
        }
        public static void placeAboveDesktop(IntPtr panel)
        {
            IntPtr list = desktopList();
            if (list == IntPtr.Zero)
                return;
            IntPtr shell = GetAncestor(list, 2), previous = GetWindow(shell, 3);
            if (previous == panel)
                return;
            SetWindowPos(panel, previous, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002);
        }
        public static bool aboveDesktop(IntPtr panel)
        {
            IntPtr shell = GetAncestor(desktopList(), 2);
            bool found = false;
            EnumWindows(delegate(IntPtr window, IntPtr state) {
                if (window == shell)
                    return false;
                if (window == panel)
                {
                    found = true;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }
        public static bool desktopOrUs()
        {
            IntPtr h = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            string c = windowClass(h);
            return pid == (uint)Process.GetCurrentProcess().Id || c == "Progman" || c == "WorkerW" ||
                   c == "Shell_TrayWnd" || c == "Shell_SecondaryTrayWnd";
        }
        public static void open(string path)
        {
            if (shellPath(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
                return;
            }
            if (!exists(path))
                throw new FileNotFoundException(
                    "Este item não está mais no caminho salvo. Use o menu do item para removê-lo do painel.");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        public static void reveal(string path)
        {
            if (Directory.Exists(path))
            {
                open(path);
                return;
            }
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe",
                                                   "/select,\"" + path + "\"") { UseShellExecute = true });
        }
        public static void guard(int pid, long handle, bool wasVisible, string readyFile)
        {
            try
            {
                using (Process parent = Process.GetProcessById(pid))
                {
                    File.WriteAllText(readyFile, "ready");
                    parent.WaitForExit();
                }
            }
            catch (ArgumentException)
            {
            }
            catch (Exception)
            {
                return;
            }
            IntPtr list = new IntPtr(handle);
            if (IsWindow(list))
                ShowWindow(list, wasVisible ? 5 : 0);
        }
    }
}
