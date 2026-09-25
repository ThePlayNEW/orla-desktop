using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Orla
{
    // The search bar that stays on the desktop. It searches what every panel shows: typing narrows the list, arrows
    // choose, Enter opens, Ctrl+Enter shows the item in its folder, Esc clears and then lets go of the focus. The
    // results show only while the bar has the focus; the bar itself never goes away.
    public partial class SearchBar : UserControl
    {
        const int MaxResults = 8;

        public class Hit
        {
            public TileItem Tile;
            public Group Group;
            public string Plain;
        }

        readonly Controller controller;
        Func<List<Hit>> items;
        List<Hit> shown = new List<Hit>();
        int selected;
        bool previewing;

        // Sized to the content: the host follows when the results open and close.
        public event Action SizeNeeded;

        public SearchBar(Controller controller)
        {
            this.controller = controller;
            items = controller.searchItems;
            InitializeComponent();
            applyTheme();
            Loaded += delegate {
                Theme.Changed -= applyTheme;
                Theme.GlassChanged -= applyGlass;
                Theme.Changed += applyTheme;
                Theme.GlassChanged += applyGlass;
                applyTheme();
            };
            Unloaded += delegate {
                Theme.Changed -= applyTheme;
                Theme.GlassChanged -= applyGlass;
            };
            Query.TextChanged += delegate { filter(); };
            PreviewKeyDown += key;
            // A click anywhere on the bar puts the caret in the field; the window takes the keyboard from the desktop first.
            Frame.PreviewMouseLeftButtonDown += (s, e) => {
                if (Query.IsKeyboardFocusWithin)
                    return;
                controller.focusSearch();
                Query.Focus();
                if (!(e.OriginalSource is DependencyObject d && isIn(d, Query)))
                    e.Handled = true;
            };
            Query.GotKeyboardFocus += delegate { show(); };
            Query.LostKeyboardFocus += delegate { show(); };
            SizeChanged += delegate { SizeNeeded?.Invoke(); };
        }

        static bool isIn(DependencyObject d, DependencyObject ancestor)
        {
            for (; d != null; d = VisualTreeHelper.GetParent(d))
                if (d == ancestor)
                    return true;
            return false;
        }

        // The bar merges the palette itself: WPF does not carry theme changes into windows hosted on the desktop.
        void applyTheme()
        {
            if (Theme.Palette == null)
                return;
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(Theme.Palette);
            applyGlass();
        }

        void applyGlass()
        {
            Resources["Brush.PanelGlass"] = Theme.Glass;
        }

        public bool Active => Query.IsKeyboardFocusWithin;

        // The height of the field alone, unscaled, whatever is open: the space panels keep clear of.
        public double IdleHeight
        {
            get
            {
                Visibility drop = Drop.Visibility;
                Transform scale = LayoutTransform;
                Drop.Visibility = Visibility.Collapsed;
                LayoutTransform = Transform.Identity;
                Measure(new Size(Width, double.PositiveInfinity));
                double height = DesiredSize.Height;
                Drop.Visibility = drop;
                LayoutTransform = scale;
                return height;
            }
        }

        // Takes the focus, adding what was typed on a panel before the field had it.
        public void begin(string more)
        {
            Query.Focus();
            if (!String.IsNullOrEmpty(more))
                Query.Text += more;
            Query.CaretIndex = Query.Text.Length;
        }

        // Back to an empty, idle bar.
        public void reset()
        {
            Query.Text = "";
            if (Query.IsKeyboardFocusWithin)
                Keyboard.ClearFocus();
            show();
        }

        // For documentation images: sample items and a query, with the results open as if in use.
        internal void preview(string text, List<Hit> sample)
        {
            items = () => sample;
            previewing = true;
            Query.Text = text;
        }

        // The results, or that nothing matched, while in use.
        void show()
        {
            bool active = Query.IsKeyboardFocusWithin || previewing;
            Placeholder.Visibility = Query.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Drop.Visibility = active && Query.Text.Trim().Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Best first: names that start with the text, then a word that does, then any match; shorter names first.
        void filter()
        {
            string q = Organizer.plain(Query.Text.Trim());
            shown = q.Length == 0 ? new List<Hit>()
                  : items().Select(h => new { Hit = h, Rank = Organizer.rank(h.Plain, q) })
                              .Where(x => x.Rank >= 0)
                              .OrderBy(x => x.Rank).ThenBy(x => x.Hit.Plain.Length).ThenBy(x => x.Hit.Plain)
                              .Take(MaxResults).Select(x => x.Hit).ToList();
            selected = 0;
            Nothing.Visibility = q.Length > 0 && shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Nothing.Text = Text.format("search.nothing", Query.Text.Trim());
            draw();
            show();
        }

        void draw()
        {
            Results.Children.Clear();
            for (int i = 0; i < shown.Count; i++)
            {
                Hit hit = shown[i];
                int index = i;
                var row = new Border { CornerRadius = (CornerRadius)FindResource("Radius.Item"), Padding = new Thickness(10, 6, 12, 6),
                                       Cursor = Cursors.Hand, ToolTip = Shell.isVirtual(hit.Tile.Path) ? null : hit.Tile.Path };
                paint(row, i == selected ? "Brush.ItemSelected" : null);
                row.MouseEnter += delegate { if (index != selected) paint(row, "Brush.ItemHover"); };
                row.MouseLeave += delegate { paint(row, index == selected ? "Brush.ItemSelected" : null); };
                row.MouseLeftButtonUp += delegate { selected = index; run(false); };
                var line = new DockPanel();
                // The chosen row carries the accent mark Windows lists use, not just a fill.
                var mark = new Border { Width = 3, Height = 16, CornerRadius = new CornerRadius(1.5), Margin = new Thickness(-6, 0, 3, 0),
                                        Visibility = i == selected ? Visibility.Visible : Visibility.Hidden };
                mark.SetResourceReference(Border.BackgroundProperty, "Brush.Accent");
                DockPanel.SetDock(mark, Dock.Left);
                line.Children.Add(mark);
                var icon = new Image { Source = hit.Tile.Icon, Width = 24, Height = 24, Margin = new Thickness(0, 0, 12, 0) };
                DockPanel.SetDock(icon, Dock.Left);
                line.Children.Add(icon);
                var where = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var dot = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(12, 0, 6, 0) };
                dot.SetResourceReference(Shape.FillProperty, "Brush.Tint." + hit.Group.Tint);
                var panel = new TextBlock { Text = hit.Group.Name, FontSize = 12, MaxWidth = 160, TextTrimming = TextTrimming.CharacterEllipsis };
                panel.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
                where.Children.Add(dot);
                where.Children.Add(panel);
                DockPanel.SetDock(where, Dock.Right);
                line.Children.Add(where);
                var name = new TextBlock { Text = hit.Tile.Label, FontSize = 14, VerticalAlignment = VerticalAlignment.Center,
                                           TextTrimming = TextTrimming.CharacterEllipsis };
                name.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
                line.Children.Add(name);
                row.Child = line;
                Results.Children.Add(row);
            }
        }

        static void paint(Border row, string brush)
        {
            if (brush == null)
                row.Background = Brushes.Transparent;
            else
                row.SetResourceReference(Border.BackgroundProperty, brush);
        }

        void key(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.Escape:
                // First Esc clears the text; the next one lets go, and sends the panels back if they came in front.
                if (Query.Text.Length > 0)
                    Query.Text = "";
                else
                    controller.leaveSearch();
                break;
            case Key.Down:
            case Key.Up:
            case Key.Tab:
                // Nothing else here takes focus, so Tab moves through the results as the arrows do.
                bool up = e.Key == Key.Up || e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                if (shown.Count > 0)
                {
                    selected = (selected + (up ? shown.Count - 1 : 1)) % shown.Count;
                    draw();
                    System.Windows.Automation.AutomationProperties.SetHelpText(Query, shown[selected].Tile.Label + ", " + shown[selected].Group.Name);
                }
                break;
            case Key.Enter:
                run((Keyboard.Modifiers & ModifierKeys.Control) != 0);
                break;
            default:
                return;
            }
            e.Handled = true;
        }

        void run(bool reveal)
        {
            if (selected >= shown.Count)
                return;
            TileItem tile = shown[selected].Tile;
            reset();
            if (reveal && !Shell.isVirtual(tile.Path))
                controller.reveal(tile.Path);
            else
                controller.open(tile.Path);
        }
    }
}
