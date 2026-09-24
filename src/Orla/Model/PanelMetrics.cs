using System;

namespace Orla
{
    // Panel geometry in device-independent units. Tiles are square, so a panel is its chrome plus whole tiles.
    public static class PanelMetrics
    {
        // Border and padding on both sides plus room for the scroll bar, so tiles never reflow when it appears.
        public const double ChromeWidth = 24;
        // Border, padding, header and the shoreline.
        public const double ChromeHeight = 62;
        public const double CollapsedHeight = 58;

        public static double icon(string size) => size == "small" ? 32 : size == "large" ? 48 : 40;

        // Tile body plus its 1 px margin on each side.
        public static double tile(string size) => icon(size) + 54;

        public static double width(int columns, string size) => ChromeWidth + columns * tile(size);

        public static double height(int rows, string size) => ChromeHeight + rows * tile(size);

        public static double height(Group g, int rows, string size) => g.Collapsed ? CollapsedHeight : height(rows, size);

        // The rows a panel shows before its content is read: a collection's items up to Rows, a folder's full Rows.
        public static int plannedRows(Group g) =>
            g.IsFolder ? g.Rows : Math.Max(1, Math.Min(g.Rows, (g.Items.Count + g.Columns - 1) / g.Columns));

        public static int columnsFor(double width, string size) =>
            Math.Max(Group.MinColumns, Math.Min(Group.MaxColumns, (int)Math.Round((width - ChromeWidth) / tile(size))));

        public static int rowsFor(double height, string size) =>
            Math.Max(Group.MinRows, Math.Min(Group.MaxRows, (int)Math.Round((height - ChromeHeight) / tile(size))));
    }
}
