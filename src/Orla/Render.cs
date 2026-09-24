using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace Orla
{
    // `Orla.exe --render <folder>`: draws the documentation images with the real controls and sample content,
    // in both themes, so screenshots never contain anyone's personal files.
    public static class Render
    {
        public static async void run(Controller controller, string[] args)
        {
            string folder = args.Length > 0 ? args[0] : ".";
            Directory.CreateDirectory(folder);
            controller.store.data = Demo.create();
            foreach (string theme in new[] { "dark", "light" })
            {
                controller.Layout.Theme = theme;
                Theme.apply(controller.Layout);
                await scene(controller, System.IO.Path.Combine(folder, "hero-" + theme + ".png"));
                await central(controller, "panels", System.IO.Path.Combine(folder, "central-" + theme + ".png"));
                await central(controller, "appearance", System.IO.Path.Combine(folder, "appearance-" + theme + ".png"));
                await central(controller, "welcome", System.IO.Path.Combine(folder, "welcome-" + theme + ".png"));
            }
            controller.quit();
        }

        static async Task scene(Controller controller, string path)
        {
            const double width = 1440, height = 820;
            var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true };
            canvas.Children.Add(wallpaper(Theme.IsDark, width, height));
            var groups = controller.Layout.Groups;
            double[][] places = { new[] { 1044.0, 28 }, new[] { 1044.0, 296 }, new[] { 648.0, 28 }, new[] { 648.0, 296 } };
            for (int i = 0; i < groups.Count && i < places.Length; i++)
            {
                var view = new PanelView(controller, groups[i]) { Scale = 2 };
                view.refresh();
                Canvas.SetLeft(view, places[i][0]);
                Canvas.SetTop(view, places[i][1]);
                canvas.Children.Add(view);
            }
            await save(canvas, width, height, path);
        }

        static async Task central(Controller controller, string page, string path)
        {
            var window = new CentralWindow(controller) { WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000,
                                                         Top = 0, ShowActivated = false, Width = 1040, Height = 700 };
            if (page == "welcome")
                window.show("welcome");
            else
            {
                window.show(null);
                if (page == "appearance")
                    window.NavAppearance.IsChecked = true;
            }
            var content = (FrameworkElement)window.Content;
            await save(content, content.ActualWidth, content.ActualHeight, path);
            window.Close();
        }

        static async Task save(FrameworkElement element, double width, double height, string path)
        {
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            // Icons and display names arrive from the shell thread; give them a moment.
            await Task.Delay(1800);
            element.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)(width * 2), (int)(height * 2), 192, 192, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream file = File.Create(path))
                png.Save(file);
        }

        // A calm shoreline in the brand colours: night for the dark theme, morning for the light one.
        static Canvas wallpaper(bool dark, double width, double height)
        {
            var c = new Canvas { Width = width, Height = height };
            Brush gradient(string a, string b) => new LinearGradientBrush((Color)ColorConverter.ConvertFromString(a),
                                                                            (Color)ColorConverter.ConvertFromString(b), 90);
            Path shape(string data, Brush fill, Brush stroke = null, double thickness = 0) => new Path {
                Data = Geometry.Parse(data), Fill = fill, Stroke = stroke, StrokeThickness = thickness
            };
            c.Children.Add(new Rectangle { Width = width, Height = height,
                                           Fill = dark ? gradient("#07141C", "#173146") : gradient("#BCD8E2", "#E6EDEE") });
            Color glow = dark ? Color.FromRgb(214, 228, 236) : Color.FromRgb(250, 243, 226);
            var sun = new Ellipse { Width = 150, Height = 150,
                                    Fill = new RadialGradientBrush(new GradientStopCollection {
                                        new GradientStop(glow, 0.0), new GradientStop(glow, 0.42),
                                        new GradientStop(Color.FromArgb(60, glow.R, glow.G, glow.B), 0.5),
                                        new GradientStop(Color.FromArgb(0, glow.R, glow.G, glow.B), 1.0) }) };
            Canvas.SetLeft(sun, dark ? 240 : 420);
            Canvas.SetTop(sun, dark ? 150 : 120);
            c.Children.Add(sun);
            c.Children.Add(shape("M0 500 C 240 440 420 480 660 452 S 1120 430 1440 470 L 1440 820 L 0 820 Z",
                                 dark ? gradient("#0E2433", "#07131D") : gradient("#8EC3CB", "#B7D7D8")));
            c.Children.Add(shape("M0 620 C 280 570 500 660 800 610 S 1240 570 1440 612 L 1440 820 L 0 820 Z",
                                 dark ? gradient("#1A2E36", "#0F1E25") : gradient("#E9DFCB", "#DCCFB4")));
            c.Children.Add(shape("M0 620 C 280 570 500 660 800 610 S 1240 570 1440 612", null,
                                 new SolidColorBrush(dark ? Color.FromArgb(120, 94, 200, 192) : Color.FromArgb(230, 244, 248, 247)), 4));
            return c;
        }
    }
}
