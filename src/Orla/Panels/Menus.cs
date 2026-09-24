using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Orla
{
    // Small builders so every menu in Orla looks and reads the same.
    public static class Menus
    {
        public static MenuItem item(string key, string glyph, Action action, string gesture = null)
        {
            var m = new MenuItem { Header = Text.get(key), InputGestureText = gesture ?? "" };
            if (glyph != null)
                m.Icon = Menus.glyph(glyph);
            if (action != null)
                m.Click += delegate { action(); };
            return m;
        }

        public static MenuItem plain(string header, Action action)
        {
            var m = new MenuItem { Header = header };
            m.Click += delegate { action(); };
            return m;
        }

        public static MenuItem check(string key, bool isChecked, Action action)
        {
            var m = new MenuItem { Header = Text.get(key), IsChecked = isChecked };
            m.Click += delegate { action(); };
            return m;
        }

        public static Path glyph(string key)
        {
            var p = new Path { Data = (Geometry)Application.Current.FindResource(key) };
            p.SetResourceReference(FrameworkElement.StyleProperty, "Orla.Glyph");
            return p;
        }

        // The shoreline colours for a panel, each with its swatch.
        public static MenuItem tints(Group g, Controller controller)
        {
            MenuItem menu = item("panel.tint", "Glyph.PanelCollection", null);
            foreach (string t in Tints.All)
            {
                string value = t;
                var dot = new Ellipse { Width = 12, Height = 12, Margin = new Thickness(4, 0, 0, 0) };
                dot.SetResourceReference(Shape.FillProperty, "Brush.Tint." + t);
                MenuItem option = check("tint." + t, g.Tint == t, () => controller.setTint(g, value));
                option.Icon = dot;
                menu.Items.Add(option);
            }
            return menu;
        }
    }
}
