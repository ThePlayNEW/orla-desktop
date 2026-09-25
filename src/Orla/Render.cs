using System;
using System.Collections.Generic;
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
        static Controller controller0;

        public static async void run(Controller controller, string[] args)
        {
            string folder = args.Length > 0 ? args[0] : ".";
            Directory.CreateDirectory(folder);
            if (args.Contains("--crowded"))
            {
                // Not for the documentation: the organizer's preview with a crowded desktop on common screen sizes.
                controller.store.data = Demo.create();
                foreach (var size in new[] { new { Name = "1080p", W = 1920, H = 1040 }, new { Name = "ultrawide", W = 2560, H = 1040 },
                                             new { Name = "laptop", W = 1366, H = 728 } })
                {
                    var screen = new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = size.W, Bottom = size.H } };
                    await central(controller, "crowded", System.IO.Path.Combine(folder, "crowded-" + size.Name + ".png"), new[] { screen });
                }
                // A second monitor to the left of the main one, as on many desks.
                var dual = new[] { new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 2560, Bottom = 1040 } },
                                   new Screen { Dpi = 96, Work = new RECT { Left = -1920, Right = 0, Top = 60, Bottom = 1100 } } };
                await central(controller, "crowded", System.IO.Path.Combine(folder, "crowded-dual.png"), dual);
                // Organizing again: a tidy desktop that gained a crowd of new items.
                controller.store.data = Organizer.build(Demo.plan(), controller.Layout, true, true, dual[0]);
                await central(controller, "complete", System.IO.Path.Combine(folder, "crowded-complete.png"), dual);
                controller.quit();
                return;
            }
            controller.store.data = Demo.create();
            controller0 = controller;
            foreach (string theme in new[] { "dark", "light" })
            {
                controller.Layout.Theme = theme;
                Theme.apply(controller.Layout);
                await scene(controller, System.IO.Path.Combine(folder, "hero-" + theme + ".png"));
                await central(controller, "panels", System.IO.Path.Combine(folder, "central-" + theme + ".png"));
                await central(controller, "appearance", System.IO.Path.Combine(folder, "appearance-" + theme + ".png"));
                await central(controller, "general", System.IO.Path.Combine(folder, "general-" + theme + ".png"));
                await central(controller, "about", System.IO.Path.Combine(folder, "about-" + theme + ".png"));
                await central(controller, "welcome", System.IO.Path.Combine(folder, "welcome-" + theme + ".png"));
                await central(controller, "presets", System.IO.Path.Combine(folder, "presets-" + theme + ".png"));
                await central(controller, "organize", System.IO.Path.Combine(folder, "organize-" + theme + ".png"), new[] { Screens.primary() });
                await search(controller, System.IO.Path.Combine(folder, "search-" + theme + ".png"));
                await dialog(System.IO.Path.Combine(folder, "dialog-" + theme + ".png"));
                await tour(System.IO.Path.Combine(folder, "tour-" + theme + ".png"));
            }
            controller.Layout.Theme = "dark";
            Theme.apply(controller.Layout);
            await social(controller, System.IO.Path.Combine(folder, "social.png"));
            controller.quit();
        }

        // GitHub's link preview: the mark, the name and one real panel over the night shoreline.
        static async Task social(Controller controller, string path)
        {
            const double width = 1280, height = 640;
            var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true };
            canvas.Children.Add(wallpaper(true, width, height, false));
            var mark = new Image { Source = (ImageSource)Application.Current.FindResource("Brand.Mark"), Width = 88, Height = 88 };
            Canvas.SetLeft(mark, 96);
            Canvas.SetTop(mark, 196);
            canvas.Children.Add(mark);
            var word = new Path { Data = (Geometry)Application.Current.FindResource("Brand.Word"), Fill = Brushes.White,
                                  Stretch = Stretch.Uniform, Height = 64 };
            Canvas.SetLeft(word, 96);
            Canvas.SetTop(word, 312);
            canvas.Children.Add(word);
            var line = new TextBlock { Text = Text.get("about.lead"), Width = 500, TextWrapping = TextWrapping.Wrap, FontSize = 22,
                                       Foreground = new SolidColorBrush(Color.FromRgb(0xC3, 0xD8, 0xD5)),
                                       FontFamily = (FontFamily)Application.Current.FindResource("Font.Display") };
            Canvas.SetLeft(line, 96);
            Canvas.SetTop(line, 404);
            canvas.Children.Add(line);
            var view = new PanelView(controller, controller.Layout.Groups[1]) { Scale = 2 };
            view.refresh();
            Canvas.SetLeft(view, 700);
            Canvas.SetTop(view, 150);
            canvas.Children.Add(view);
            await save(canvas, width, height, path);
        }

        static async Task scene(Controller controller, string path)
        {
            const double width = 1440, height = 820;
            var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true };
            canvas.Children.Add(wallpaper(Theme.IsDark, width, height));
            var groups = controller.Layout.Groups;
            var views = new List<PanelView>();
            foreach (Group g in groups.Take(4))
            {
                var view = new PanelView(controller, g) { Scale = 2 };
                view.refresh();
                canvas.Children.Add(view);
                views.Add(view);
            }
            // Panels size themselves once their content is read; stack them in two columns like on a desktop.
            await Task.Delay(800);
            double[] columns = { width - 28, width - 28 };
            double[] tops = { 28, 28 };
            for (int i = 0; i < views.Count; i++)
            {
                int column = i % 2;
                PanelView view = views[i];
                columns[column] = width - 28 - column * (view.Width + 24);
                Canvas.SetLeft(view, columns[column] - view.Width);
                Canvas.SetTop(view, tops[column]);
                tops[column] += view.Height + 24;
            }
            await save(canvas, width, height, path);
        }

        // Quick search over the sample panels, with a few letters typed.
        static async Task search(Controller controller, string path)
        {
            var hits = new List<SearchWindow.Hit>();
            foreach (Group g in controller.Layout.Groups)
            {
                var view = new PanelView(controller, g);
                view.refresh();
                await Task.Delay(600);
                hits.AddRange(view.Tiles.Select(t => new SearchWindow.Hit { Tile = t, Group = g, Plain = Organizer.plain(t.Label ?? "") }));
            }
            var window = new SearchWindow(controller, hits, "re") { ShowActivated = false };
            window.Show();
            window.Left = -20000;
            await Task.Delay(300);
            // The card keeps room around it for its shadow.
            var content = (FrameworkElement)window.Content;
            await save(content, content.ActualWidth + content.Margin.Left + content.Margin.Right,
                       content.ActualHeight + content.Margin.Top + content.Margin.Bottom, path);
            window.Close();
        }

        // One tour step over a sample panel on the shoreline wallpaper.
        static async Task tour(string path)
        {
            const double width = 1000, height = 560;
            var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true };
            canvas.Children.Add(wallpaper(Theme.IsDark, width, height));
            var view = new PanelView(controller0, controller0.Layout.Groups[1]) { Scale = 1 };
            view.refresh();
            Canvas.SetLeft(view, 560);
            Canvas.SetTop(view, 40);
            canvas.Children.Add(view);
            await Task.Delay(1800);
            var window = new TourWindow(new List<TourWindow.Step> {
                new TourWindow.Step { Key = "move", Target = new Rect(560, 40, view.Width, 44), Around = new Rect(560, 40, view.Width, view.Height) },
                new TourWindow.Step { Key = "resize" }, new TourWindow.Step { Key = "menu" }, new TourWindow.Step { Key = "search", Argument = "Ctrl+Alt+Espaço" },
                new TourWindow.Step { Key = "tray" } }, new Rect(0, 0, width, height)) { ShowActivated = false };
            window.Show();
            window.Left = -20000;
            await Task.Delay(300);
            var stage = (Canvas)window.Content;
            var copy = new VisualBrush(stage) { Stretch = Stretch.None, ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, width, height) };
            canvas.Children.Add(new Rectangle { Width = width, Height = height, Fill = copy });
            await save(canvas, width, height, path);
            window.Close();
        }

        static async Task dialog(string path)
        {
            Window window = Dialog.sample(Text.format("panel.removeTitle", Text.get("preset.work")), Text.get("panel.removeMessage"),
                                          Text.get("panel.removeAction"), true);
            window.ShowActivated = false;
            window.Show();
            window.Left = -20000;
            await Task.Delay(300);
            var content = (FrameworkElement)window.Content;
            await save(content, content.ActualWidth + content.Margin.Left + content.Margin.Right,
                       content.ActualHeight + content.Margin.Top + content.Margin.Bottom, path);
            window.Close();
        }

        static async Task central(Controller controller, string page, string path, IList<Screen> screens = null)
        {
            var window = new CentralWindow(controller) { PreviewScreens = screens, WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000,
                                                         Top = 0, ShowActivated = false, Width = 1040, Height = 700 };
            if (page == "welcome" || page == "presets" || page == "organize" || page == "crowded")
            {
                window.show("welcome");
                if (page == "presets")
                    window.showPresetChoices();
                if (page == "organize")
                    window.showOrganize(true, Demo.plan);
                if (page == "crowded")
                    window.showOrganize(true, Demo.crowded);
            }
            else if (page == "complete")
            {
                window.show(null);
                window.showOrganize(false, Demo.crowded);
            }
            else
            {
                window.show(null);
                if (page == "appearance")
                    window.NavAppearance.IsChecked = true;
                if (page == "general")
                    window.NavGeneral.IsChecked = true;
                if (page == "about")
                    window.NavAbout.IsChecked = true;
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
        static Canvas wallpaper(bool dark, double width, double height, bool withSun = true)
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
            if (withSun)
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
