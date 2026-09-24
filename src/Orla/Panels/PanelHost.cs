using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Orla
{
    public enum PanelMode
    {
        // A child of the window that holds the desktop icons: part of the desktop, below every application.
        Desktop,
        // A topmost window, summoned in front of applications with the shortcut or the tray.
        Overlay,
        // A plain window just above the desktop, used when Explorer's desktop cannot be found.
        Fallback
    }

    // Owns the native window of one panel. Switching modes rebuilds the window around the same PanelView, which is
    // also how panels come back after Explorer restarts.
    public class PanelHost : IDisposable
    {
        readonly Controller controller;
        HwndSource source;
        Desktop desktop;
        RECT rect, moveStart;
        POINT moveCursor;
        double scale = 1;

        public PanelView View { get; }
        public PanelMode Mode { get; private set; }
        public Group Group => View.Group;
        public RECT Rect => rect;
        public IntPtr Handle => source?.Handle ?? IntPtr.Zero;

        public PanelHost(Controller controller, Group group)
        {
            this.controller = controller;
            View = new PanelView(controller, group);
            View.MoveStarted += delegate {
                Native.GetCursorPos(out moveCursor);
                moveStart = rect;
                bringToFront();
            };
            View.MoveUpdated += delegate {
                Native.GetCursorPos(out POINT now);
                int dx = now.X - moveCursor.X, dy = now.Y - moveCursor.Y;
                rect = new RECT { Left = moveStart.Left + dx, Top = moveStart.Top + dy, Right = moveStart.Right + dx,
                                  Bottom = moveStart.Bottom + dy };
                place(false);
            };
            View.MoveEnded += delegate {
                rect = Screens.snap(rect, controller.Hosts.Where(h => h != this).Select(h => h.Rect));
                Group.X = rect.Left;
                Group.Y = rect.Top;
                fit();
                controller.saveLayout();
            };
            View.Resized += (dx, dy) => {
                Group.Width = Math.Max(Group.MinWidth, Math.Min(Group.MaxWidth, Group.Width + dx));
                Group.Height = Math.Max(Group.MinHeight, Math.Min(Group.MaxHeight, Group.Height + dy));
                View.Width = Group.Width;
                View.Height = Group.Height;
                rect.Right = rect.Left + pixels(Group.Width);
                rect.Bottom = rect.Top + pixels(Group.Height);
                place(true);
            };
            View.ResizeEnded += delegate {
                fit();
                controller.saveLayout();
            };
        }

        int pixels(double dip) => (int)Math.Round(dip * scale);

        public void show(PanelMode mode, Desktop target)
        {
            destroyWindow();
            desktop = target;
            Mode = mode;
            measure();
            create();
        }

        // Size and position in physical pixels, on the monitor the panel belongs to.
        void measure()
        {
            Screen s = Screens.at((int)Group.X + 40, (int)Group.Y + 20);
            scale = s.Scale;
            double height = Group.Collapsed ? PanelView.CollapsedHeight : Group.Height;
            rect = Screens.clamp(new RECT { Left = (int)Group.X, Top = (int)Group.Y, Right = (int)Group.X + pixels(Group.Width),
                                            Bottom = (int)Group.Y + pixels(height) });
        }

        void create()
        {
            int style, ex;
            IntPtr parent = IntPtr.Zero;
            int x = rect.Left, y = rect.Top;
            if (Mode == PanelMode.Desktop)
            {
                style = Native.WS_CHILD | Native.WS_CLIPSIBLINGS | Native.WS_CLIPCHILDREN;
                ex = 0;
                parent = desktop.Host;
                POINT p = desktop.toHost(rect.Left, rect.Top);
                x = p.X;
                y = p.Y;
            }
            else if (Mode == PanelMode.Overlay)
            {
                style = Native.WS_POPUP | Native.WS_CLIPCHILDREN;
                ex = Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST;
            }
            else
            {
                style = Native.WS_POPUP | Native.WS_CLIPCHILDREN;
                ex = Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
            }
            source = new HwndSource(new HwndSourceParameters("Orla · " + Group.Name, rect.Width, rect.Height) {
                WindowStyle = style, ExtendedWindowStyle = ex, ParentWindow = parent, PositionX = x, PositionY = y,
                UsesPerPixelTransparency = true
            });
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            applyScale();
            source.RootVisual = View;
            if (Mode == PanelMode.Desktop)
                desktop.placeAbove(Handle);
            else if (Mode == PanelMode.Overlay)
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0,
                                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            else
                placeAboveShell();
            Native.ShowWindow(Handle, Native.SW_SHOWNA);
            if (controller.Layout.Animations && SystemParameters.ClientAreaAnimation)
                Motion.reveal(View);
        }

        // A child window takes the DPI of its host (the primary monitor). Panels on other monitors are scaled to
        // match the monitor they sit on; top-level windows get this from WPF itself.
        void applyScale()
        {
            View.Scale = scale;
            double factor = Mode == PanelMode.Desktop ? scale * 96.0 / desktop.dpi : 1;
            View.LayoutTransform = Math.Abs(factor - 1) < 0.01 ? Transform.Identity : new ScaleTransform(factor, factor);
        }

        void placeAboveShell()
        {
            IntPtr shell = Native.GetShellWindow();
            IntPtr previous = shell == IntPtr.Zero ? IntPtr.Zero : Native.GetWindow(shell, Native.GW_HWNDPREV);
            if (previous != IntPtr.Zero && previous != Handle)
                Native.SetWindowPos(Handle, previous, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            Native.setDwm(Handle, Native.DWMWA_EXCLUDED_FROM_PEEK, 1);
        }

        void place(bool resize)
        {
            if (source == null)
                return;
            int x = rect.Left, y = rect.Top;
            if (Mode == PanelMode.Desktop)
            {
                POINT p = desktop.toHost(x, y);
                x = p.X;
                y = p.Y;
            }
            Native.SetWindowPos(Handle, IntPtr.Zero, x, y, rect.Width, rect.Height,
                                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | (resize ? 0 : Native.SWP_NOSIZE));
        }

        // After a move or resize: keep the panel on screen and adopt the scale of the monitor it landed on.
        void fit()
        {
            double before = scale;
            Group.X = rect.Left;
            Group.Y = rect.Top;
            measure();
            if (Math.Abs(before - scale) > 0.01)
                applyScale();
            place(true);
        }

        public void refresh()
        {
            View.refresh();
            measure();
            place(true);
        }

        // Called when something reorders the desktop's windows, such as a wallpaper app starting.
        public void keepAbove()
        {
            if (Mode == PanelMode.Desktop && source != null && desktop.IsValid && !desktop.isAbove(Handle))
                desktop.placeAbove(Handle);
        }

        public void bringToFront()
        {
            if (source == null)
                return;
            if (Mode == PanelMode.Desktop)
                desktop.placeAbove(Handle);
            else if (Mode == PanelMode.Overlay)
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0,
                                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        public void focus()
        {
            if (source == null)
                return;
            if (Mode == PanelMode.Overlay)
                Native.SetForegroundWindow(Handle);
            Native.SetFocus(Handle);
        }

        void destroyWindow()
        {
            if (source == null)
                return;
            try
            {
                source.RootVisual = null;
                source.Dispose();
            }
            catch (Exception)
            {
                // The window may already be gone with Explorer's desktop.
            }
            source = null;
        }

        public void Dispose()
        {
            destroyWindow();
        }
    }
}
