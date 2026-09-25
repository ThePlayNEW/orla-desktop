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
    public class PanelHost : LayerWindow
    {
        RECT dragStart;
        POINT dragCursor;
        string resizeSide;
        RECT resizeLimits;
        bool fitting;
        System.Windows.Threading.DispatcherTimer settleAnimation;
        RECT animationTarget;
        Action animationDone;

        public PanelView View { get; }
        public Group Group => View.Group;
        protected override FrameworkElement Root => View;
        protected override string Title => "Orla · " + Group.Name;

        public PanelHost(Controller controller, Group group) : base(controller)
        {
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
                startDrag();
                resizeSide = side;
                resizeLimits = limitsForResize();
                View.Resizing = true;
            };
            View.ResizeUpdated += dragResize;
            View.ResizeEnded += endResize;
            // Content changed (a download arrived, a reference was added): grow only into the room below.
            View.SizeNeeded += delegate {
                if (resizeSide != null || source == null || fitting)
                    return;
                fitHeight();
                place(true);
            };
        }

        List<RECT> others() => controller.Hosts.Where(h => h != this).Select(h => h.Rect).Concat(controller.SearchSpace).ToList();

        public void show(PanelMode mode, Desktop target, bool shown)
        {
            finishAnimation();
            destroyWindow();
            desktop = target;
            Mode = mode;
            visible = shown;
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
                int limit = work.Bottom - Screens.Edge;
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

        // ---- dragging ----

        void startDrag()
        {
            // A new drag while the previous resize is still gliding into place: land it first.
            finishAnimation();
            Native.GetCursorPos(out dragCursor);
            dragStart = rect;
            bringToFront();
        }

        // Moving keeps the whole panel inside the work area of the monitor under the pointer, and lets it stick to
        // screen margins and neighbouring panels on the way.
        void dragMove()
        {
            Native.GetCursorPos(out POINT now);
            int dx = now.X - dragCursor.X, dy = now.Y - dragCursor.Y;
            var moved = new RECT { Left = dragStart.Left + dx, Top = dragStart.Top + dy, Right = dragStart.Right + dx,
                                   Bottom = dragStart.Bottom + dy };
            RECT work = Screens.at(now.X, now.Y).Work;
            rect = Screens.clampTo(Screens.magnet(Screens.clampTo(moved, work), others(), work), work);
            place(false);
        }

        // How far each edge may travel: the work area margin, and the nearest panel on that side.
        RECT limitsForResize()
        {
            RECT work = Screens.forRect(dragStart).Work;
            var l = new RECT { Left = work.Left + Screens.Edge, Top = work.Top + Screens.Edge, Right = work.Right - Screens.Edge,
                               Bottom = work.Bottom - Screens.Edge };
            foreach (RECT o in others())
            {
                bool rowsOverlap = o.Top < dragStart.Bottom && o.Bottom > dragStart.Top;
                bool columnsOverlap = o.Left < dragStart.Right && o.Right > dragStart.Left;
                if (rowsOverlap && o.Right <= dragStart.Left)
                    l.Left = Math.Max(l.Left, o.Right + Screens.Margin);
                if (rowsOverlap && o.Left >= dragStart.Right)
                    l.Right = Math.Min(l.Right, o.Left - Screens.Margin);
                if (columnsOverlap && o.Bottom <= dragStart.Top)
                    l.Top = Math.Max(l.Top, o.Bottom + Screens.Margin);
                if (columnsOverlap && o.Top >= dragStart.Bottom)
                    l.Bottom = Math.Min(l.Bottom, o.Top - Screens.Margin);
            }
            return l;
        }

        // While resizing, the panel follows the pointer smoothly and shows its size in columns and rows; on release it
        // settles on whole tiles. Edges stop at the screen margin and at neighbouring panels.
        void dragResize()
        {
            if (resizeSide == null)
                return;
            Native.GetCursorPos(out POINT now);
            int dx = now.X - dragCursor.X, dy = now.Y - dragCursor.Y;
            string size = controller.Layout.IconSize;
            int minW = pixels(PanelMetrics.width(Group.MinColumns, size)), maxW = pixels(PanelMetrics.width(Group.MaxColumns, size));
            int minH = pixels(PanelMetrics.height(Group.MinRows, size)), maxH = pixels(PanelMetrics.height(Group.MaxRows, size));
            RECT r = dragStart;
            if (resizeSide.Contains("Left"))
                r.Left = clamp(dragStart.Left + dx, Math.Max(resizeLimits.Left, dragStart.Right - maxW), dragStart.Right - minW);
            if (resizeSide.Contains("Right"))
                r.Right = clamp(dragStart.Right + dx, dragStart.Left + minW, Math.Min(resizeLimits.Right, dragStart.Left + maxW));
            if (!Group.Collapsed && resizeSide.Contains("Top"))
                r.Top = clamp(dragStart.Top + dy, Math.Max(resizeLimits.Top, dragStart.Bottom - maxH), dragStart.Bottom - minH);
            if (!Group.Collapsed && resizeSide.Contains("Bottom"))
                r.Bottom = clamp(dragStart.Bottom + dy, dragStart.Top + minH, Math.Min(resizeLimits.Bottom, dragStart.Top + maxH));
            if (others().Any(o => Screens.overlaps(r, o)))
                return;
            rect = r;
            View.Width = rect.Width / scale;
            View.Height = rect.Height / scale;
            View.showSize(PanelMetrics.columnsFor(View.Width, size), Group.Collapsed ? 0 : PanelMetrics.rowsFor(View.Height, size));
            place(true);
        }

        static int clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

        void endResize()
        {
            if (resizeSide == null)
                return;
            // A click on an edge without moving (or the first half of a double-click) changes nothing.
            if (rect.Left == dragStart.Left && rect.Top == dragStart.Top && rect.Right == dragStart.Right && rect.Bottom == dragStart.Bottom)
            {
                resizeSide = null;
                View.Resizing = false;
                View.showSize(0, 0);
                return;
            }
            string size = controller.Layout.IconSize;
            bool vertical = !Group.Collapsed && (resizeSide.Contains("Top") || resizeSide.Contains("Bottom"));
            int columns = PanelMetrics.columnsFor(rect.Width / scale, size);
            int rows = vertical ? PanelMetrics.rowsFor(rect.Height / scale, size) : Group.Rows;
            // Rounding up can cross a limit; step back a tile when it does.
            RECT target;
            while (true)
            {
                int w = pixels(PanelMetrics.width(columns, size));
                int h = vertical ? pixels(PanelMetrics.height(rows, size)) : rect.Height;
                int x = resizeSide.Contains("Left") ? rect.Right - w : rect.Left;
                int y = resizeSide.Contains("Top") ? rect.Bottom - h : rect.Top;
                target = new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h };
                bool fits = target.Left >= resizeLimits.Left && target.Right <= resizeLimits.Right && target.Top >= resizeLimits.Top &&
                            target.Bottom <= resizeLimits.Bottom && !others().Any(o => Screens.overlaps(target, o));
                if (fits || columns <= Group.MinColumns && rows <= Group.MinRows)
                    break;
                // Give back a tile on the side that crossed the limit.
                bool wide = target.Left < resizeLimits.Left || target.Right > resizeLimits.Right || target.Width > rect.Width;
                bool tall = target.Top < resizeLimits.Top || target.Bottom > resizeLimits.Bottom || target.Height > rect.Height;
                if (wide && columns > Group.MinColumns)
                    columns--;
                else if (tall && rows > Group.MinRows)
                    rows--;
                else if (columns > Group.MinColumns)
                    columns--;
                else
                    rows--;
            }
            Group.Columns = columns;
            if (vertical)
            {
                Group.Rows = rows;
                Group.AutoHeight = false;
            }
            View.showSize(0, 0);
            animateTo(target, delegate {
                resizeSide = null;
                View.Resizing = false;
                commit();
            });
        }

        // A short eased glide into the final size; instant when animations are off.
        void finishAnimation()
        {
            if (settleAnimation == null || !settleAnimation.IsEnabled)
                return;
            settleAnimation.Stop();
            rect = animationTarget;
            View.Width = rect.Width / scale;
            View.Height = rect.Height / scale;
            place(true);
            Action done = animationDone;
            animationDone = null;
            done?.Invoke();
        }

        void animateTo(RECT target, Action done)
        {
            finishAnimation();
            animationTarget = target;
            animationDone = done;
            if (!controller.Layout.Animations || !SystemParameters.ClientAreaAnimation)
            {
                rect = target;
                place(true);
                done();
                return;
            }
            RECT from = rect;
            int frame = 0;
            const int frames = 7;
            settleAnimation = new System.Windows.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(16),
                                                                            System.Windows.Threading.DispatcherPriority.Render,
                                                                            delegate {
                frame++;
                double t = 1 - Math.Pow(1 - frame / (double)frames, 3);
                rect = new RECT { Left = lerp(from.Left, target.Left, t), Top = lerp(from.Top, target.Top, t),
                                  Right = lerp(from.Right, target.Right, t), Bottom = lerp(from.Bottom, target.Bottom, t) };
                View.Width = rect.Width / scale;
                View.Height = rect.Height / scale;
                place(true);
                if (frame < frames)
                    return;
                settleAnimation.Stop();
                animationDone = null;
                done();
            }, View.Dispatcher);
            settleAnimation.Start();
        }

        static int lerp(int a, int b, double t) => (int)Math.Round(a + (b - a) * t);

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

        public override void Dispose()
        {
            settleAnimation?.Stop();
            base.Dispose();
        }
    }
}
