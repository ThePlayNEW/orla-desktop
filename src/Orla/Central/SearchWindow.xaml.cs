using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace Orla
{
    // Quick search over what every panel shows. Typing narrows the list; arrows choose, Enter opens, Ctrl+Enter shows
    // the item in its folder, Esc closes. Clicking anywhere else closes it too.
    public partial class SearchWindow : Window
    {
        const int MaxResults = 8;

        public class Hit
        {
            public TileItem Tile;
            public Group Group;
            public string Plain;
        }

        readonly Controller controller;
        readonly List<Hit> all;
        List<Hit> shown = new List<Hit>();
        int selected;

        public SearchWindow(Controller controller, List<Hit> items, string first)
        {
            this.controller = controller;
            all = items;
            InitializeComponent();
            new WindowInteropHelper(this).Owner = Dialog.OwnerHandle;
            Rect work = SystemParameters.WorkArea;
            Left = work.Left + (work.Width - Width) / 2;
            Top = work.Top + work.Height * 0.12;
            Query.TextChanged += delegate { filter(); };
            PreviewKeyDown += key;
            // Closing loses the focus, which fires Deactivated in the middle of closing; whatever started the close,
            // the window knows it is going and does not close twice.
            Closing += delegate { closing = true; };
            Deactivated += delegate { close(); };
            Loaded += delegate {
                Query.Focus();
                Query.Text = first ?? "";
                Query.CaretIndex = Query.Text.Length;
                filter();
            };
        }

        // More typing that reached a panel before the field had focus.
        public void type(string more)
        {
            if (String.IsNullOrEmpty(more))
                return;
            Query.Text += more;
            Query.CaretIndex = Query.Text.Length;
        }

        bool closing;

        public void close()
        {
            if (closing)
                return;
            closing = true;
            Close();
        }

        // Best first: names that start with the text, then a word that does, then any match; shorter names first.
        void filter()
        {
            string q = Organizer.plain(Query.Text.Trim());
            Placeholder.Visibility = Query.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            shown = q.Length == 0 ? new List<Hit>()
                  : all.Select(h => new { Hit = h, Rank = Organizer.rank(h.Plain, q) })
                       .Where(x => x.Rank >= 0)
                       .OrderBy(x => x.Rank).ThenBy(x => x.Hit.Plain.Length).ThenBy(x => x.Hit.Plain)
                       .Take(MaxResults).Select(x => x.Hit).ToList();
            selected = 0;
            Nothing.Visibility = q.Length > 0 && shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Nothing.Text = Text.format("search.nothing", Query.Text.Trim());
            draw();
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
                row.Background = System.Windows.Media.Brushes.Transparent;
            else
                row.SetResourceReference(Border.BackgroundProperty, brush);
        }

        void key(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.Escape:
                close();
                controller.leaveOverlay();
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
            close();
            if (reveal && !Shell.isVirtual(tile.Path))
                controller.reveal(tile.Path);
            else
                controller.open(tile.Path);
        }
    }
}
