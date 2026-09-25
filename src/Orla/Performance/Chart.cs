using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Orla
{
    // The last minute of a metric: the newest reading at the right edge, filled below like water under its line.
    // With a grid, pointing at it reads out the value at that moment.
    public class Chart : FrameworkElement
    {
        public static readonly DependencyProperty ValuesProperty = register(nameof(Values), typeof(IList<double>), null);
        public static readonly DependencyProperty SecondProperty = register(nameof(Second), typeof(IList<double>), null);
        public static readonly DependencyProperty MaxProperty = register(nameof(Max), typeof(double), 100.0);
        public static readonly DependencyProperty TintProperty = register(nameof(Tint), typeof(string), Tints.Sky);
        public static readonly DependencyProperty GridProperty = register(nameof(Grid), typeof(bool), false);

        static DependencyProperty register(string name, Type type, object value) =>
            DependencyProperty.Register(name, type, typeof(Chart), new FrameworkPropertyMetadata(value, FrameworkPropertyMetadataOptions.AffectsRender));

        public IList<double> Values { get => (IList<double>)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
        public IList<double> Second { get => (IList<double>)GetValue(SecondProperty); set => SetValue(SecondProperty, value); }
        // 100 for percentages; 0 scales to the largest value in view.
        public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
        public string Tint { get => (string)GetValue(TintProperty); set => SetValue(TintProperty, value); }
        public bool Grid { get => (bool)GetValue(GridProperty); set => SetValue(GridProperty, value); }
        // How the readout writes a value.
        public Func<double, string> Format { get; set; } = Units.percent;

        // The reading under the pointer, counted back from the newest; -1 when the pointer is elsewhere.
        int hover = -1;

        // The top of the scale in use: 100 for percentages, a round number above the peak for rates.
        public double Top => Max > 0 ? Max : Units.ceiling(Math.Max(peak(Values), peak(Second)), 1000);

        static double peak(IList<double> values) => values == null || values.Count == 0 ? 0 : values.Max();

        double Step => ActualWidth / (Sensors.HistoryLength - 1);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!Grid)
                return;
            int back = (int)Math.Round((ActualWidth - e.GetPosition(this).X) / Step);
            if (back != hover)
            {
                hover = back;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            hover = -1;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0)
                return;
            // Hit testing over the whole area, so pointing anywhere on the chart works.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));
            if (Grid && TryFindResource("Brush.Line") is Brush line)
            {
                var pen = new Pen(line, 1);
                for (int i = 1; i < 4; i++)
                {
                    double y = Math.Round(h * i / 4) + 0.5;
                    dc.DrawLine(pen, new Point(0, y), new Point(w, y));
                }
            }
            var tint = TryFindResource("Brush.Tint." + Tint) as SolidColorBrush ?? Brushes.Gray;
            double top = Top;
            draw(dc, Second, tint, top, false);
            draw(dc, Values, tint, top, true);
            readout(dc, tint, top);
        }

        Point at(IList<double> values, int i, double top)
        {
            double h = ActualHeight;
            // Kept inside by the line's width, so a flat zero or a full 100 still shows.
            return new Point(ActualWidth - (values.Count - 1 - i) * Step, Math.Max(1, Math.Min(h - 1, h - values[i] / top * h)));
        }

        void draw(DrawingContext dc, IList<double> values, SolidColorBrush tint, double top, bool main)
        {
            if (values == null || values.Count == 0)
                return;
            double h = ActualHeight;
            var points = Enumerable.Range(0, values.Count).Select(i => at(values, i, top)).ToList();
            Color c = tint.Color;
            if (main)
            {
                var area = new StreamGeometry();
                using (StreamGeometryContext g = area.Open())
                {
                    g.BeginFigure(new Point(points[0].X, h), true, true);
                    g.PolyLineTo(points, false, false);
                    g.LineTo(new Point(points[points.Count - 1].X, h), false, false);
                }
                area.Freeze();
                // Deepest just under the line and clear at the bottom, like water seen from the side.
                var fill = new LinearGradientBrush(Color.FromArgb(0x40, c.R, c.G, c.B), Color.FromArgb(0x06, c.R, c.G, c.B), 90);
                fill.Freeze();
                dc.DrawGeometry(fill, null, area);
            }
            var stroke = new StreamGeometry();
            using (StreamGeometryContext g = stroke.Open())
            {
                g.BeginFigure(points[0], false, false);
                g.PolyLineTo(points, true, true);
            }
            stroke.Freeze();
            // The second line, such as sending, is the same colour at half strength.
            var pen = new Pen(main ? (Brush)tint : new SolidColorBrush(Color.FromArgb(0x80, c.R, c.G, c.B)), Grid ? 2 : 1.5) { LineJoin = PenLineJoin.Round };
            pen.Freeze();
            dc.DrawGeometry(null, pen, stroke);
        }

        // A marker on the reading under the pointer, with its value and how long ago it was.
        void readout(DrawingContext dc, SolidColorBrush tint, double top)
        {
            IList<double> values = Values;
            if (hover < 0 || values == null || hover >= values.Count)
                return;
            int i = values.Count - 1 - hover;
            Point p = at(values, i, top);
            Brush surface = TryFindResource("Brush.Surface") as Brush ?? Brushes.White;
            if (TryFindResource("Brush.TextSecondary") is Brush guide)
                dc.DrawLine(new Pen(guide, 1) { DashStyle = DashStyles.Dot }, new Point(Math.Round(p.X) + 0.5, 0), new Point(Math.Round(p.X) + 0.5, ActualHeight));
            dc.DrawEllipse(tint, new Pen(surface, 2), p, 4.5, 4.5);
            string when = hover == 0 ? Text.get("perf.now") : Text.format("perf.secondsAgo", (hover * Sensors.Interval / 1000.0).ToString("0.#"));
            var text = new FormattedText(Format(values[i]) + "   " + when, CultureInfo.CurrentCulture, FlowDirection,
                                         new Typeface((FontFamily)FindResource("Font.Text"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                                         12, TryFindResource("Brush.Text") as Brush ?? Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            // Beside the marker, flipping to its left near the right edge.
            double x = p.X + 12 + text.Width + 16 > ActualWidth ? p.X - 12 - text.Width - 16 : p.X + 12;
            var box = new Rect(Math.Max(4, x), 6, text.Width + 16, text.Height + 8);
            dc.DrawRoundedRectangle(surface, new Pen(TryFindResource("Brush.Line") as Brush ?? Brushes.Gray, 1), box, 6, 6);
            dc.DrawText(text, new Point(box.X + 8, box.Y + 4));
        }
    }
}
