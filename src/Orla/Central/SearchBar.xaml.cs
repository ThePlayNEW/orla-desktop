using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Orla
{
    // The search bar that stays on the desktop. It searches what every panel shows: typing narrows the list, arrows
    // choose, Enter opens, Ctrl+Enter shows the item in its folder, Shift+Enter shows it in its panel, Esc clears and
    // then lets go of the focus. The results show only while the bar has the focus; the bar itself never goes away and
    // moves by its grip like a panel by its title.
    public partial class SearchBar : UserControl
    {
        public const int MaxResults = 8;
        // Every result row has the same height, so the room the bar keeps below it is known in advance.
        const double RowHeight = 36;

        public class Hit
        {
            public TileItem Tile;
            public Group Group;
            public string Plain;
        }

        readonly Controller controller;
        Func<List<Hit>> items;
        List<Hit> shown = new List<Hit>();
        // The rows on screen, built once per search and only restyled as the choice or the pointer moves: rebuilding
        // under the pointer would lose clicks and tooltips.
        readonly List<(Border Row, UIElement Mark, UIElement Actions)> rows = new List<(Border, UIElement, UIElement)>();
        int selected, hovered = -1;
        bool previewing, moving, menuOpen;

        public event Action MoveStarted, MoveUpdated, MoveEnded;

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
            // A click on the field or its padding puts the caret there; the window takes the keyboard from the desktop first.
            Frame.PreviewMouseLeftButtonDown += (s, e) => {
                var d = e.OriginalSource as DependencyObject;
                if (Query.IsKeyboardFocusWithin || isIn(d, Grip) || isIn(d, Drop))
                    return;
                controller.focusSearch();
                Query.Focus();
                if (!isIn(d, Query))
                    e.Handled = true;
            };
            Query.GotKeyboardFocus += delegate { show(); };
            Query.LostKeyboardFocus += delegate { show(); };

            Grip.MouseLeftButtonDown += (s, e) => {
                if (controller.Layout.LockLayout)
                    return;
                moving = Grip.CaptureMouse();
                if (moving)
                    MoveStarted?.Invoke();
                e.Handled = true;
            };
            Grip.MouseMove += delegate {
                if (moving)
                    MoveUpdated?.Invoke();
            };
            Grip.MouseLeftButtonUp += delegate { endMove(); };
            Grip.LostMouseCapture += delegate { endMove(); };
            Grip.MouseRightButtonUp += (s, e) => {
                var menu = new ContextMenu { PlacementTarget = Grip, Placement = PlacementMode.Bottom };
                menu.Items.Add(Menus.item("search.resetPlace", "Glyph.Search", controller.resetSearchPlace));
                menu.IsOpen = true;
                e.Handled = true;
            };
            refresh();
        }

        void endMove()
        {
            if (!moving)
                return;
            moving = false;
            Grip.ReleaseMouseCapture();
            MoveEnded?.Invoke();
        }

        static bool isIn(DependencyObject d, DependencyObject ancestor)
        {
            for (; d != null; d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d))
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

        // Settings the bar follows: a locked layout hides the grip.
        public void refresh()
        {
            Grip.Visibility = controller.Layout.LockLayout ? Visibility.Collapsed : Visibility.Visible;
        }

        public bool Active => Query.IsKeyboardFocusWithin;

        // The field alone, unscaled: where the bar is when nothing is open.
        public double IdleHeight
        {
            get
            {
                Visibility drop = Drop.Visibility;
                Transform scale = LayoutTransform;
                Drop.Visibility = Visibility.Collapsed;
                LayoutTransform = Transform.Identity;
                Measure(new Size(Width, double.PositiveInfinity));
                double idle = DesiredSize.Height;
                Drop.Visibility = drop;
                LayoutTransform = scale;
                return idle;
            }
        }

        // The field with a full list of results open: the room the bar holds on the desktop, so opening the results
        // never covers a panel. The shoreline (12), the rows, and the line about the keys (up to two lines).
        public double FullHeight => IdleHeight + 12 + MaxResults * RowHeight + 48;

        // Takes the focus, adding what was typed on a panel before the field had it. A bar just rebuilt (when the
        // panels come in front) is not ready for the keyboard until it is loaded.
        public void begin(string more)
        {
            if (!IsLoaded)
            {
                RoutedEventHandler once = null;
                once = delegate {
                    Loaded -= once;
                    Dispatcher.BeginInvoke(new Action(() => begin(more)), System.Windows.Threading.DispatcherPriority.Input);
                };
                Loaded += once;
                return;
            }
            controller.focusSearch();
            Query.Focus();
            Keyboard.Focus(Query);
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
            // A result's menu takes the keyboard while open; the list stays under it.
            bool active = Query.IsKeyboardFocusWithin || previewing || menuOpen;
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
            hovered = -1;
            Nothing.Visibility = q.Length > 0 && shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Nothing.Text = Text.format("search.nothing", Query.Text.Trim());
            draw();
            show();
        }

        void draw()
        {
            Results.Children.Clear();
            rows.Clear();
            for (int i = 0; i < shown.Count; i++)
                Results.Children.Add(row(shown[i], i));
            restyle();
        }

        // The chosen row carries the fill and the accent mark; the chosen and the pointed row show their actions.
        void restyle()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                paint(rows[i].Row, i == selected ? "Brush.ItemSelected" : i == hovered ? "Brush.ItemHover" : null);
                rows[i].Mark.Visibility = i == selected ? Visibility.Visible : Visibility.Hidden;
                rows[i].Actions.Visibility = i == selected || i == hovered ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // One result: its icon and name, the panel it is in, and, on the chosen or pointed row, what can be done with it.
        FrameworkElement row(Hit hit, int index)
        {
            var row = new Border { Height = RowHeight, CornerRadius = (CornerRadius)FindResource("Radius.Item"), Padding = new Thickness(10, 0, 4, 0),
                                   Cursor = Cursors.Hand, ToolTip = Shell.isVirtual(hit.Tile.Path) ? null : hit.Tile.Path };
            row.MouseEnter += delegate {
                hovered = index;
                restyle();
            };
            row.MouseLeave += delegate {
                if (hovered != index)
                    return;
                hovered = -1;
                restyle();
            };
            row.MouseLeftButtonUp += (s, e) => {
                if (e.Handled)
                    return;
                act(hit, Act.Open);
            };
            row.MouseRightButtonUp += (s, e) => {
                ContextMenu menu = menuFor(hit, row);
                menuOpen = true;
                menu.Closed += delegate {
                    menuOpen = false;
                    show();
                };
                menu.IsOpen = true;
                e.Handled = true;
            };
            var line = new DockPanel();
            // The chosen row carries the accent mark Windows lists use, not just a fill.
            var mark = new Border { Width = 3, Height = 16, CornerRadius = new CornerRadius(1.5), Margin = new Thickness(-6, 0, 3, 0) };
            mark.SetResourceReference(Border.BackgroundProperty, "Brush.Accent");
            DockPanel.SetDock(mark, Dock.Left);
            line.Children.Add(mark);
            var icon = new Image { Source = hit.Tile.Icon, Width = 24, Height = 24, Margin = new Thickness(0, 0, 12, 0) };
            DockPanel.SetDock(icon, Dock.Left);
            line.Children.Add(icon);
            // What can be done with it, where the pointer or the keyboard is.
            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            actions.Children.Add(button("Glyph.Open", "search.open", () => act(hit, Act.Open)));
            if (!Shell.isVirtual(hit.Tile.Path))
                actions.Children.Add(button("Glyph.Reveal", "search.reveal", () => act(hit, Act.Reveal)));
            actions.Children.Add(button("Glyph.PanelCollection", "search.showInPanel", () => act(hit, Act.InPanel)));
            DockPanel.SetDock(actions, Dock.Right);
            line.Children.Add(actions);
            var where = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var dot = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(12, 0, 6, 0) };
            dot.SetResourceReference(Shape.FillProperty, "Brush.Tint." + hit.Group.Tint);
            var panel = new TextBlock { Text = hit.Group.Name, FontSize = 12, MaxWidth = 140, TextTrimming = TextTrimming.CharacterEllipsis,
                                        Margin = new Thickness(0, 0, 8, 0) };
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
            rows.Add((row, mark, actions));
            return row;
        }

        // Row buttons never take the focus: the field keeps it, so the results stay open while they are used.
        Button button(string glyph, string label, System.Action run)
        {
            var b = new Button { Focusable = false, ToolTip = Text.get(label), Margin = new Thickness(2, 0, 0, 0),
                                 Content = Menus.glyph(glyph) };
            b.SetResourceReference(StyleProperty, "Orla.IconButton");
            System.Windows.Automation.AutomationProperties.SetName(b, Text.get(label));
            b.Click += (s, e) => run();
            b.MouseLeftButtonUp += (s, e) => e.Handled = true;
            return b;
        }

        ContextMenu menuFor(Hit hit, UIElement target)
        {
            var menu = new ContextMenu { PlacementTarget = target, Placement = PlacementMode.MousePoint };
            menu.Items.Add(Menus.item("search.open", "Glyph.Open", () => act(hit, Act.Open), "Enter"));
            if (!Shell.isVirtual(hit.Tile.Path))
                menu.Items.Add(Menus.item("search.reveal", "Glyph.Reveal", () => act(hit, Act.Reveal), "Ctrl+Enter"));
            menu.Items.Add(Menus.item("search.showInPanel", "Glyph.PanelCollection", () => act(hit, Act.InPanel), "Shift+Enter"));
            if (!Shell.isVirtual(hit.Tile.Path))
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(Menus.item("search.copyPath", "Glyph.ItemFile", () => controller.tryAction(() => Clipboard.SetText(hit.Tile.Path))));
            }
            return menu;
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
                    restyle();
                    System.Windows.Automation.AutomationProperties.SetHelpText(Query, shown[selected].Tile.Label + ", " + shown[selected].Group.Name);
                }
                break;
            case Key.Enter:
                if (selected < shown.Count)
                    act(shown[selected], (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? Act.Reveal
                                         : (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? Act.InPanel : Act.Open);
                break;
            default:
                return;
            }
            e.Handled = true;
        }

        enum Act
        {
            Open,
            Reveal,
            InPanel
        }

        void act(Hit hit, Act what)
        {
            TileItem tile = hit.Tile;
            reset();
            if (what == Act.InPanel)
                controller.showInPanel(tile);
            else if (what == Act.Reveal && !Shell.isVirtual(tile.Path))
                controller.reveal(tile.Path);
            else
                controller.open(tile.Path);
        }
    }
}
