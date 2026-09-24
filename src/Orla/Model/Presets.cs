using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orla
{
    // Ready-made panels for the first run and for everyday use. Folder presets show a real folder live; collection
    // presets gather shortcuts that are already on this computer. Nothing is moved or copied to build them.
    public class Preset
    {
        public string Key { get; set; }
        public string Glyph { get; set; }
        public string Tint { get; set; }
        public bool Recommended { get; set; }
        public Func<Group> Create { get; set; }
        public Func<bool> Available { get; set; } = () => true;

        public string Name => Text.get("preset." + Key);
        public string Hint => Text.get("preset." + Key + "Hint");
    }

    public static class Presets
    {
        public static readonly Preset[] All = {
            new Preset { Key = "quickAccess", Glyph = "Glyph.Desktop", Tint = Tints.Moss, Recommended = true, Create = quickAccess },
            new Preset { Key = "downloads", Glyph = "Glyph.PanelFolder", Tint = Tints.Sky, Recommended = true,
                         Create = () => folder("downloads", "shell:Downloads", Tints.Sky) },
            new Preset { Key = "apps", Glyph = "Glyph.ItemApp", Tint = Tints.SeaGlass, Recommended = true, Create = apps,
                         Available = () => desktopShortcuts().Any(p => !Games.isGame(p)) },
            new Preset { Key = "games", Glyph = "Glyph.Grid", Tint = Tints.Coral, Create = games,
                         Available = () => Games.find().Any() },
            new Preset { Key = "documents", Glyph = "Glyph.ItemFile", Tint = Tints.Sand,
                         Create = () => folder("documents", special(Environment.SpecialFolder.MyDocuments), Tints.Sand),
                         Available = () => Directory.Exists(special(Environment.SpecialFolder.MyDocuments)) },
            new Preset { Key = "pictures", Glyph = "Glyph.ItemImage", Tint = Tints.Coral,
                         Create = () => folder("pictures", special(Environment.SpecialFolder.MyPictures), Tints.Coral),
                         Available = () => Directory.Exists(special(Environment.SpecialFolder.MyPictures)) },
            new Preset { Key = "screenshots", Glyph = "Glyph.ItemImage", Tint = Tints.Sky,
                         Create = () => folder("screenshots", screenshots(), Tints.Sky), Available = () => Directory.Exists(screenshots()) },
            new Preset { Key = "desktop", Glyph = "Glyph.Desktop", Tint = Tints.SeaGlass, Create = inbox },
            new Preset { Key = "work", Glyph = "Glyph.PanelCollection", Tint = Tints.SeaGlass,
                         Create = () => new Group { Name = Text.get("preset.work"), Tint = Tints.SeaGlass, Rows = 2 } },
            new Preset { Key = "study", Glyph = "Glyph.PanelCollection", Tint = Tints.Moss,
                         Create = () => new Group { Name = Text.get("preset.study"), Tint = Tints.Moss, Rows = 2 } },
        };

        public static IEnumerable<Preset> available() => All.Where(p => {
            try
            {
                return p.Available();
            }
            catch (Exception)
            {
                return false;
            }
        });

        // The desktop inbox: what is on the desktop and not in any other panel yet.
        public static Group inbox()
        {
            Group g = folder("desktop", Shell.DesktopFolder, Tints.SeaGlass);
            g.OnlyUnorganized = true;
            return g;
        }

        static string special(Environment.SpecialFolder f) => Environment.GetFolderPath(f);

        static string screenshots() => Path.Combine(special(Environment.SpecialFolder.MyPictures), "Screenshots");

        static Group folder(string key, string path, string tint) =>
            new Group { Kind = PanelKind.Folder, Name = Text.get("preset." + key), FolderPath = path, Tint = tint, Rows = 2 };

        static Group quickAccess()
        {
            var g = new Group { Name = Text.get("preset.quickAccess"), Tint = Tints.Moss, Columns = 5, Rows = 1 };
            var places = new List<string> { "shell:MyComputerFolder", "shell:Downloads" };
            places.AddRange(new[] { special(Environment.SpecialFolder.MyDocuments), special(Environment.SpecialFolder.MyPictures) }
                                .Where(Directory.Exists));
            places.Add("shell:RecycleBinFolder");
            foreach (string path in places)
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            return g;
        }

        static IEnumerable<string> desktopShortcuts() =>
            Shell.desktopDirectories().SelectMany(Shell.list).Where(Games.isShortcut);

        static Group apps() => collection("apps", Tints.SeaGlass, desktopShortcuts().Where(p => !Games.isGame(p)));

        static Group games() => collection("games", Tints.Coral, Games.find());

        static Group collection(string key, string tint, IEnumerable<string> paths)
        {
            var g = new Group { Name = Text.get("preset." + key), Tint = tint, Rows = 2 };
            foreach (string path in paths.Take(Store.MaxItems))
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            return g;
        }
    }
}
