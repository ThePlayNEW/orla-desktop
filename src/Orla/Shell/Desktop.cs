using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Orla
{
    // Explorer's desktop, found by structure rather than by Windows version:
    // Windows 10 and Windows 11 up to 23H2 keep the icon view in Progman or, once a wallpaper app is running, in a
    // top-level WorkerW; Windows 11 24H2 keeps it in Progman as a layered child. Panels become children of whichever
    // window holds the icon view, placed just above it. Orla never sends Progman's 0x052C message and never changes
    // the wallpaper, so wallpaper engines keep full control of their own windows.
    public class Desktop
    {
        public IntPtr Host { get; private set; }
        public IntPtr IconView { get; private set; }
        public IntPtr IconList { get; private set; }
        public uint ExplorerProcess { get; private set; }

        public bool IsValid => IconView != IntPtr.Zero && Native.IsWindow(Host) && Native.IsWindow(IconView);

        public static Desktop find()
        {
            var d = new Desktop();
            IntPtr progman = Native.FindWindow("Progman", null);
            IntPtr view = progman == IntPtr.Zero
                              ? IntPtr.Zero
                              : Native.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (view == IntPtr.Zero)
                Native.EnumWindows(delegate(IntPtr h, IntPtr a) {
                    view = Native.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                    return view == IntPtr.Zero;
                }, IntPtr.Zero);
            if (view == IntPtr.Zero && progman != IntPtr.Zero)
                foreach (IntPtr child in Native.children(progman))
                {
                    view = Native.FindWindowEx(child, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (view != IntPtr.Zero)
                        break;
                }
            if (view == IntPtr.Zero)
                return d;
            d.IconView = view;
            d.Host = Native.GetParent(view);
            d.IconList = Native.FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
            d.ExplorerProcess = Native.processOf(view);
            return d;
        }

        // Puts a panel on top of the host's children, which keeps it above the icon view and above other panels.
        public void placeAbove(IntPtr panel)
        {
            Native.SetWindowPos(panel, Native.HWND_TOP, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        public bool isAbove(IntPtr panel)
        {
            foreach (IntPtr child in Native.children(Host))
            {
                if (child == panel)
                    return true;
                if (child == IconView)
                    return false;
            }
            return false;
        }

        public POINT toHost(int screenX, int screenY)
        {
            var p = new POINT(screenX, screenY);
            Native.ScreenToClient(Host, ref p);
            return p;
        }

        public uint dpi
        {
            get
            {
                uint value = Native.GetDpiForWindow(Host);
                return value == 0 ? 96u : value;
            }
        }

        public bool iconsVisible => IconList != IntPtr.Zero && Native.IsWindowVisible(IconList);

        // What the person chose in Explorer (View > Show desktop icons). A crashed organizer can leave the icon view
        // hidden while this still says "show", so this, not the window state, is the state to return to.
        public static bool userWantsIcons()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                           @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    return !(key?.GetValue("HideIcons") is int hide && hide != 0);
            }
            catch (Exception)
            {
                return true;
            }
        }

        public void setIconsVisible(bool visible)
        {
            if (IconList != IntPtr.Zero && Native.IsWindow(IconList))
                Native.ShowWindow(IconList, visible ? Native.SW_SHOW : Native.SW_HIDE);
        }
    }

    // Hides the Windows icons for "clean desktop" and guarantees they come back: a small companion copy of Orla
    // waits for this process to exit, however it exits, and restores the icons to the person's Explorer setting.
    // The companion starts off the UI thread; icons are hidden only once it is ready.
    public class IconGuard
    {
        readonly string dataDirectory;
        readonly System.Windows.Threading.Dispatcher dispatcher;
        bool originalVisible, hidden, wantHidden, starting, guardStarted;

        public Desktop Target { get; }
        public event Action Failed;

        public IconGuard(Desktop desktop, string dataDirectory, System.Windows.Threading.Dispatcher dispatcher)
        {
            Target = desktop;
            this.dataDirectory = dataDirectory;
            this.dispatcher = dispatcher;
        }

        public void hide()
        {
            wantHidden = true;
            if (hidden || starting || !Target.IsValid || Target.IconList == IntPtr.Zero)
                return;
            originalVisible = Desktop.userWantsIcons();
            if (guardStarted)
            {
                apply();
                return;
            }
            starting = true;
            long list = Target.IconList.ToInt64();
            bool visible = originalVisible;
            System.Threading.Tasks.Task.Run(() => startGuard(list, visible)).ContinueWith(t => {
                dispatcher.BeginInvoke(new Action(delegate {
                    starting = false;
                    guardStarted = !t.IsFaulted && t.Result;
                    if (guardStarted)
                        apply();
                    else if (wantHidden)
                        Failed?.Invoke();
                }));
            });
        }

        void apply()
        {
            if (!wantHidden || hidden)
                return;
            Target.setIconsVisible(false);
            hidden = true;
        }

        public void restore()
        {
            wantHidden = false;
            if (!hidden)
                return;
            hidden = false;
            Target.setIconsVisible(originalVisible);
        }

        // Shows icons that another program left hidden against the person's Explorer setting.
        public void repair()
        {
            if (!hidden && Target.IsValid && !Target.iconsVisible && Desktop.userWantsIcons())
                Target.setIconsVisible(true);
        }

        bool startGuard(long iconList, bool visible)
        {
            int pid = Process.GetCurrentProcess().Id;
            string ready = Path.Combine(dataDirectory, "guard-" + pid + ".ready");
            Directory.CreateDirectory(dataDirectory);
            if (File.Exists(ready))
                File.Delete(ready);
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            var p = Process.Start(new ProcessStartInfo(exe, "--guard " + pid + " " + iconList + " " + (visible ? "1" : "0") +
                                                                 " \"" + ready + "\"") {
                UseShellExecute = false, CreateNoWindow = true
            });
            for (int i = 0; i < 100 && !File.Exists(ready) && !p.HasExited; i++)
                Thread.Sleep(50);
            if (!File.Exists(ready))
                return false;
            File.Delete(ready);
            return true;
        }

        // Runs in the companion process.
        public static void guard(int pid, long iconList, bool wasVisible, string readyFile)
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
            var list = new IntPtr(iconList);
            if (Native.IsWindow(list))
                Native.ShowWindow(list, wasVisible ? Native.SW_SHOW : Native.SW_HIDE);
        }
    }
}
