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
    }

    // One panel on the desktop. X and Y are physical screen pixels; Width and Height are device-independent
    // units, scaled by the DPI of the monitor the panel sits on.
    public class Group
    {
        public const double MinWidth = 240, MaxWidth = 720, MinHeight = 120, MaxHeight = 900;
        public const double DefaultWidth = 348, DefaultHeight = 292;

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
        public double Width { get; set; }
        public double Height { get; set; }
        public List<Entry> Items { get; set; }

        public Group()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            Kind = PanelKind.Collection;
            Tint = Tints.SeaGlass;
            Visible = true;
            Width = DefaultWidth;
            Height = DefaultHeight;
            Items = new List<Entry>();
        }

        public bool IsFolder => Kind == PanelKind.Folder;
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
        public bool LockLayout { get; set; }
        public bool OverlayHotkey { get; set; }
        public bool Animations { get; set; }
        public double Opacity { get; set; }
        public string Theme { get; set; }
        public string Language { get; set; }
        public string IconSize { get; set; }

        public Layout()
        {
            Version = CurrentVersion;
            Groups = new List<Group>();
            StartupEnabled = true;
            OverlayHotkey = true;
            Animations = true;
            Opacity = DefaultOpacity;
            Theme = "system";
            Language = "system";
            IconSize = "medium";
        }
    }
}
