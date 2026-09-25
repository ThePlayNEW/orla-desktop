using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Orla
{
    // A native window on one of Orla's layers: part of the desktop below every application, in front of everything
    // when summoned, or a plain window when Explorer's desktop is not there. Switching layers rebuilds the window
    // around the same content. Panels and the search bar are both one of these.
    public abstract class LayerWindow : IDisposable
    {
        protected readonly Controller controller;
        protected HwndSource source;
        protected Desktop desktop;
        protected RECT rect;
        protected bool visible = true;
        protected double scale = 1;

        protected LayerWindow(Controller controller)
        {
            this.controller = controller;
        }

        protected abstract FrameworkElement Root { get; }
        protected abstract string Title { get; }

        public PanelMode Mode { get; protected set; }
        public RECT Rect => rect;
        public IntPtr Handle => source?.Handle ?? IntPtr.Zero;

        protected int pixels(double dip) => (int)Math.Round(dip * scale);

        protected void create()
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
            source = new HwndSource(new HwndSourceParameters(Title, rect.Width, rect.Height) {
                WindowStyle = style, ExtendedWindowStyle = ex, ParentWindow = parent, PositionX = x, PositionY = y,
                UsesPerPixelTransparency = true
            });
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            applyScale();
            source.RootVisual = Root;
            if (Mode == PanelMode.Fallback)
                placeAboveShell();
            else
                bringToFront();
            if (!visible)
                return;
            Native.ShowWindow(Handle, Native.SW_SHOWNA);
            reveal();
        }

        // A child window takes the DPI of its host (the primary monitor). Content on other monitors is scaled to
        // match the monitor it sits on; top-level windows get this from WPF itself.
        protected void applyScale()
        {
            double factor = Mode == PanelMode.Desktop ? scale * 96.0 / desktop.dpi : 1;
            Root.LayoutTransform = Math.Abs(factor - 1) < 0.01 ? Transform.Identity : new ScaleTransform(factor, factor);
        }

        void placeAboveShell()
        {
            IntPtr shell = Native.GetShellWindow();
            IntPtr previous = shell == IntPtr.Zero ? IntPtr.Zero : Native.GetWindow(shell, Native.GW_HWNDPREV);
            if (previous != IntPtr.Zero && previous != Handle)
                Native.SetWindowPos(Handle, previous, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            Native.setDwm(Handle, Native.DWMWA_EXCLUDED_FROM_PEEK, 1);
        }

        protected void place(bool resize)
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

        // Called when something reorders the desktop's windows, such as a wallpaper app starting.
        public void keepAbove()
        {
            if (Mode == PanelMode.Desktop && source != null && desktop.IsValid && !desktop.isAbove(Handle))
                desktop.placeAbove(Handle);
        }

        public void setVisible(bool show)
        {
            if (source == null || visible == show)
                return;
            visible = show;
            Native.ShowWindow(Handle, show ? Native.SW_SHOWNA : Native.SW_HIDE);
            if (show)
                reveal();
        }

        // The only motion when the window appears: a short, eased fade. Nothing runs continuously.
        void reveal()
        {
            if (!controller.Layout.Animations || !SystemParameters.ClientAreaAnimation)
                return;
            Root.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.6, 1, TimeSpan.FromMilliseconds(160)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            });
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
            // A window of its own (in front, or the fallback) needs to be the active one before it gets the keyboard;
            // a child of the desktop only needs the focus.
            if (Mode != PanelMode.Desktop)
                Native.SetForegroundWindow(Handle);
            Native.SetFocus(Handle);
        }

        protected void destroyWindow()
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

        public virtual void Dispose()
        {
            destroyWindow();
        }
    }
}
