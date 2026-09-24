using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Markup;

namespace Orla
{
    public static class UI
    {
        static ControlTemplate buttonTemplate;
        public static Brush foreground = brush("#EEF2F7"), muted = brush("#939EAF"),
                            surface = brush("#171E2A"), line = brush("#303A49"), accent = brush("#91DFC4");
        public static Brush brush(string hex)
        {
            try
            {
                var b = (SolidColorBrush) new BrushConverter().ConvertFromString(hex);
                b.Freeze();
                return b;
            }
            catch
            {
                return Brushes.LightGray;
            }
        }
        public static void theme()
        {
            string xaml =
                "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'><Setter " +
                "Property='Width' Value='7'/><Setter Property='Background' Value='Transparent'/><Setter " +
                "Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid " +
                "Background='Transparent'><Track x:Name='PART_Track' IsDirectionReversed='True' " +
                "Orientation='{TemplateBinding Orientation}' Minimum='{TemplateBinding Minimum}' " +
                "Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' " +
                "ViewportSize='{TemplateBinding ViewportSize}'><Track.DecreaseRepeatButton><RepeatButton " +
                "Command='ScrollBar.PageUpCommand' " +
                "Opacity='0'/></Track.DecreaseRepeatButton><Track.Thumb><Thumb " +
                "MinHeight='24'><Thumb.Template><ControlTemplate TargetType='Thumb'><Border " +
                "Background='#455163' CornerRadius='3' " +
                "Margin='1'/></ControlTemplate></Thumb.Template></Thumb></" +
                "Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' " +
                "Opacity='0'/></Track.IncreaseRepeatButton></Track></Grid></ControlTemplate></" +
                "Setter.Value></" + "Setter></Style>";
            Application.Current.Resources[typeof(ScrollBar)] = (Style)XamlReader.Parse(xaml);
        }
        public static void fade(UIElement element, Layout settings)
        {
            if (!settings.Animations || !SystemParameters.ClientAreaAnimation)
                return;
            element.Opacity = 1;
            element.BeginAnimation(UIElement.OpacityProperty,
                                   new DoubleAnimation(.82, 1, TimeSpan.FromMilliseconds(140)) {
                                       EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                                       FillBehavior = FillBehavior.Stop
                                   });
        }
        public static TextBlock label(string text, double size, Brush color)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = color,
                                   TextWrapping = TextWrapping.Wrap,
                                   FontFamily = new FontFamily("Segoe UI") };
        }
        public static Button button(string text, Action action, bool primary)
        {
            var b = new Button { Content = text,
                                 Padding = new Thickness(14, 9, 14, 9),
                                 Margin = new Thickness(4, 0, 0, 0),
                                 Foreground = primary ? brush("#10211C") : foreground,
                                 Background = primary ? accent : brush("#273140"),
                                 BorderThickness = new Thickness(0),
                                 Cursor = Cursors.Hand,
                                 FontSize = 12,
                                 FontWeight = FontWeights.SemiBold };
            if (buttonTemplate == null)
                buttonTemplate = (ControlTemplate)XamlReader.Parse(
                    "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                    "TargetType='Button'><Border x:Name='b' " +
                    "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='{TemplateBinding " +
                    "Background}' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter " +
                    "HorizontalAlignment='Center' " +
                    "VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger " +
                    "Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Opacity' " +
                    "Value='0.78'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter " +
                    "TargetName='b' Property='BorderBrush' Value='#91DFC4'/><Setter TargetName='b' " +
                    "Property='BorderThickness' " +
                    "Value='1'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            b.Template = buttonTemplate;
            b.Click += delegate
            {
                action();
            };
            return b;
        }
        public static MenuItem menuItem(string label, Action action)
        {
            var m = new MenuItem { Header = label };
            m.Click += delegate
            {
                action();
            };
            return m;
        }
        public static string prompt(Window owner, string title, string value)
        {
            var w = new Window { Title = title,
                                 Width = 420,
                                 Height = 180,
                                 ResizeMode = ResizeMode.NoResize,
                                 WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                 Owner = owner,
                                 Background = surface,
                                 Foreground = foreground,
                                 ShowInTaskbar = false };
            var p = new StackPanel { Margin = new Thickness(20) };
            p.Children.Add(label(title, 15, foreground));
            var input = new TextBox { Text = value, Margin = new Thickness(0, 12, 0, 12),
                                      Padding = new Thickness(8), MaxLength = 60 };
            p.Children.Add(input);
            var ok = button("Salvar", delegate {
                if (!String.IsNullOrWhiteSpace(input.Text))
                    w.DialogResult = true;
            }, true);
            ok.IsDefault = true;
            p.Children.Add(ok);
            w.Content = p;
            w.Loaded += delegate
            {
                input.Focus();
                input.SelectAll();
            };
            return w.ShowDialog() == true ? input.Text.Trim() : null;
        }
        public static Border tile(Controller controller, Group group, Entry item, bool compact)
        {
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            ImageSource icon = null;
            try
            {
                icon = Native.icon(item.Path);
            }
            catch
            {
            }
            if (icon != null)
                stack.Children.Add(new Image { Source = icon, Width = compact ? 30 : 36,
                                               Height = compact ? 30 : 36,
                                               Margin = new Thickness(0, 2, 0, 8) });
            else
                stack.Children.Add(new System.Windows.Shapes.Path {
                    Data = Geometry.Parse("M 3,5 L 13,5 16,9 29,9 29,27 3,27 Z"), Stroke = accent,
                    StrokeThickness = 1.6, Width = 32, Height = 32, Margin = new Thickness(0, 2, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            var name = label(item.Name, 11, foreground);
            name.TextAlignment = TextAlignment.Center;
            name.MaxHeight = 18;
            name.TextWrapping = TextWrapping.NoWrap;
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            stack.Children.Add(name);
            bool exists = Native.exists(item.Path);
            var tile = new Border { Child = stack,
                                    Width = compact ? 88 : 112,
                                    Height = compact ? 84 : 104,
                                    Margin = new Thickness(3),
                                    Padding = new Thickness(7),
                                    CornerRadius = new CornerRadius(10),
                                    Background = Brushes.Transparent,
                                    Cursor = Cursors.Hand,
                                    ToolTip = item.Path + (exists ? "" : "\nCaminho não encontrado"),
                                    Opacity = exists ? 1 : .45,
                                    Focusable = true };
            System.Windows.Automation.AutomationProperties.SetName(tile, item.Name);
            Point start = new Point();
            bool armed = false;
            tile.MouseEnter += delegate
            {
                tile.Background = brush("#243143");
            };
            tile.MouseLeave += delegate
            {
                tile.Background = Brushes.Transparent;
            };
            tile.GotKeyboardFocus += delegate
            {
                tile.BorderBrush = accent;
                tile.BorderThickness = new Thickness(1);
            };
            tile.LostKeyboardFocus += delegate
            {
                tile.BorderThickness = new Thickness(0);
            };
            tile.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                start = e.GetPosition(tile);
                armed = true;
                tile.Focus();
                if (e.ClickCount == 2)
                {
                    armed = false;
                    controller.open(item);
                    e.Handled = true;
                }
            };
            tile.MouseLeftButtonUp += delegate
            {
                armed = false;
            };
            tile.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (!armed || e.LeftButton != MouseButtonState.Pressed)
                    return;
                Point now = e.GetPosition(tile);
                if (Math.Abs(now.X - start.X) + Math.Abs(now.Y - start.Y) < 9)
                    return;
                armed = false;
                var data = new DataObject("Orla.Entry", item.Id);
                DragDrop.DoDragDrop(tile, data, DragDropEffects.Move);
            };
            tile.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    controller.open(item);
                    e.Handled = true;
                }
            };
            tile.AllowDrop = true;
            tile.Drop += delegate(object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent("Orla.Entry"))
                {
                    controller.store.move((string)e.Data.GetData("Orla.Entry"), group,
                                          group.Items.IndexOf(item));
                    controller.changed();
                    e.Handled = true;
                }
            };
            var menu = new ContextMenu();
            menu.Items.Add(menuItem("Abrir", delegate { controller.open(item); }));
            menu.Items.Add(menuItem("Mostrar no Explorador", delegate {
                controller.tryAction(delegate { Native.reveal(item.Path); });
            }));
            menu.Items.Add(menuItem("Editar nome no painel", delegate {
                string newName = prompt(Window.GetWindow(tile), "Nome do item", item.Name);
                if (newName != null)
                {
                    item.Name = newName;
                    controller.changed();
                }
            }));
            var move = new MenuItem { Header = "Mover para o grupo" };
            foreach (var g in controller.store.data.Groups.Where(x => x != group))
            {
                Group target = g;
                move.Items.Add(menuItem(g.Name, delegate {
                    controller.store.move(item.Id, target, target.Items.Count);
                    controller.changed();
                }));
            }
            menu.Items.Add(move);
            menu.Items.Add(new Separator());
            menu.Items.Add(menuItem("Remover do painel (manter arquivo)", delegate {
                group.Items.Remove(item);
                controller.changed();
            }));
            tile.ContextMenu = menu;
            return tile;
        }
        public static FrameworkElement items(Controller controller, Group group, bool compact, string query)
        {
            var wrap = new WrapPanel { Margin = new Thickness(8, 4, 8, 6) };
            foreach (Entry item in group.Items.Where(
                         e => String.IsNullOrEmpty(query) ||
                              e.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0))
                wrap.Children.Add(tile(controller, group, item, compact));
            var viewer = new ScrollViewer { Content = wrap,
                                            Background = Brushes.Transparent,
                                            MinHeight = 70,
                                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            AllowDrop = true };
            viewer.DragOver += delegate(object s, DragEventArgs e)
            {
                e.Effects = e.Data.GetDataPresent("Orla.Entry")           ? DragDropEffects.Move
                            : e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link
                                                                          : DragDropEffects.None;
                e.Handled = true;
            };
            viewer.Drop += delegate(object s, DragEventArgs e)
            {
                if (e.Handled)
                    return;
                if (e.Data.GetDataPresent("Orla.Entry"))
                {
                    controller.store.move((string)e.Data.GetData("Orla.Entry"), group, group.Items.Count);
                    controller.changed();
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    foreach (string p in (string[])e.Data.GetData(DataFormats.FileDrop))
                        controller.store.add(group, p);
                    controller.changed();
                }
                e.Handled = true;
            };
            if (group.Items.Count == 0)
            {
                var grid = new Grid();
                grid.Children.Add(viewer);
                var empty = label("Arraste arquivos ou atalhos para cá", 12, muted);
                empty.HorizontalAlignment = HorizontalAlignment.Center;
                empty.VerticalAlignment = VerticalAlignment.Center;
                empty.IsHitTestVisible = false;
                grid.Children.Add(empty);
                return grid;
            }
            return viewer;
        }
    }
}
