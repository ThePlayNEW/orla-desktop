using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Orla
{
    // A short guided tour on the real desktop: each step rings the part of a panel it talks about and explains it in a
    // card beside it. The window covers the main screen but is transparent, so clicks outside the card reach the desktop.
    public class TourWindow : Window
    {
        public class Step
        {
            public string Key;
            public Rect? Target;
            // What the card must not cover, such as the whole panel when the ring is on its title; the target by default.
            public Rect? Around;
            public string Argument;
        }

        readonly List<Step> steps;
        readonly Canvas stage = new Canvas();
        int index;

        // bounds: the main screen's work area in device-independent units, from the same scale as the step targets.
        public TourWindow(List<Step> steps, Rect bounds)
        {
            this.steps = steps;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
            SetResourceReference(FontFamilyProperty, "Font.Text");
            Content = stage;
            new WindowInteropHelper(this).Owner = Dialog.OwnerHandle;
            PreviewKeyDown += (s, e) => {
                if (e.Key == Key.Escape)
                    Close();
                else if (e.Key == Key.Enter || e.Key == Key.Right)
                    next();
                else if (e.Key == Key.Left && index > 0)
                {
                    index--;
                    draw();
                }
                else
                    return;
                // Handled here, so the default button does not take the same Enter again.
                e.Handled = true;
            };
            Loaded += delegate { draw(); };
        }

        void next()
        {
            if (++index >= steps.Count)
                Close();
            else
                draw();
        }

        void draw()
        {
            stage.Children.Clear();
            Step step = steps[index];
            // The ring: an accent outline around the part being explained, open in the middle so it can be used.
            if (step.Target is Rect t)
            {
                var ring = new Rectangle { Width = t.Width + 12, Height = t.Height + 12, RadiusX = 12, RadiusY = 12, StrokeThickness = 2,
                                           IsHitTestVisible = false };
                ring.SetResourceReference(Shape.StrokeProperty, "Brush.Accent");
                Canvas.SetLeft(ring, t.Left - Left - 6);
                Canvas.SetTop(ring, t.Top - Top - 6);
                stage.Children.Add(ring);
            }
            FrameworkElement card = cardFor(step);
            card.Measure(new Size(360, Double.PositiveInfinity));
            Size size = card.DesiredSize;
            Point at = place(step.Around ?? step.Target, size);
            Canvas.SetLeft(card, at.X);
            Canvas.SetTop(card, at.Y);
            stage.Children.Add(card);
            Activate();
        }

        // Beside the target where there is room: below, above, left or right; the middle of the screen without one.
        Point place(Rect? target, Size size)
        {
            const double gap = 18;
            if (!(target is Rect t))
                return new Point((Width - size.Width) / 2, (Height - size.Height) / 2);
            double x = Math.Max(12, Math.Min(t.Left - Left, Width - size.Width - 12));
            if (t.Bottom - Top + gap + size.Height < Height)
                return new Point(x, t.Bottom - Top + gap);
            if (t.Top - Top - gap - size.Height > 0)
                return new Point(x, t.Top - Top - gap - size.Height);
            double y = Math.Max(12, Math.Min(t.Top - Top, Height - size.Height - 12));
            return t.Left - Left - gap - size.Width > 0 ? new Point(t.Left - Left - gap - size.Width, y) : new Point(t.Right - Left + gap, y);
        }

        FrameworkElement cardFor(Step step)
        {
            var card = new Border { Width = 360, Padding = new Thickness(20, 16, 20, 16), BorderThickness = new Thickness(1),
                                    Effect = new DropShadowEffect { BlurRadius = 32, ShadowDepth = 8, Opacity = 0.3, Direction = 270 } };
            card.SetResourceReference(Border.BackgroundProperty, "Brush.Surface");
            card.SetResourceReference(Border.BorderBrushProperty, "Brush.Line");
            card.SetResourceReference(Border.CornerRadiusProperty, "Radius.Panel");
            var body = new StackPanel();
            var title = new TextBlock { Text = Text.format("tour." + step.Key + "Title", step.Argument), FontSize = 16, FontWeight = FontWeights.SemiBold,
                                        TextWrapping = TextWrapping.Wrap };
            title.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text");
            title.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Display");
            var text = new TextBlock { Text = Text.get("tour." + step.Key + "Text"), FontSize = 13, TextWrapping = TextWrapping.Wrap,
                                       Margin = new Thickness(0, 6, 0, 0) };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
            body.Children.Add(title);
            body.Children.Add(text);
            var footer = new DockPanel { Margin = new Thickness(0, 16, 0, 0) };
            bool last = index == steps.Count - 1;
            var go = new Button { Content = Text.get(last ? "tour.done" : "tour.next"), IsDefault = true };
            go.SetResourceReference(StyleProperty, "Orla.Button.Accent");
            go.Click += delegate { next(); };
            DockPanel.SetDock(go, Dock.Right);
            footer.Children.Add(go);
            if (!last)
            {
                var skip = new Button { Content = Text.get("tour.skip"), Margin = new Thickness(0, 0, 8, 0) };
                skip.SetResourceReference(StyleProperty, "Orla.Button.Quiet");
                skip.Click += delegate { Close(); };
                DockPanel.SetDock(skip, Dock.Right);
                footer.Children.Add(skip);
            }
            // Where the tour is: a dot per step, the current one in the accent colour.
            var dots = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < steps.Count; i++)
            {
                var dot = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 6, 0), Opacity = i == index ? 1 : 0.45 };
                dot.SetResourceReference(Shape.FillProperty, i == index ? "Brush.Accent" : "Brush.TextSecondary");
                dots.Children.Add(dot);
            }
            var where = new TextBlock { Text = Text.format("tour.step", index + 1, steps.Count), FontSize = 12, Margin = new Thickness(4, 0, 0, 0),
                                        VerticalAlignment = VerticalAlignment.Center };
            where.SetResourceReference(TextBlock.ForegroundProperty, "Brush.TextSecondary");
            dots.Children.Add(where);
            footer.Children.Add(dots);
            body.Children.Add(footer);
            card.Child = body;
            return card;
        }
    }
}
