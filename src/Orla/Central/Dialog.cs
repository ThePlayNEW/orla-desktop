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

        public static bool confirm(string title, string message, string action)
        {
            bool accepted = false;
            build(title, message, null, action, delegate {
                accepted = true;
                return true;
            }).ShowDialog();
            return accepted;
        }

        public static void alert(string title, string message)
        {
            build(title, message, null, Text.get("dialog.ok"), () => true, false).ShowDialog();
        }

        static Window build(string title, string message, UIElement body, string action, Func<bool> accept,
                            bool cancellable = true)
        {
            var w = new Window {
                Title = title, Width = 400, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow, ShowInTaskbar = false, Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            w.SetResourceReference(Control.BackgroundProperty, "Brush.Surface");
            var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
            var heading = new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            heading.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            heading.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Display");
            panel.Children.Add(heading);
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
            ok.SetResourceReference(FrameworkElement.StyleProperty, "Orla.Button.Accent");
            ok.Click += delegate {
                if (accept())
                    w.DialogResult = true;
            };
            if (cancellable)
                buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            panel.Children.Add(buttons);
            w.Content = panel;
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
