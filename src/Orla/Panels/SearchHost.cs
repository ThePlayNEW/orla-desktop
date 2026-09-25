using System.Windows;

namespace Orla
{
    // Holds the search bar at the top of the main screen, centred over the middle the panels leave free, the same
    // distance from the edge as every panel. It follows the panels between the desktop and the front.
    public class SearchHost : LayerWindow
    {
        public SearchBar Bar { get; }
        protected override FrameworkElement Root => Bar;
        protected override string Title => "Orla · " + Text.get("search.placeholder");

        public SearchHost(Controller controller) : base(controller)
        {
            Bar = new SearchBar(controller);
            // The results open below the field: the window grows down to fit them and shrinks back after.
            Bar.SizeNeeded += delegate {
                if (source == null)
                    return;
                rect.Bottom = rect.Top + pixels(Bar.ActualHeight);
                // Open results reach over the panels below, so the bar comes above them first.
                bringToFront();
                place(true);
            };
        }

        // Where the idle bar sits, which panels keep clear of.
        public RECT Resting { get; private set; }

        public void show(PanelMode mode, Desktop target, bool shown)
        {
            destroyWindow();
            desktop = target;
            Mode = mode;
            visible = shown;
            Screen main = Screens.primary();
            scale = main.Scale;
            int width = pixels(Bar.Width), left = main.Work.Left + (main.Work.Width - width) / 2, top = main.Work.Top + Screens.Edge;
            Resting = new RECT { Left = left, Top = top, Right = left + width, Bottom = top + pixels(Bar.IdleHeight) };
            // Open results, if any, resize the window as soon as the bar is laid out.
            rect = Resting;
            create();
        }
    }
}
