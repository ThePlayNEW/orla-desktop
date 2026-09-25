using System;
using System.Collections.Generic;

namespace Orla
{
    // A reference shown in a collection panel. Removing it never touches the file at Path.
    public class Entry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }

        public Entry()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }

    public static class PanelKind
    {
        public const string Collection = "collection";
        public const string Folder = "folder";
        // Live performance: processor, graphics, memory, disk and network.
        public const string Sensors = "sensors";
    }

    // One panel on the desktop. X and Y are physical screen pixels. Its size is counted in whole icon columns and
    // rows, so a panel never shows an empty strip; Rows is the most it grows to before scrolling.
    public class Group
    {
        public const int MinColumns = 2, MaxColumns = 10, MinRows = 1, MaxRows = 10;
        public const int DefaultColumns = 4, DefaultRows = 3;

        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string FolderPath { get; set; }
        public bool OnlyUnorganized { get; set; }
        public string Tint { get; set; }
        public bool Visible { get; set; }
        public bool Collapsed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        // When on, the panel is as tall as its content, up to Rows. Resizing by hand turns it off.
        public bool AutoHeight { get; set; }
        // Set on panels made by "Let Orla organize": the kind of item that belongs here, so new ones can follow.
        public string AutoCategory { get; set; }
        public List<Entry> Items { get; set; }
        // The text key of the name Orla gave the panel, so the name follows the interface language. Renaming clears it.
        public string TitleKey { get; set; }

        public Group()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            Kind = PanelKind.Collection;
            Tint = Tints.SeaGlass;
            Visible = true;
            Columns = DefaultColumns;
            Rows = DefaultRows;
            AutoHeight = true;
            Items = new List<Entry>();
        }

        public bool IsFolder => Kind == PanelKind.Folder;
        public bool IsCollection => Kind == PanelKind.Collection;
        public bool IsSensors => Kind == PanelKind.Sensors;

        public Group titled(string key)
        {
            TitleKey = key;
            Name = Text.get(key);
            return this;
        }
    }

    // Shoreline colours a panel can use. The values live in the theme; the layout stores only the name.
    public static class Tints
    {
        public const string SeaGlass = "seaglass", Sand = "sand", Coral = "coral", Sky = "sky", Moss = "moss";
        public static readonly string[] All = { SeaGlass, Sand, Coral, Sky, Moss };
    }

    public class Layout
    {
        public const int CurrentVersion = 2;
        public const double MinOpacity = .75, MaxOpacity = .95, DefaultOpacity = .82;

        public int Version { get; set; }
        public List<Group> Groups { get; set; }
        public bool Welcomed { get; set; }
        public bool StartupConfigured { get; set; }
        public bool StartupEnabled { get; set; }
        public bool CleanDesktop { get; set; }
        public bool AutoOrganize { get; set; }
        // The copy of the layout made by the last organize, in the data folder, which "Restore previous panels" uses.
        public string UndoBackup { get; set; }
        // Folders of shortcuts the organizer read from the inside, such as Shortcuts\Games. New items there are kept
        // organized like items on the desktop itself.
        public List<string> SortedFolders { get; set; }
        // What people taught the organizer by moving items between panels: an item's identity (the program a
        // shortcut opens, or the file name) and the Id of the panel it belongs in.
        public Dictionary<string, string> Learned { get; set; }
        public bool LockLayout { get; set; }
        public bool OverlayHotkey { get; set; }
        public string OverlayShortcut { get; set; }
        public bool Animations { get; set; }
        public double Opacity { get; set; }
        public string Theme { get; set; }
        public string Language { get; set; }
        public string IconSize { get; set; }
        public bool AutoUpdate { get; set; }
        public string LastUpdateCheck { get; set; }
        // The version that last ran, so an update can say what is new once.
        public string SeenVersion { get; set; }

        public Layout()
        {
            Version = CurrentVersion;
            Groups = new List<Group>();
            SortedFolders = new List<string>();
            Learned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            StartupEnabled = true;
            OverlayHotkey = true;
            OverlayShortcut = Orla.Shortcut.Default;
            Animations = true;
            Opacity = DefaultOpacity;
            Theme = "system";
            Language = "system";
            IconSize = "medium";
            AutoUpdate = true;
        }

        // A new layout with no panels and the same settings.
        public Layout copySettings() => new Layout {
            StartupConfigured = StartupConfigured, StartupEnabled = StartupEnabled, OverlayHotkey = OverlayHotkey,
            OverlayShortcut = OverlayShortcut, Welcomed = Welcomed, Animations = Animations, Opacity = Opacity, Theme = Theme,
            Language = Language, IconSize = IconSize, LockLayout = LockLayout, AutoUpdate = AutoUpdate, LastUpdateCheck = LastUpdateCheck,
            SeenVersion = SeenVersion
        };
    }
}
