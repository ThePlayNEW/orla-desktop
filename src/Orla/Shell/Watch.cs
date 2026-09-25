using System;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Orla
{
    // An invisible top-level window that receives what Windows broadcasts: Explorer restarts (TaskbarCreated),
    // theme and display changes, and the global shortcut.
    public class MessageWindow : IDisposable
    {
        const int HotkeyId = 0x4F52;
        static readonly uint taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");

        readonly HwndSource source;
        bool hotkeyRegistered;

        public event Action ExplorerRestarted, SettingsChanged, DisplayChanged, Hotkey;

        public MessageWindow()
        {
            source = new HwndSource(new HwndSourceParameters("Orla") {
                Width = 0, Height = 0, WindowStyle = Native.WS_POPUP, ExtendedWindowStyle = Native.WS_EX_TOOLWINDOW
            });
            source.AddHook(hook);
        }

        public IntPtr Handle => source.Handle;

        public bool setHotkey(bool enabled, Shortcut shortcut)
        {
            if (hotkeyRegistered)
                Native.UnregisterHotKey(Handle, HotkeyId);
            hotkeyRegistered = enabled && Native.RegisterHotKey(Handle, HotkeyId, shortcut.nativeModifiers | Native.MOD_NOREPEAT,
                                                                shortcut.virtualKey);
            return hotkeyRegistered || !enabled;
        }

        IntPtr hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == Native.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
                Hotkey?.Invoke();
            else if (message == Native.WM_SETTINGCHANGE)
                SettingsChanged?.Invoke();
            else if (message == Native.WM_DISPLAYCHANGE || message == Native.WM_DPICHANGED)
                DisplayChanged?.Invoke();
            else if (taskbarCreated != 0 && message == taskbarCreated)
                ExplorerRestarted?.Invoke();
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            setHotkey(false, default(Shortcut));
            source.Dispose();
        }
    }

    // Follows Explorer's desktop windows through WinEvents, which Windows delivers only when something changes:
    // the icon host being destroyed or re-parented (Explorer restart, a wallpaper app starting) and z-order
    // changes among the host's children (something covering the panels).
    public class DesktopWatch : IDisposable
    {
        readonly Native.WinEventProc proc;
        readonly Dispatcher dispatcher;
        IntPtr destroyHook, reorderHook, parentHook, foregroundHook;
        Desktop desktop;
        bool reorderQueued;

        // DesktopActivated: the desktop came in front of the applications, as with Win+D or a click on it.
        public event Action Lost, Reordered, DesktopActivated;

        public DesktopWatch(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            proc = onEvent;
        }

        public void follow(Desktop target)
        {
            stop();
            desktop = target;
            if (target == null || !target.IsValid)
                return;
            uint pid = target.ExplorerProcess;
            destroyHook = Native.SetWinEventHook(Native.EVENT_OBJECT_DESTROY, Native.EVENT_OBJECT_DESTROY, IntPtr.Zero,
                                                 proc, pid, 0, 0);
            reorderHook = Native.SetWinEventHook(Native.EVENT_OBJECT_REORDER, Native.EVENT_OBJECT_REORDER, IntPtr.Zero,
                                                 proc, pid, 0, 0);
            parentHook = Native.SetWinEventHook(Native.EVENT_OBJECT_PARENTCHANGE, Native.EVENT_OBJECT_PARENTCHANGE,
                                                IntPtr.Zero, proc, pid, 0, 0);
            foregroundHook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                                                    IntPtr.Zero, proc, 0, 0, 0);
        }

        void onEvent(IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            if (desktop == null || idObject != 0)
                return;
            if (ev == Native.EVENT_SYSTEM_FOREGROUND)
            {
                string c = Native.windowClass(hwnd);
                // After the click or key that brought it has been handled.
                if (c == "Progman" || c == "WorkerW")
                    dispatcher.BeginInvoke(new Action(() => DesktopActivated?.Invoke()), DispatcherPriority.Background);
                return;
            }
            if (ev == Native.EVENT_OBJECT_DESTROY && (hwnd == desktop.Host || hwnd == desktop.IconView) ||
                ev == Native.EVENT_OBJECT_PARENTCHANGE && hwnd == desktop.IconView)
            {
                dispatcher.BeginInvoke(new Action(() => Lost?.Invoke()));
            }
            else if (ev == Native.EVENT_OBJECT_REORDER && hwnd == desktop.Host && !reorderQueued)
            {
                reorderQueued = true;
                dispatcher.BeginInvoke(new Action(delegate {
                    reorderQueued = false;
                    Reordered?.Invoke();
                }), DispatcherPriority.Background);
            }
        }

        void stop()
        {
            foreach (IntPtr h in new[] { destroyHook, reorderHook, parentHook, foregroundHook })
                if (h != IntPtr.Zero)
                    Native.UnhookWinEvent(h);
            destroyHook = reorderHook = parentHook = foregroundHook = IntPtr.Zero;
        }

        public void Dispose()
        {
            stop();
        }
    }
}
