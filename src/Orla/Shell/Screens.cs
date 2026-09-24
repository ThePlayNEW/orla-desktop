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
        public const int Margin = 12, Snap = 12, Grid = 8;

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

        // Keeps a panel fully inside the work area of the monitor it mostly covers.
        public static RECT clamp(RECT r)
        {
            RECT work = forRect(r).Work;
            int w = Math.Min(r.Width, work.Width - 2 * Margin), h = Math.Min(r.Height, work.Height - 2 * Margin);
            int x = Math.Max(work.Left + Margin, Math.Min(r.Left, work.Right - Margin - w));
            int y = Math.Max(work.Top + Margin, Math.Min(r.Top, work.Bottom - Margin - h));
            return new RECT { Left = x, Top = y, Right = x + w, Bottom = y + h };
        }

        // Aligns a dropped panel to an 8 px grid and pulls it to nearby work-area edges and other panels.
        public static RECT snap(RECT r, IEnumerable<RECT> others)
        {
            RECT work = forRect(r).Work;
            int x = pull(r.Left, r.Width, new[] { work.Left + Margin }, new[] { work.Right - Margin });
            int y = pull(r.Top, r.Height, new[] { work.Top + Margin }, new[] { work.Bottom - Margin });
            foreach (RECT o in others)
            {
                bool rowsOverlap = r.Top < o.Bottom + Snap && r.Bottom > o.Top - Snap;
                bool columnsOverlap = r.Left < o.Right + Snap && r.Right > o.Left - Snap;
                if (rowsOverlap)
                    x = pull(x, r.Width, new[] { o.Right + Margin, o.Left }, new[] { o.Left - Margin, o.Right });
                if (columnsOverlap)
                    y = pull(y, r.Height, new[] { o.Bottom + Margin, o.Top }, new[] { o.Top - Margin, o.Bottom });
            }
            if (x == r.Left)
                x = (int)Math.Round(x / (double)Grid) * Grid;
            if (y == r.Top)
                y = (int)Math.Round(y / (double)Grid) * Grid;
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

        // Default arrangement: columns from the top-right corner of the primary monitor, leaving the left side,
        // where Windows places its own icons, free.
        public static void arrange(IList<Group> groups)
        {
            Screen s = primary();
            int x = s.Work.Right - Margin * 2, y = s.Work.Top + Margin * 2, columnWidth = 0;
            foreach (Group g in groups.Where(g => g.Visible))
            {
                int w = (int)Math.Round(g.Width * s.Scale), h = (int)Math.Round(g.Height * s.Scale);
                if (y + h > s.Work.Bottom - Margin && y > s.Work.Top + Margin * 2)
                {
                    x -= columnWidth + Margin;
                    y = s.Work.Top + Margin * 2;
                    columnWidth = 0;
                }
                g.X = x - w;
                g.Y = y;
                y += h + Margin;
                columnWidth = Math.Max(columnWidth, w);
            }
        }
    }
}
