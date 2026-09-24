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
            new Preset { Key = "desktop", Glyph = "Glyph.Desktop", Tint = Tints.Sand,
                         Create = () => {
                             Group g = folder("desktop", Shell.DesktopFolder, Tints.Sand);
                             g.OnlyUnorganized = true;
                             return g;
                         } },
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

        static Group apps()
        {
            var g = new Group { Name = Text.get("preset.apps"), Tint = Tints.SeaGlass, Rows = 2 };
            foreach (string path in desktopShortcuts().Where(p => !Games.isGame(p)).Take(Store.MaxItems))
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            return g;
        }

        static Group games()
        {
            var g = new Group { Name = Text.get("preset.games"), Tint = Tints.Coral, Rows = 2 };
            foreach (string path in Games.find().Take(Store.MaxItems))
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            return g;
        }
    }

    // Recognizes game and game-launcher shortcuts by where they point: store links such as steam:// and the
    // install folders the major stores use.
    public static class Games
    {
        static readonly string[] schemes = { "steam://rungameid", "steam://run", "com.epicgames.launcher://apps", "uplay://launch",
                                             "origin2://", "origin://launchgame", "eadm://", "battlenet://", "riotclient://",
                                             "goggalaxy://", "heroic://", "rockstar://" };
        static readonly string[] folders = { @"\steamapps\common\", @"\epic games\", @"\riot games\", @"\ubisoft game launcher\games\",
                                             @"\ea games\", @"\origin games\", @"\gog galaxy\games\", @"\gog games\", @"\battle.net\",
                                             @"\rockstar games\", @"\xboxgames\", @"\minecraft launcher\" };
        static readonly string[] launchers = { "steam.exe", "epicgameslauncher.exe", "battle.net launcher.exe", "battle.net.exe",
                                               "riotclientservices.exe", "upc.exe", "ubisoftconnect.exe", "eadesktop.exe", "origin.exe",
                                               "galaxyclient.exe", "launcher.exe", "minecraftlauncher.exe", "playnite.desktopapp.exe" };

        public static bool isShortcut(string path)
        {
            string e = Path.GetExtension(path).ToLowerInvariant();
            return e == ".lnk" || e == ".url";
        }

        public static bool isGame(string path)
        {
            string target = (targetOf(path) ?? "").ToLowerInvariant();
            if (target.Length == 0)
                return false;
            if (schemes.Any(s => target.StartsWith(s)) || folders.Any(f => target.Contains(f)))
                return true;
            string exe = Path.GetFileName(target);
            return launchers.Contains(exe) && (exe != "launcher.exe" || target.Contains("rockstar"));
        }

        // Game shortcuts on the desktop and in the Start menu, one per name.
        public static IEnumerable<string> find()
        {
            var roots = Shell.desktopDirectories().Select(d => new { Path = d, Deep = false }).ToList();
            foreach (var menu in new[] { Environment.SpecialFolder.Programs, Environment.SpecialFolder.CommonPrograms })
                roots.Add(new { Path = Environment.GetFolderPath(menu), Deep = true });
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots.Where(r => Directory.Exists(r.Path)))
            {
                List<string> files;
                try
                {
                    files = Directory.EnumerateFiles(root.Path, "*.*", root.Deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                .Where(isShortcut)
                                .ToList();
                }
                catch (Exception)
                {
                    continue;
                }
                foreach (string file in files)
                    if (isGame(file) && seen.Add(Path.GetFileNameWithoutExtension(file)))
                        yield return file;
            }
        }

        // Where a shortcut points: the URL of a .url file, or the target path of a .lnk file.
        public static string targetOf(string path)
        {
            try
            {
                if (Path.GetExtension(path).Equals(".url", StringComparison.OrdinalIgnoreCase))
                    return File.ReadLines(path).FirstOrDefault(l => l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))?.Substring(4);
                Type type = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(type);
                try
                {
                    dynamic link = shell.CreateShortcut(path);
                    try
                    {
                        return (string)link.TargetPath;
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
