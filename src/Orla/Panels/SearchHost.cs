using System;
using System.Collections.Generic;
using System.Linq;

namespace Orla
{
    // Holds the search bar on the desktop: at the top of the main screen, centred over the middle the panels leave
    // free, until people move it by its grip. The window is always as tall as the bar with a full list of results,
    // clear below while closed, so opening the results never covers a panel and panels always keep clear of it.
    public class SearchHost : LayerWindow
    {
        public SearchBar Bar { get; }
        protected override System.Windows.FrameworkElement Root => Bar;
        protected override string Title => "Orla · " + Text.get("search.placeholder");

        RECT dragStart;
        POINT dragCursor;

        public SearchHost(Controller controller) : base(controller)
        {
            Bar = new SearchBar(controller);
            Bar.MoveStarted += delegate {
                Native.GetCursorPos(out dragCursor);
                dragStart = rect;
                bringToFront();
            };
            Bar.MoveUpdated += dragMove;
            Bar.MoveEnded += delegate {
                if (rect.Left == dragStart.Left && rect.Top == dragStart.Top)
                    return;
                // At the size it has on the screen it landed on, then clear of the panels there.
                measure(rect.Left, rect.Top);
                applyScale();
                rect = Screens.free(Screens.snap(rect, others()), others());
                controller.Layout.SearchX = rect.Left;
                controller.Layout.SearchY = rect.Top;
                place(true);
                controller.saveLayout();
            };
        }

        List<RECT> others() => controller.Hosts.Select(h => h.Rect).ToList();

        int idle;

        // The field alone, where the bar shows while nothing is open.
        public RECT Field => new RECT { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Top + idle };

        // After the panels are laid out: where the bar's room would cover one, the bar moves to the nearest free place
        // instead of pushing panels away. Not saved, so it goes back when room returns.
        public void yieldTo(IList<RECT> panels)
        {
            if (!panels.Any(p => Screens.overlaps(p, rect)))
                return;
            rect = Screens.free(rect, panels);
            place(true);
        }

        // Follows the pointer inside the screen under it, sticking to the screen edge and to the panels like a panel does.
        void dragMove()
        {
            Native.GetCursorPos(out POINT now);
            int dx = now.X - dragCursor.X, dy = now.Y - dragCursor.Y;
            var moved = new RECT { Left = dragStart.Left + dx, Top = dragStart.Top + dy, Right = dragStart.Right + dx, Bottom = dragStart.Bottom + dy };
            RECT work = Screens.at(now.X, now.Y).Work;
            rect = Screens.clampTo(Screens.magnet(Screens.clampTo(moved, work), others(), work), work);
            place(false);
        }

        public void show(PanelMode mode, Desktop target, bool shown)
        {
            destroyWindow();
            desktop = target;
            Mode = mode;
            visible = shown;
            Layout layout = controller.Layout;
            if (layout.SearchX is double x && layout.SearchY is double y)
                measure((int)x, (int)y);
            else
            {
                // Centred at the top of the main screen, the same distance from the edge as every panel.
                Screen main = Screens.primary();
                scale = main.Scale;
                int width = pixels(Bar.Width);
                measure(main.Work.Left + (main.Work.Width - width) / 2, main.Work.Top + Screens.Edge);
            }
            create();
        }

        // The window's size at the scale of the screen it sits on, kept whole on that screen.
        void measure(int left, int top)
        {
            scale = Screens.at(left + 40, top + 20).Scale;
            idle = pixels(Bar.IdleHeight);
            rect = Screens.clamp(new RECT { Left = left, Top = top, Right = left + pixels(Bar.Width), Bottom = top + pixels(Bar.FullHeight) });
        }
    }
}
