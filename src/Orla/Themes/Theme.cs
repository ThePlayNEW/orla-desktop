using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Orla
{
    // Loads the design system and keeps the palette in step with Windows: light or dark apps, high contrast, and
    // the panel opacity chosen in the settings.
    public static class Theme
    {
        static ResourceDictionary palette;

        public static bool IsDark { get; private set; }
        public static ResourceDictionary Palette => palette;
        public static Brush Glass { get; private set; }

        // Panels live in their own native windows, which WPF does not notify when application resources change,
        // so they listen here and re-apply the palette themselves.
        public static event Action Changed, GlassChanged;
        public static bool IsHighContrast => SystemParameters.HighContrast;

        public static void install(Application app)
        {
            app.Resources.MergedDictionaries.Add(load("Controls.xaml"));
        }

        public static void apply(Layout layout)
        {
            var resources = Application.Current.Resources;
            IsDark = layout.Theme == "dark" || layout.Theme == "system" && !appsUseLightTheme();
            ResourceDictionary next = IsHighContrast ? highContrast() : load(IsDark ? "Dark.xaml" : "Light.xaml");
            if (palette != null)
                resources.MergedDictionaries.Remove(palette);
            resources.MergedDictionaries.Add(next);
            palette = next;
            updateGlass(layout.Opacity);
            Changed?.Invoke();
        }

        public static void setOpacity(double opacity)
        {
            updateGlass(opacity);
            GlassChanged?.Invoke();
        }

        static void updateGlass(double opacity)
        {
            if (IsHighContrast)
            {
                Glass = (Brush)palette["Brush.PanelGlass"];
                return;
            }
            var color = (Color)palette["Color.PanelGlass"];
            color.A = (byte)Math.Round(255 * Math.Max(Layout.MinOpacity, Math.Min(Layout.MaxOpacity, opacity)));
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Glass = brush;
            Application.Current.Resources["Brush.PanelGlass"] = brush;
        }

        static ResourceDictionary load(string name)
        {
            return new ResourceDictionary { Source = new Uri("pack://application:,,,/Orla;component/Themes/" + name) };
        }

        // High contrast uses the system colours as they are, with no translucency.
        static ResourceDictionary highContrast()
        {
            var d = new ResourceDictionary();
            void set(string key, Brush brush) => d[key] = brush;
            d["Color.PanelGlass"] = SystemColors.WindowColor;
            set("Brush.PanelGlass", SystemColors.WindowBrush);
            set("Brush.PanelBorder", SystemColors.WindowTextBrush);
            set("Brush.Shoreline", SystemColors.HighlightBrush);
            set("Brush.Text", SystemColors.WindowTextBrush);
            set("Brush.TextSecondary", SystemColors.WindowTextBrush);
            set("Brush.TextDisabled", SystemColors.GrayTextBrush);
            set("Brush.Glyph", SystemColors.WindowTextBrush);
            set("Brush.ItemHover", SystemColors.HighlightBrush);
            set("Brush.ItemPressed", SystemColors.HighlightBrush);
            set("Brush.ItemSelected", SystemColors.HighlightBrush);
            set("Brush.Focus", SystemColors.HighlightBrush);
            set("Brush.DropTarget", SystemColors.HighlightBrush);
            set("Brush.Accent", SystemColors.HighlightBrush);
            set("Brush.OnAccent", SystemColors.HighlightTextBrush);
            set("Brush.AccentHover", SystemColors.HighlightBrush);
            set("Brush.Window", SystemColors.WindowBrush);
            set("Brush.Surface", SystemColors.WindowBrush);
            set("Brush.SurfaceRaised", SystemColors.MenuBrush);
            set("Brush.Control", SystemColors.ControlBrush);
            set("Brush.ControlHover", SystemColors.HighlightBrush);
            set("Brush.Line", SystemColors.WindowTextBrush);
            set("Brush.Warning", SystemColors.WindowTextBrush);
            set("Brush.Critical", SystemColors.WindowTextBrush);
            set("Brush.OnCritical", SystemColors.WindowBrush);
            foreach (string tint in Tints.All)
                set("Brush.Tint." + tint, SystemColors.HighlightBrush);
            return d;
        }

        static bool readPersonalize(string name, bool fallback)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key?.GetValue(name) is int value ? value != 0 : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        public static bool appsUseLightTheme() => readPersonalize("AppsUseLightTheme", true);

        public static bool taskbarUsesLightTheme() => readPersonalize("SystemUsesLightTheme", false);
    }
}
