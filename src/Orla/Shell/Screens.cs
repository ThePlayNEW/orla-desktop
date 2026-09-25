using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Orla
{
    public class Screen
    {
        public RECT Bounds, Work;
        public uint Dpi;
        public bool Primary;
        public double Scale => Dpi / 96.0;
    }

    // Monitor geometry in physical pixels, and where panels are allowed to sit.
    public static class Screens
    {
        // Margin: the gap between panels. Edge: the gap between the panels and the edges of the screen, the same
        // wherever a panel lands, whether arranged, placed, dragged or kept on screen.
        public const int Margin = 12, Edge = 24, Snap = 12, Grid = 8;

        public static List<Screen> all()
        {
            var list = new List<Screen>();
            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr m, IntPtr hdc, ref RECT r, IntPtr d) {
                list.Add(describe(m));
                return true;
            }, IntPtr.Zero);
            return list;
        }

        public static Screen primary() => all().FirstOrDefault(s => s.Primary) ?? all().First();

        public static Screen at(int x, int y) => describe(Native.MonitorFromPoint(new POINT(x, y),
                                                                                    Native.MONITOR_DEFAULTTONEAREST));

        public static Screen forRect(RECT r) => describe(Native.MonitorFromRect(ref r,
                                                                                  Native.MONITOR_DEFAULTTONEAREST));

        static Screen describe(IntPtr monitor)
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf(typeof(MONITORINFOEX)) };
            Native.GetMonitorInfo(monitor, ref info);
            uint dpi = 96;
            try
            {
                if (Native.GetDpiForMonitor(monitor, 0, out uint x, out uint y) == 0)
                    dpi = x;
            }
            catch (DllNotFoundException)
            {
            }
            return new Screen { Bounds = info.rcMonitor, Work = info.rcWork, Dpi = dpi, Primary = (info.dwFlags & 1) != 0 };
        }

        // Keeps a panel fully inside the work area of the monitor it mostly covers, with a margin on every side.
        public static RECT clamp(RECT r) => clampTo(r, forRect(r).Work);

        public static RECT clampTo(RECT r, RECT work)
        {
            int w = Math.Min(r.Width, work.Width - 2 * Edge), h = Math.Min(r.Height, work.Height - 2 * Edge);
            int x = Math.Max(work.Left + Edge, Math.Min(r.Left, work.Right - Edge - w));
            int y = Math.Max(work.Top + Edge, Math.Min(r.Top, work.Bottom - Edge - h));
            return new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h };
        }

        // The monitor a panel belongs to: the one holding its top-left area, or else the closest one.
        public static Screen nearest(Group g, IList<Screen> screens)
        {
            double x = g.X + 40, y = g.Y + 20;
            double distance(Screen s) => Math.Pow(Math.Max(s.Work.Left - x, Math.Max(0, x - s.Work.Right)), 2) +
                                         Math.Pow(Math.Max(s.Work.Top - y, Math.Max(0, y - s.Work.Bottom)), 2);
            return screens.OrderBy(distance).First();
        }

        // Where a panel sits, in physical pixels, from its saved position and size.
        public static RECT rectOf(Group g, string iconSize, double scale)
        {
            int x = (int)g.X, y = (int)g.Y;
            return new RECT { Left = x, Top = y, Right = x + (int)Math.Round(PanelMetrics.width(g.Columns, iconSize) * scale),
                              Bottom = y + (int)Math.Round(PanelMetrics.height(g, PanelMetrics.plannedRows(g), iconSize) * scale) };
        }

        // A spot for a new panel next to the panels already on a screen: the first free place in the column nearest the
        // chosen edge, then the next column inward, keeping the usual gap. It never covers a panel.
        public static void place(Group g, IList<RECT> taken, Screen s, string iconSize, bool fromLeft)
        {
            const int edge = Edge;
            RECT work = s.Work;
            RECT size = rectOf(g, iconSize, s.Scale);
            int w = size.Width, h = size.Height;
            for (int i = 0; ; i += Grid * 2)
            {
                int x = fromLeft ? work.Left + edge + i : work.Right - edge - w - i;
                if (x < work.Left + edge || x + w > work.Right - edge)
                    break;
                for (int y = work.Top + edge; y + h <= work.Bottom - edge; y += Grid * 2)
                {
                    var r = new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h };
                    var gap = new RECT { Left = x - Margin, Top = y - Margin, Right = x + w + Margin, Bottom = y + h + Margin };
                    if (taken.Any(o => overlaps(gap, o)))
                        continue;
                    g.X = x;
                    g.Y = y;
                    taken.Add(r);
                    return;
                }
            }
            // No free place big enough: the corner, and the desktop settles it next to the others.
            g.X = fromLeft ? work.Left + edge : work.Right - edge - w;
            g.Y = work.Top + edge;
        }

        public static bool overlaps(RECT a, RECT b) =>
            a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;

        // The closest place for a panel that stays on its monitor and clear of every other panel. Candidates are the
        // spots right next to the panels it would cover; a coarse scan of the work area is the last resort.
        public static RECT free(RECT r, IList<RECT> others)
        {
            r = clamp(r);
            if (!others.Any(o => overlaps(r, o)))
                return r;
            RECT work = forRect(r).Work;
            int w = r.Width, h = r.Height;
            var xs = new List<int> { r.Left };
            var ys = new List<int> { r.Top };
            foreach (RECT o in others)
            {
                xs.Add(o.Right + Margin);
                xs.Add(o.Left - Margin - w);
                ys.Add(o.Bottom + Margin);
                ys.Add(o.Top - Margin - h);
            }
            RECT? best = null;
            double bestDistance = Double.MaxValue;
            void consider(int x, int y)
            {
                RECT c = clampTo(new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h }, work);
                if (others.Any(o => overlaps(c, o)))
                    return;
                double d = Math.Pow(c.Left - r.Left, 2) + Math.Pow(c.Top - r.Top, 2);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c;
                }
            }
            foreach (int x in xs)
                foreach (int y in ys)
                    consider(x, y);
            if (best == null)
                for (int y = work.Top + Edge; y + h <= work.Bottom - Edge; y += 24)
                    for (int x = work.Left + Edge; x + w <= work.Right - Edge; x += 24)
                        consider(x, y);
            return best ?? r;
        }

        // While dragging: edges within reach stick to the work area margin and to the edges of other panels, leaving
        // the same gap between panels everywhere. No grid, so the panel still follows the pointer smoothly.
        public static RECT magnet(RECT r, IEnumerable<RECT> others, RECT work)
        {
            int x = pull(r.Left, r.Width, new[] { work.Left + Edge }, new[] { work.Right - Edge });
            int y = pull(r.Top, r.Height, new[] { work.Top + Edge }, new[] { work.Bottom - Edge });
            foreach (RECT o in others)
            {
                if (r.Top < o.Bottom + Snap && r.Bottom > o.Top - Snap)
                    x = pull(x, r.Width, new[] { o.Right + Margin, o.Left }, new[] { o.Left - Margin, o.Right });
                if (r.Left < o.Right + Snap && r.Right > o.Left - Snap)
                    y = pull(y, r.Height, new[] { o.Bottom + Margin, o.Top }, new[] { o.Top - Margin, o.Bottom });
            }
            return new RECT { Left = x, Top = y, Right = x + r.Width, Bottom = y + r.Height };
        }

        // Aligns a dropped panel to an 8 px grid and pulls it to nearby work-area edges and other panels.
        public static RECT snap(RECT r, IEnumerable<RECT> others)
        {
            RECT m = magnet(r, others, forRect(r).Work);
            int x = m.Left == r.Left ? (int)Math.Round(r.Left / (double)Grid) * Grid : m.Left;
            int y = m.Top == r.Top ? (int)Math.Round(r.Top / (double)Grid) * Grid : m.Top;
            return clamp(new RECT { Left = x, Top = y, Right = x + r.Width, Bottom = y + r.Height });
        }

        static int pull(int start, int size, int[] startEdges, int[] endEdges)
        {
            foreach (int e in startEdges)
                if (Math.Abs(start - e) <= Snap)
                    return e;
            foreach (int e in endEdges)
                if (Math.Abs(start + size - e) <= Snap)
                    return e - size;
            return start;
        }

        // The organizer's layout: columns of panels from both top corners of a screen, the middle left free. Panels in a
        // column share one width, so the edges line up. Among a few ways to split each side into columns and icons per
        // row, it picks the one that hides the fewest rows behind scrolling while keeping the middle open, and never lets
        // panels overlap or leave the screen. When even one row each does not fit, the last panels start collapsed.
        // keepCollapsed: panels people collapsed stay collapsed (Rearrange); the organizer's preview starts fresh.
        public static void arrange(IList<Group> left, IList<Group> right, string iconSize, Screen s, bool keepCollapsed = false)
        {
            left = left.Where(g => g.Visible).ToList();
            right = right.Where(g => g.Visible).ToList();
            const int edge = Edge;
            RECT work = s.Work;
            int room = work.Height - 2 * edge;
            var leftOptions = Fit.options(left, iconSize, s.Scale, room, keepCollapsed);
            var rightOptions = Fit.options(right, iconSize, s.Scale, room, keepCollapsed);
            // The middle should keep a third of the screen. Below that, 60 px of middle costs about one row hidden behind
            // scrolling, and below a quarter it costs four times as much: a crowded desktop scrolls inside its panels, or
            // on a small screen starts some collapsed, before the wallpaper disappears.
            Fit bestLeft = null, bestRight = null;
            double best = Double.MaxValue;
            foreach (Fit l in leftOptions)
                foreach (Fit r in rightOptions)
                {
                    double width = l.Width + r.Width + 2 * edge + (l.Width > 0 && r.Width > 0 ? Margin : 0);
                    if (width > work.Width)
                        continue;
                    double middle = work.Width - width;
                    double cost = l.Cost + r.Cost + Math.Max(0, work.Width / 3.0 - middle) / 20 + Math.Max(0, work.Width / 4.0 - middle) / 5;
                    if (cost < best)
                    {
                        best = cost;
                        bestLeft = l;
                        bestRight = r;
                    }
                }
            if (bestLeft == null)
            {
                // The two sides cannot share the width: everything goes on the right, as narrow as it has to be.
                var one = Fit.options(left.Concat(right).ToList(), iconSize, s.Scale, room, keepCollapsed);
                bestLeft = Fit.options(new Group[0], iconSize, s.Scale, room, false)[0];
                bestRight = one.Where(o => o.Width + 2 * edge <= work.Width).OrderBy(o => o.Cost).FirstOrDefault() ??
                            one.OrderBy(o => o.Width).First();
            }
            bestLeft.place(work.Left + edge, work.Top + edge, 1);
            bestRight.place(work.Right - edge, work.Top + edge, -1);
        }

        // One way to lay out one side: its panels split into columns in order, all with the same icons per row.
        class Fit
        {
            static readonly int[][] shapes = { new[] { 1, 4 }, new[] { 1, 5 }, new[] { 1, 3 }, new[] { 2, 4 }, new[] { 2, 3 },
                                               new[] { 2, 5 }, new[] { 3, 3 }, new[] { 3, 4 }, new[] { 4, 3 } };
            const int MostRows = 6;

            List<List<Group>> stacks;
            Dictionary<Group, int> rows;
            HashSet<Group> collapsed;
            int columns, panelWidth;
            double scale;
            string iconSize;

            public double Width, Cost;

            public static List<Fit> options(IList<Group> groups, string iconSize, double scale, int room, bool keepCollapsed)
            {
                var list = new List<Fit>();
                if (groups.Count == 0)
                {
                    list.Add(new Fit { stacks = new List<List<Group>>(), rows = new Dictionary<Group, int>(), collapsed = new HashSet<Group>() });
                    return list;
                }
                foreach (int[] shape in shapes.Where(x => x[0] <= groups.Count))
                    list.Add(new Fit(groups, shape[0], shape[1], iconSize, scale, room, keepCollapsed));
                return list;
            }

            Fit()
            {
            }

            Fit(IList<Group> groups, int stackCount, int columns, string iconSize, double scale, int room, bool keepCollapsed)
            {
                this.columns = columns;
                this.scale = scale;
                this.iconSize = iconSize;
                panelWidth = (int)Math.Round(PanelMetrics.width(columns, iconSize) * scale);
                rows = groups.ToDictionary(g => g, g => wanted(g));
                // Collapsed and Rows are this layout's own output; a layout drawn again must not start from them, unless
                // the panels were collapsed by hand.
                collapsed = new HashSet<Group>(keepCollapsed ? groups.Where(g => g.Collapsed) : Enumerable.Empty<Group>());
                // Columns keep the panels' order and share the height evenly.
                double total = groups.Sum(g => height(g, rows[g]) + Margin), before = 0;
                stacks = Enumerable.Range(0, stackCount).Select(_ => new List<Group>()).ToList();
                foreach (Group g in groups)
                {
                    double h = height(g, rows[g]) + Margin;
                    int index = Math.Min(stackCount - 1, (int)((before + h / 2) / (total / stackCount)));
                    stacks[index].Add(g);
                    before += h;
                }
                stacks.RemoveAll(st => st.Count == 0);
                int hidden = 0;
                double overflow = 0;
                foreach (List<Group> st in stacks)
                {
                    // Too tall: the panel with the most rows gives one up, until the column fits.
                    while (stackHeight(st) > room)
                    {
                        Group tallest = st.Where(g => !collapsed.Contains(g) && rows[g] > 1).OrderByDescending(g => rows[g]).FirstOrDefault();
                        if (tallest != null)
                        {
                            rows[tallest]--;
                            hidden++;
                            continue;
                        }
                        Group last = st.LastOrDefault(g => !collapsed.Contains(g));
                        if (last == null)
                        {
                            overflow += stackHeight(st) - room;
                            break;
                        }
                        collapsed.Add(last);
                        hidden += 6;
                    }
                }
                Width = stacks.Count * panelWidth + (stacks.Count - 1) * Margin;
                // A hidden row costs more than a wider side; empty slots in a last row cost a little.
                double empty = groups.Where(g => g.IsCollection).Sum(g => rows[g] * columns - Math.Min(g.Items.Count, rows[g] * columns));
                // Running off the screen is the last resort, and the less of it the better.
                Cost = hidden * 3 + Width / 400.0 + empty * 0.1 + overflow * 1000;
            }

            int wanted(Group g)
            {
                if (g.IsFolder)
                    return 2;
                return Math.Max(1, Math.Min(MostRows, (PanelMetrics.count(g) + columns - 1) / columns));
            }

            double height(Group g, int r) =>
                Math.Round((collapsed.Contains(g) ? PanelMetrics.CollapsedHeight : PanelMetrics.height(r, iconSize)) * scale);

            double stackHeight(List<Group> st) => st.Sum(g => height(g, rows[g])) + (st.Count - 1) * Margin;

            // Writes the result into the panels, from an outer edge toward the middle.
            public void place(int outer, int top, int direction)
            {
                int x = direction > 0 ? outer : outer - panelWidth;
                foreach (List<Group> st in stacks)
                {
                    double y = top;
                    foreach (Group g in st)
                    {
                        g.Columns = columns;
                        g.Rows = rows[g];
                        g.AutoHeight = true;
                        g.Collapsed = collapsed.Contains(g);
                        g.X = x;
                        g.Y = y;
                        y += height(g, rows[g]) + Margin;
                    }
                    x += direction * (panelWidth + Margin);
                }
            }
        }
    }
}
