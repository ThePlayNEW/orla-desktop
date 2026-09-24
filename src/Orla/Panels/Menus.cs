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

        public static FrameworkElement swatch(string brushKey)
        {
            var dot = new Ellipse { Width = 12, Height = 12, Margin = new Thickness(4, 0, 0, 0) };
            dot.SetResourceReference(Shape.FillProperty, brushKey);
            return dot;
        }
    }
}
