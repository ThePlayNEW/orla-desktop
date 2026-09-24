using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Orla
{
    // A translucent copy of the dragged tile that follows the pointer, over Orla and over any other program.
    // It never takes clicks, so the window under it still receives the drop.
    public class DragGhost
    {
        readonly Window window;

        public DragGhost(ImageSource icon, string label, int count)
        {
            var stack = new StackPanel { Width = 96 };
            var grid = new Grid { Width = 48, Height = 48, HorizontalAlignment = HorizontalAlignment.Center };
            grid.Children.Add(new Image { Source = icon, Width = 48, Height = 48 });
            if (count > 1)
            {
                var badge = new Border { CornerRadius = new CornerRadius(9), MinWidth = 18, Height = 18, Padding = new Thickness(5, 0, 5, 0),
                                         HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
                                         Margin = new Thickness(0, -4, -8, 0),
                                         Child = new TextBlock { Text = count.ToString(), FontSize = 11, FontWeight = FontWeights.SemiBold,
                                                                 VerticalAlignment = VerticalAlignment.Center,
                                                                 HorizontalAlignment = HorizontalAlignment.Center } };
                badge.SetResourceReference(Border.BackgroundProperty, "Brush.Accent");
                ((TextBlock)badge.Child).SetResourceReference(TextBlock.ForegroundProperty, "Brush.OnAccent");
                grid.Children.Add(badge);
            }
            stack.Children.Add(grid);
            var text = new TextBlock { Text = label, FontSize = 12, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
                                       Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(6, 1, 6, 2) };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            var plate = new Border { CornerRadius = new CornerRadius(6), HorizontalAlignment = HorizontalAlignment.Center, Child = text };
            plate.SetResourceReference(Border.BackgroundProperty, "Brush.PanelGlass");
            stack.Children.Add(plate);
            window = new Window { WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
                                  Topmost = true, ShowInTaskbar = false, ShowActivated = false, SizeToContent = SizeToContent.WidthAndHeight,
                                  Opacity = 0.85, Content = stack, IsHitTestVisible = false, Left = -10000, Top = -10000 };
            window.SourceInitialized += delegate {
                IntPtr h = new WindowInteropHelper(window).Handle;
                // Click-through and never activated: drop targets below keep receiving the drag.
                Native.setExStyle(h, Native.exStyle(h) | WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE);
            };
            window.Show();
            follow();
        }

        const int WS_EX_TRANSPARENT = 0x20;

        public void follow()
        {
            Native.GetCursorPos(out POINT p);
            var source = PresentationSource.FromVisual(window);
            double scale = source?.CompositionTarget.TransformToDevice.M11 ?? 1;
            window.Left = p.X / scale - 48;
            window.Top = p.Y / scale - 20;
        }

        public void close()
        {
            window.Close();
        }
    }

    // Lets Windows draw the drag image of files dragged from File Explorer while they are over a panel, as it does over
    // any folder window.
    public static class DropImages
    {
        [ComImport, Guid("4657278B-411B-11D2-839A-00C04FD918D0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDropTargetHelper
        {
            void DragEnter(IntPtr target, ComDataObject data, ref POINT point, int effect);
            void DragLeave();
            void DragOver(ref POINT point, int effect);
            void Drop(ComDataObject data, ref POINT point, int effect);
            void Show(bool show);
        }

        static IDropTargetHelper helper;

        static IDropTargetHelper Helper
        {
            get
            {
                if (helper == null)
                    try
                    {
                        helper = (IDropTargetHelper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("4657278A-411B-11D2-839A-00C04FD918D0")));
                    }
                    catch (Exception)
                    {
                    }
                return helper;
            }
        }

        public static void enter(Visual target, IDataObject data, DragDropEffects effect)
        {
            if (!(data is ComDataObject com) || !(PresentationSource.FromVisual(target) is HwndSource source))
                return;
            try
            {
                Native.GetCursorPos(out POINT p);
                Helper?.DragEnter(source.Handle, com, ref p, (int)effect);
            }
            catch (Exception)
            {
            }
        }

        public static void over(DragDropEffects effect)
        {
            try
            {
                Native.GetCursorPos(out POINT p);
                Helper?.DragOver(ref p, (int)effect);
            }
            catch (Exception)
            {
            }
        }

        public static void leave()
        {
            try
            {
                Helper?.DragLeave();
            }
            catch (Exception)
            {
            }
        }

        public static void drop(IDataObject data, DragDropEffects effect)
        {
            if (!(data is ComDataObject com))
                return;
            try
            {
                Native.GetCursorPos(out POINT p);
                Helper?.Drop(com, ref p, (int)effect);
            }
            catch (Exception)
            {
            }
        }
    }
}
