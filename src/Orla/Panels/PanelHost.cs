using System;
using System.Collections.Generic;
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
    // also how panels come back after Explorer restarts. Panels always stay inside a monitor's work area and never
    // overlap each other.
    public class PanelHost : IDisposable
    {
        readonly Controller controller;
        HwndSource source;
        Desktop desktop;
        RECT rect, dragStart;
        POINT dragCursor;
        string resizeSide;
        int resizeRows;
        bool fitting;
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
            View.MoveStarted += startDrag;
            View.MoveUpdated += dragMove;
            View.MoveEnded += delegate {
                // A click on the title without dragging leaves the panel exactly where it was.
                if (rect.Left == dragStart.Left && rect.Top == dragStart.Top)
                    return;
                rect = Screens.free(Screens.snap(rect, others()), others());
                commit();
            };
            View.ResizeStarted += side => {
                resizeSide = side;
                resizeRows = Group.Rows;
                startDrag();
            };
            View.ResizeUpdated += dragResize;
            View.ResizeEnded += delegate {
                resizeSide = null;
                fitHeight();
                commit();
            };
            // Content changed (a download arrived, a reference was added): grow only into the room below.
            View.SizeNeeded += delegate {
                if (resizeSide != null || source == null || fitting)
                    return;
                fitHeight();
                place(true);
            };
        }

        int pixels(double dip) => (int)Math.Round(dip * scale);

        List<RECT> others() => controller.Hosts.Where(h => h != this).Select(h => h.Rect).ToList();

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
            scale = Screens.at((int)Group.X + 40, (int)Group.Y + 20).Scale;
            View.Scale = scale;
            if (source != null)
                applyScale();
            rect = new RECT { Left = (int)Group.X, Top = (int)Group.Y };
            fitHeight();
        }

        // Lets the height follow the content, up to the rows chosen and the room left below the panel.
        void fitHeight()
        {
            fitting = true;
            try
            {
                string size = controller.Layout.IconSize;
                int width = pixels(PanelMetrics.width(Group.Columns, size));
                var corner = new RECT { Left = rect.Left, Top = rect.Top, Right = rect.Left + width, Bottom = rect.Top + 1 };
                RECT work = Screens.forRect(corner).Work;
                int limit = work.Bottom - Screens.Margin;
                foreach (RECT o in others())
                    if (o.Top >= rect.Top && o.Left < rect.Left + width && o.Right > rect.Left)
                        limit = Math.Min(limit, o.Top - Screens.Margin);
                double room = (limit - rect.Top) / scale - PanelMetrics.ChromeHeight;
                View.RoomRows = Math.Max(1, (int)Math.Floor(room / PanelMetrics.tile(size)));
                View.applySize();
                rect.Right = rect.Left + pixels(View.Width);
                rect.Bottom = rect.Top + pixels(View.Height);
                // Only moves the panel when not even one row fits where it is.
                rect = Screens.clamp(rect);
                Group.X = rect.Left;
                Group.Y = rect.Top;
            }
            finally
            {
                fitting = false;
            }
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
            if (Mode == PanelMode.Fallback)
                placeAboveShell();
            else
                bringToFront();
            Native.ShowWindow(Handle, Native.SW_SHOWNA);
            if (controller.Layout.Animations && SystemParameters.ClientAreaAnimation)
                Motion.reveal(View);
        }

        // A child window takes the DPI of its host (the primary monitor). Panels on other monitors are scaled to
        // match the monitor they sit on; top-level windows get this from WPF itself.
        void applyScale()
        {
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

        // ---- dragging ----

        void startDrag()
        {
            Native.GetCursorPos(out dragCursor);
            dragStart = rect;
            bringToFront();
        }

        // Moving keeps the whole panel inside the work area of the monitor under the pointer.
        void dragMove()
        {
            Native.GetCursorPos(out POINT now);
            int dx = now.X - dragCursor.X, dy = now.Y - dragCursor.Y;
            var moved = new RECT { Left = dragStart.Left + dx, Top = dragStart.Top + dy, Right = dragStart.Right + dx,
                                   Bottom = dragStart.Bottom + dy };
            rect = Screens.clampTo(moved, Screens.at(now.X, now.Y).Work);
            place(false);
        }

        // Resizing moves the grabbed edges in whole icon columns and rows, stays on screen and stops at other panels.
        void dragResize()
        {
            Native.GetCursorPos(out POINT now);
            int dx = now.X - dragCursor.X, dy = now.Y - dragCursor.Y;
            bool left = resizeSide.Contains("Left"), right = resizeSide.Contains("Right");
            bool top = resizeSide.Contains("Top"), bottom = resizeSide.Contains("Bottom");
            string size = controller.Layout.IconSize;
            int columns = Group.Columns, rows = Group.Rows;
            if (left || right)
                columns = PanelMetrics.columnsFor((dragStart.Width + (right ? dx : -dx)) / scale, size);
            // Vertical drags change the most rows the panel may show, counted from where the drag began.
            if ((top || bottom) && !Group.Collapsed)
                rows = Math.Max(Group.MinRows, Math.Min(Group.MaxRows,
                                resizeRows + (int)Math.Round((bottom ? dy : -dy) / scale / PanelMetrics.tile(size))));
            int w = pixels(PanelMetrics.width(columns, size));
            int h = Group.Collapsed ? dragStart.Height : pixels(PanelMetrics.height(rows, size));
            int x = left ? dragStart.Right - w : dragStart.Left, y = top ? dragStart.Bottom - h : dragStart.Top;
            var next = new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h };
            RECT work = Screens.forRect(dragStart).Work;
            bool inside = next.Left >= work.Left + Screens.Margin && next.Right <= work.Right - Screens.Margin &&
                          next.Top >= work.Top + Screens.Margin && next.Bottom <= work.Bottom - Screens.Margin;
            if (!inside || others().Any(o => Screens.overlaps(next, o)) || columns == Group.Columns && rows == Group.Rows)
                return;
            Group.Columns = columns;
            Group.Rows = rows;
            View.RoomRows = Group.MaxRows;
            View.Width = PanelMetrics.width(columns, size);
            View.Height = Group.Collapsed ? View.Height : PanelMetrics.height(rows, size);
            rect = next;
            place(true);
        }

        void commit()
        {
            Group.X = rect.Left;
            Group.Y = rect.Top;
            double before = scale;
            scale = Screens.forRect(rect).Scale;
            View.Scale = scale;
            if (Math.Abs(before - scale) > 0.01)
                applyScale();
            fitHeight();
            place(true);
            controller.saveLayout();
        }

        // ---- lifetime ----

        public void refresh()
        {
            View.refresh();
            measure();
            place(true);
        }

        // Moves this panel off any panel it covers, used when panels are first placed or grow.
        public void settle()
        {
            RECT free = Screens.free(rect, others());
            if (free.Left == rect.Left && free.Top == rect.Top)
                return;
            rect = free;
            Group.X = rect.Left;
            Group.Y = rect.Top;
            fitHeight();
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
