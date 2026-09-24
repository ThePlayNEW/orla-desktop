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
                                          FolderPath = Shell.DesktopFolder, OnlyUnorganized = true, Rows = 2 });
            foreach (Group g in new[] { apps, folders, files })
                if (g.Items.Count > 0)
                    layout.Groups.Add(g);
            layout.Groups.Add(Presets.All[0].Create());
            return layout;
        }

        // Keeps the Windows icons as they are; the panels come from the presets people pick.
        public static Layout alongside(Layout settings)
        {
            Layout layout = copySettings(settings);
            layout.CleanDesktop = false;
            return layout;
        }

        static Layout copySettings(Layout s)
        {
            return new Layout {
                StartupConfigured = s.StartupConfigured, StartupEnabled = s.StartupEnabled, OverlayHotkey = s.OverlayHotkey,
                Animations = s.Animations, Opacity = s.Opacity, Theme = s.Theme, Language = s.Language, IconSize = s.IconSize,
                LockLayout = s.LockLayout, AutoUpdate = s.AutoUpdate, LastUpdateCheck = s.LastUpdateCheck
            };
        }
    }
}
