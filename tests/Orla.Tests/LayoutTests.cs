using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Orla.Tests
{
    // The organizer's layout on common screens, from a tidy desktop to a crowded one.
    public class LayoutTests
    {
        static List<Group> panels(params int[] counts) =>
            counts.Select(n => {
                var g = new Group { AutoCategory = Organizer.Apps };
                for (int i = 0; i < n; i++)
                    g.Items.Add(new Entry { Name = "Item " + i, Path = @"C:\x\" + i });
                return g;
            }).ToList();

        static RECT rect(Group g, double scale)
        {
            int rows = Math.Min(g.Rows, (g.Items.Count + g.Columns - 1) / g.Columns);
            double h = g.Collapsed ? PanelMetrics.CollapsedHeight : PanelMetrics.height(Math.Max(1, rows), "medium");
            int x = (int)g.X, y = (int)g.Y;
            return new RECT { Left = x, Top = y, Right = x + (int)Math.Round(PanelMetrics.width(g.Columns, "medium") * scale),
                              Bottom = y + (int)Math.Round(h * scale) };
        }

        public static IEnumerable<object[]> cases()
        {
            foreach (var screen in new[] { new[] { 1366, 728, 96 }, new[] { 1920, 1040, 96 }, new[] { 2560, 1040, 96 }, new[] { 2880, 1760, 192 } })
                foreach (var desk in new[] { new[] { 6, 5, 3, 7 }, new[] { 28, 14, 9, 17, 46, 5 }, new[] { 120, 80, 60, 40, 30, 20, 10, 5 } })
                    yield return new object[] { screen[0], screen[1], screen[2], desk };
        }

        [Theory]
        [MemberData(nameof(cases))]
        public void panelsStayOnScreenAlignedAndApart(int width, int height, int dpi, int[] desk)
        {
            var screen = new Screen { Dpi = (uint)dpi, Primary = true, Work = new RECT { Right = width, Bottom = height } };
            List<Group> left = panels(desk), right = panels(desk.Reverse().ToArray());
            Screens.arrange(left, right, "medium", screen);
            var all = left.Concat(right).ToList();
            var rects = all.Select(g => rect(g, screen.Scale)).ToList();
            foreach (RECT r in rects)
            {
                Assert.True(r.Left >= 0 && r.Top >= 0 && r.Right <= width && r.Bottom <= height, "off screen");
            }
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.False(Screens.overlaps(rects[i], rects[j]), "overlap");
            // Panels on one side share a width, so the columns line up.
            Assert.Single(left.Select(g => g.Columns).Distinct());
            Assert.Single(right.Select(g => g.Columns).Distinct());
        }

        static void noOverlap(List<Group> all, double scale)
        {
            var rects = all.Select(g => rect(g, scale)).ToList();
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.False(Screens.overlaps(rects[i], rects[j]), "overlap");
        }

        [Fact]
        public void layingOutAgainStartsFresh()
        {
            var small = new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 1366, Bottom = 728 } };
            List<Group> left = panels(120, 80, 60, 40, 30, 20), right = panels(10, 10, 10, 10, 10);
            Screens.arrange(new Group[0], left.Concat(right).ToList(), "medium", small);
            Screens.arrange(left, right, "medium", small);
            var fresh = panels(120, 80, 60, 40, 30, 20);
            var freshRight = panels(10, 10, 10, 10, 10);
            Screens.arrange(fresh, freshRight, "medium", small);
            Assert.Equal(fresh.Select(g => g.Collapsed + ":" + g.Rows + ":" + g.X + ":" + g.Y),
                         left.Select(g => g.Collapsed + ":" + g.Rows + ":" + g.X + ":" + g.Y));
        }

        [Fact]
        public void aShortScreenUsesMoreColumnsInsteadOfRunningOffTheBottom()
        {
            var screen = new Screen { Dpi = 144, Primary = true, Work = new RECT { Right = 1366, Bottom = 400 } };
            List<Group> right = panels(8, 8, 8, 8, 8, 8);
            Screens.arrange(new Group[0], right, "medium", screen);
            Assert.All(right, g => Assert.True(rect(g, screen.Scale).Bottom <= 400));
            noOverlap(right, screen.Scale);
        }

        [Fact]
        public void whenTheSidesCannotShareTheWidthTheyNeverOverlap()
        {
            var screen = new Screen { Dpi = 192, Primary = true, Work = new RECT { Right = 1024, Bottom = 728 } };
            List<Group> left = panels(20, 20, 20, 20, 20, 20), right = panels(20, 20, 20, 20, 20);
            Screens.arrange(left, right, "medium", screen);
            noOverlap(left.Concat(right).ToList(), screen.Scale);
        }

        [Fact]
        public void aTidyDesktopLeavesTheMiddleFreeAndHidesNothing()
        {
            var screen = new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 1920, Bottom = 1040 } };
            List<Group> left = panels(6, 5, 3, 7), right = panels(4, 4, 3);
            Screens.arrange(left, right, "medium", screen);
            Assert.All(left.Concat(right), g => Assert.True(g.Rows * g.Columns >= g.Items.Count && !g.Collapsed));
            double leftEdge = left.Max(g => rect(g, 1).Right), rightEdge = right.Min(g => rect(g, 1).Left);
            Assert.True(rightEdge - leftEdge >= 1920 / 3.0);
        }
    }
}
