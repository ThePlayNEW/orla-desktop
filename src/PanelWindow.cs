using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;

namespace Orla
{
    public class PanelWindow : Window
    {
        Controller controller;
        Group group;
        bool sizing;
        public string GroupId
        {
            get {
                return group.Id;
            }
        }
        public PanelWindow(Controller controller, Group g)
        {
            this.controller = controller;
            group = g;
            Title = "Orla · " + g.Name;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            ResizeMode = ResizeMode.NoResize;
            MinWidth = 280;
            FontFamily = new FontFamily("Segoe UI");
            SourceInitialized += delegate
            {
                new WindowInteropHelper(this).Owner = controller.shellOwner;
            };
            refresh();
        }
        public void clamp()
        {
            Rect area = SystemParameters.WorkArea;
            Width = Math.Min(Math.Max(280, group.Width), area.Width - 24);
            Height = group.Collapsed ? 64 : Math.Min(group.Height, area.Height - 24);
            Left = Math.Max(area.Left + 8, Math.Min(group.X, area.Right - Width - 8));
            Top = Math.Max(area.Top + 8, Math.Min(group.Y, area.Bottom - Height - 8));
        }
        public void refresh()
        {
            clamp();
            Color glass = Color.FromArgb(
                (byte)(255 * (SystemParameters.HighContrast ? .95 : controller.store.data.Opacity)), 17, 24,
                36);
            var outer = new Border { CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(1),
                                     BorderBrush = UI.brush("#607D8995"),
                                     Background = new SolidColorBrush(glass), Padding = new Thickness(7) };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var header = new DockPanel { Background = Brushes.Transparent, Margin = new Thickness(8, 0, 2, 0),
                                         Cursor = Cursors.SizeAll };
            var collapse = UI.button(group.Collapsed ? "+" : "−", delegate {
                group.Collapsed = !group.Collapsed;
                controller.changed();
            }, false);
            collapse.Padding = new Thickness(9, 3, 9, 3);
            collapse.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(collapse, Dock.Right);
            header.Children.Add(collapse);
            var more = UI.button("···", delegate { controller.showManager(group.Id); }, false);
            more.Padding = new Thickness(9, 3, 9, 3);
            more.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(more, Dock.Right);
            header.Children.Add(more);
            var dot = new Border { Width = 5,
                                   Height = 5,
                                   CornerRadius = new CornerRadius(3),
                                   Background = UI.brush(group.Color),
                                   Margin = new Thickness(0, 0, 10, 0),
                                   VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(dot, Dock.Left);
            header.Children.Add(dot);
            var title = UI.label(group.Name + "  " + group.Items.Count, 13, UI.foreground);
            title.FontWeight = FontWeights.SemiBold;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.TextTrimming = TextTrimming.CharacterEllipsis;
            title.TextWrapping = TextWrapping.NoWrap;
            header.Children.Add(title);
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is System.Windows.Shapes.Rectangle)
                    return;
                if (e.ClickCount == 2)
                {
                    group.Collapsed = !group.Collapsed;
                    controller.changed();
                    return;
                }
                try
                {
                    DragMove();
                    group.X = Left;
                    group.Y = Top;
                    clamp();
                    group.X = Left;
                    group.Y = Top;
                    controller.save();
                }
                catch (InvalidOperationException)
                {
                }
            };
            grid.Children.Add(header);
            if (!group.Collapsed)
            {
                var items = UI.items(controller, group, true, null);
                Grid.SetRow(items, 1);
                grid.Children.Add(items);
                var thumb = new Thumb { Width = 14,
                                        Height = 14,
                                        HorizontalAlignment = HorizontalAlignment.Right,
                                        VerticalAlignment = VerticalAlignment.Bottom,
                                        Cursor = Cursors.SizeNWSE,
                                        Opacity = .5,
                                        Background = UI.brush(group.Color) };
                Grid.SetRow(thumb, 1);
                grid.Children.Add(thumb);
                thumb.DragDelta += delegate(object s, DragDeltaEventArgs e)
                {
                    sizing = true;
                    group.Width = Math.Max(280, Math.Min(620, Width + e.HorizontalChange));
                    group.Height = Math.Max(180, Math.Min(700, Height + e.VerticalChange));
                    clamp();
                };
                thumb.DragCompleted += delegate
                {
                    if (sizing)
                    {
                        sizing = false;
                        group.Width = Width;
                        group.Height = Height;
                        controller.save();
                    }
                };
            }
            outer.Child = grid;
            Content = outer;
        }
        public void showQuietly()
        {
            if (!IsVisible)
            {
                Show();
                UI.fade(this, controller.store.data);
            }
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Native.placeAboveDesktop(new WindowInteropHelper(this).Handle);
        }
    }
}
