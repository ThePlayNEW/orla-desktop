using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Orla
{
    // Compact themed dialogs for renaming and confirming. They open near the pointer, above everything, because
    // they are usually summoned from a panel on the desktop.
    public static class Dialog
    {
        // Orla's hidden message window. Without an explicit owner, WPF would use the thread's active window, which can
        // be Explorer's desktop because panels share its input, and would disable it while the dialog is open.
        public static IntPtr OwnerHandle { get; set; }

        public static string prompt(string title, string value, string hint = null)
        {
            var input = new TextBox { Text = value, MaxLength = 60, Margin = new Thickness(0, 14, 0, 0) };
            input.SetResourceReference(FrameworkElement.StyleProperty, "Orla.TextBox");
            string result = null;
            Window w = build(title, hint, input, Text.get("dialog.save"), delegate {
                if (input.Text.Trim().Length == 0)
                    return false;
                result = input.Text.Trim();
                return true;
            });
            w.Loaded += delegate {
                input.Focus();
                input.SelectAll();
            };
            w.ShowDialog();
            return result;
        }

        // danger: the action removes or resets something, and its button says so in red.
        public static bool confirm(string title, string message, string action, bool danger = false)
        {
            bool accepted = false;
            build(title, message, null, action, delegate {
                accepted = true;
                return true;
            }, true, danger).ShowDialog();
            return accepted;
        }

        public static void alert(string title, string message)
        {
            build(title, message, null, Text.get("dialog.ok"), () => true, false).ShowDialog();
        }

        // For the documentation images: the confirmation card, built but not shown.
        internal static Window sample(string title, string message, string action, bool danger) =>
            build(title, message, null, action, () => true, true, danger);

        // A card without the system frame, with the panels' corners and shadow. It can be dragged by any empty part.
        static Window build(string title, string message, UIElement body, string action, Func<bool> accept,
                            bool cancellable = true, bool danger = false)
        {
            var w = new Window {
                Title = title, Width = 448, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false, Topmost = true, WindowStartupLocation = WindowStartupLocation.Manual
            };
            w.SetResourceReference(Window.FontFamilyProperty, "Font.Text");
            var card = new Border { Margin = new Thickness(24, 16, 24, 32), Padding = new Thickness(24, 18, 16, 20), BorderThickness = new Thickness(1),
                                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 32, ShadowDepth = 8, Opacity = 0.3, Direction = 270 } };
            card.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
            card.SetResourceReference(Border.BorderBrushProperty, "Brush.Line");
            card.SetResourceReference(Border.CornerRadiusProperty, "Radius.Panel");
            card.MouseLeftButtonDown += (s, e) => {
                if (e.OriginalSource == card || e.OriginalSource is TextBlock || e.OriginalSource is StackPanel || e.OriginalSource is DockPanel)
                    w.DragMove();
            };
            var panel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var top = new DockPanel();
            var close = new Button { Content = Menus.glyph("Glyph.Close"), ToolTip = Text.get("window.close"), IsTabStop = false,
                                     VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(12, -4, -8, 0) };
            close.SetResourceReference(FrameworkElement.StyleProperty, "Orla.IconButton");
            close.Click += delegate { w.Close(); };
            ((System.Windows.Shapes.Path)close.Content).SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "Brush.TextSecondary");
            DockPanel.SetDock(close, Dock.Right);
            top.Children.Add(close);
            var heading = new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            heading.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            heading.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Display");
            top.Children.Add(heading);
            panel.Children.Add(top);
            if (!String.IsNullOrEmpty(message))
            {
                var text = new TextBlock { Text = message, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
                text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
                panel.Children.Add(text);
            }
            if (body != null)
                panel.Children.Add(body);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                                           Margin = new Thickness(0, 20, 0, 0) };
            var cancel = new Button { Content = Text.get("dialog.cancel"), IsCancel = true, Margin = new Thickness(0, 0, 8, 0) };
            cancel.SetResourceReference(FrameworkElement.StyleProperty, "Orla.Button");
            var ok = new Button { Content = action, IsDefault = true };
            ok.SetResourceReference(FrameworkElement.StyleProperty, danger ? "Orla.Button.Danger" : "Orla.Button.Accent");
            ok.Click += delegate {
                if (accept())
                    w.DialogResult = true;
            };
            if (cancellable)
                buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            panel.Children.Add(buttons);
            card.Child = panel;
            w.Content = card;
            if (OwnerHandle != IntPtr.Zero)
                new System.Windows.Interop.WindowInteropHelper(w).Owner = OwnerHandle;
            w.SourceInitialized += delegate { Controller.applyWindowTheme(w); };
            w.Loaded += delegate {
                Native.GetCursorPos(out POINT p);
                Screen s = Screens.at(p.X, p.Y);
                var source = PresentationSource.FromVisual(w);
                double scale = source?.CompositionTarget.TransformToDevice.M11 ?? 1;
                double left = p.X / scale - w.ActualWidth / 2, top = p.Y / scale - 40;
                w.Left = Math.Max(s.Work.Left / scale + 8, Math.Min(left, s.Work.Right / scale - w.ActualWidth - 8));
                w.Top = Math.Max(s.Work.Top / scale + 8, Math.Min(top, s.Work.Bottom / scale - w.ActualHeight - 8));
                w.Activate();
            };
            w.PreviewKeyDown += (s, e) => {
                if (e.Key == Key.Escape)
                    w.Close();
            };
            return w;
        }
    }
}
