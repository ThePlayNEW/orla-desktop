using System;
using System.IO;
using System.Linq;

namespace Orla
{
    // The two ways to begin, offered on the welcome screen.
    public static class Starter
    {
        // Sorts what is on the desktop into collection panels and hides the Windows icons. Files stay where they
        // are. A live panel of the desktop folder catches anything saved there later.
        public static Layout organized(Layout settings)
        {
            Layout layout = copySettings(settings);
            layout.CleanDesktop = true;
            var folders = new Group { Name = Text.get("starter.projects"), Tint = Tints.Coral };
            var apps = new Group { Name = Text.get("starter.apps"), Tint = Tints.Sky };
            var files = new Group { Name = Text.get("starter.files"), Tint = Tints.Sand };
            foreach (string path in Shell.desktopDirectories().SelectMany(d => Shell.list(d)))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                Group target = Directory.Exists(path) ? folders : extension == ".lnk" || extension == ".url" ? apps : files;
                if (target.Items.Count < Store.MaxItems &&
                    !target.Items.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    target.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            }
            layout.Groups.Add(new Group { Kind = PanelKind.Folder, Name = Text.get("starter.desktop"),
                                          FolderPath = Shell.DesktopFolder, OnlyUnorganized = true, Height = 200 });
            foreach (Group g in new[] { apps, folders, files })
                if (g.Items.Count > 0)
                    layout.Groups.Add(g);
            layout.Groups.Add(quickAccess());
            return layout;
        }

        // Keeps the Windows icons as they are and adds panels next to them.
        public static Layout alongside(Layout settings)
        {
            Layout layout = copySettings(settings);
            layout.CleanDesktop = false;
            layout.Groups.Add(new Group { Kind = PanelKind.Folder, Name = Text.get("starter.downloads"),
                                          FolderPath = "shell:Downloads", Tint = Tints.Sky });
            layout.Groups.Add(new Group { Name = Text.get("starter.favorites"), Tint = Tints.Sand, Height = 200 });
            layout.Groups.Add(quickAccess());
            return layout;
        }

        static Group quickAccess()
        {
            var g = new Group { Name = Text.get("starter.quickAccess"), Tint = Tints.Moss, Height = 150 };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(documents))
                g.Items.Add(new Entry { Name = Shell.displayName(documents), Path = documents });
            return g;
        }

        static Layout copySettings(Layout s)
        {
            return new Layout {
                StartupConfigured = s.StartupConfigured, StartupEnabled = s.StartupEnabled, OverlayHotkey = s.OverlayHotkey,
                Animations = s.Animations, Opacity = s.Opacity, Theme = s.Theme, Language = s.Language, IconSize = s.IconSize,
                LockLayout = s.LockLayout
            };
        }
    }
}
