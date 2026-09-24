using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Orla
{
    // "Let Orla organize" reads the desktop the way a person tidying it would. Program shortcuts are sorted by what
    // they open, work folders and files by kind, and the panels are laid out with the tools on the left, the work on
    // the right and the middle of the screen free. Only references are created: nothing on disk moves.
    public static class Organizer
    {
        public const string Apps = "apps", Dev = "dev", Creative = "creative", Utilities = "utilities", Play = "games",
                            Folders = "folders", Documents = "documents", Media = "media", Files = "files";
        // Not a category of desktop items: marks the Quick access panel the organizer adds, which sits with the tools.
        public const string QuickAccess = "quickAccess";

        public class Category
        {
            public string Key, Tint, Fallback;
            public bool Tools;
        }

        // In display order. A category with a single item joins its fallback, so no panel holds one lonely icon.
        public static readonly Category[] Categories = {
            new Category { Key = Apps, Tint = Tints.Sky, Tools = true },
            new Category { Key = Dev, Tint = Tints.SeaGlass, Tools = true, Fallback = Apps },
            new Category { Key = Creative, Tint = Tints.Coral, Tools = true, Fallback = Apps },
            new Category { Key = Utilities, Tint = Tints.Moss, Tools = true, Fallback = Apps },
            new Category { Key = Play, Tint = Tints.Sand, Tools = true, Fallback = Apps },
            new Category { Key = Folders, Tint = Tints.Coral },
            new Category { Key = Documents, Tint = Tints.SeaGlass, Fallback = Files },
            new Category { Key = Media, Tint = Tints.Coral, Fallback = Files },
            new Category { Key = Files, Tint = Tints.SeaGlass },
        };

        public static Category category(string key) => Categories.FirstOrDefault(c => c.Key == key);

        public class Plan
        {
            public List<Group> Groups = new List<Group>();
            public Group QuickAccess;
            public int Count;
            public List<string> SortedFolders = new List<string>();

            public void add(Group g)
            {
                Groups.Add(g);
                Count += g.Items.Count;
            }
        }

        // Reads the desktop. Shortcuts that point at network drives can make this slow, so callers run it off the UI thread.
        public static Plan plan()
        {
            var found = new Dictionary<string, List<string>>();
            var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            var sorted = new List<string>();
            foreach (string desktop in Shell.desktopDirectories())
                foreach (string path in Shell.list(desktop))
                {
                    var inside = new List<KeyValuePair<string, string>>();
                    if (Directory.Exists(path) && launcherFolder(path, inside))
                    {
                        sorted.Add(path);
                        sorted.AddRange(inside.Select(i => Path.GetDirectoryName(i.Key)).Where(d => d != path));
                        foreach (var item in inside)
                            add(found, names, classify(item.Key, item.Value), item.Key);
                    }
                    else
                        add(found, names, classify(path), path);
                }
            foreach (Category c in Categories.Where(c => c.Fallback != null))
                if (found.TryGetValue(c.Key, out List<string> few) && few.Count < 2)
                {
                    found.Remove(c.Key);
                    if (!found.ContainsKey(c.Fallback))
                        found[c.Fallback] = new List<string>();
                    found[c.Fallback].AddRange(few);
                }
            var plan = new Plan { QuickAccess = Presets.All[0].Create() };
            plan.SortedFolders.AddRange(sorted.Distinct(StringComparer.OrdinalIgnoreCase));
            foreach (Category c in Categories.Where(c => found.ContainsKey(c.Key)))
                plan.add(panel(c.Key, found[c.Key]));
            return plan;
        }

        // A panel for one category, in name order and sized to its content.
        public static Group panel(string key, IEnumerable<string> paths)
        {
            Category c = category(key);
            var g = new Group { Name = Text.get("organize." + c.Key), Tint = c.Tint, AutoCategory = c.Key };
            foreach (string path in paths.OrderBy(Shell.displayName, StringComparer.CurrentCultureIgnoreCase).Take(Store.MaxItems))
                g.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            return g;
        }

        static void add(Dictionary<string, List<string>> found, HashSet<string> names, string key, string path)
        {
            // The public desktop often repeats a shortcut that is also on the user's own desktop. Files are never merged:
            // Report.pdf and Report.docx show the same name when extensions are hidden.
            if (Games.isShortcut(path) && !names.Add(key + "|" + Shell.displayName(path)))
                return;
            if (!found.TryGetValue(key, out List<string> list))
                found[key] = list = new List<string>();
            list.Add(path);
        }

        // Lays out a plan: tools from the left edge, work and the desktop inbox from the right edge.
        public static Layout build(Plan plan, Layout settings, bool clean, bool keep, Screen screen)
        {
            Layout layout = Starter.copySettings(settings);
            layout.Welcomed = true;
            layout.CleanDesktop = clean;
            layout.AutoOrganize = keep;
            layout.SortedFolders = plan.SortedFolders.ToList();
            var tools = plan.Groups.Where(g => category(g.AutoCategory)?.Tools == true).ToList();
            var work = plan.Groups.Where(g => !tools.Contains(g)).ToList();
            if (plan.QuickAccess != null)
            {
                plan.QuickAccess.AutoCategory = QuickAccess;
                tools.Add(plan.QuickAccess);
            }
            // Without a clean desktop the Windows icons already show what is new, so the inbox would only repeat them.
            if (clean)
                work.Add(Presets.inbox());
            layout.Groups.AddRange(tools);
            layout.Groups.AddRange(work);
            arrange(layout, screen);
            return layout;
        }

        // Tools on the left and the rest on the right; with the Windows icons showing, their column on the left stays
        // free and every panel goes on the right. Panels the organizer did not make count as work.
        public static void arrange(Layout layout, Screen screen)
        {
            var tools = layout.Groups.Where(g => category(g.AutoCategory)?.Tools == true || g.AutoCategory == QuickAccess).ToList();
            var work = layout.Groups.Where(g => !tools.Contains(g)).ToList();
            if (layout.CleanDesktop)
                Screens.arrange(tools, work, layout.IconSize, screen);
            else
                Screens.arrange(new Group[0], tools.Concat(work).ToList(), layout.IconSize, screen);
        }

        // Where something new on the desktop belongs, among the panels the organizer made. Null leaves it in the inbox.
        public static Group home(string path, IEnumerable<Group> groups)
        {
            var auto = groups.Where(g => !g.IsFolder && g.AutoCategory != null).ToList();
            string key = classify(path, hintAround(path));
            for (Category c = category(key); c != null; c = category(c.Fallback))
            {
                Group g = auto.FirstOrDefault(x => x.AutoCategory == c.Key);
                if (g != null)
                    return g;
            }
            return null;
        }

        static string hintAround(string path)
        {
            var roots = Shell.desktopDirectories().Select(d => d.TrimEnd('\\'));
            string parent = Path.GetDirectoryName(path);
            for (int i = 0; i < 2 && parent != null && !roots.Contains(parent.TrimEnd('\\'), StringComparer.OrdinalIgnoreCase); i++)
            {
                string hint = hintFor(parent);
                if (hint != null)
                    return hint;
                parent = Path.GetDirectoryName(parent);
            }
            return null;
        }

        // ---- telling things apart ----

        static readonly string[] launchables = { ".lnk", ".url", ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".ahk", ".reg" };
        static readonly string[] scripts = { ".bat", ".cmd", ".ps1", ".vbs", ".ahk", ".reg" };
        static readonly string[] documents = { ".pdf", ".doc", ".docx", ".odt", ".rtf", ".txt", ".md", ".xls", ".xlsx", ".ods", ".csv",
                                               ".ppt", ".pptx", ".odp", ".epub", ".pages", ".numbers", ".key" };
        static readonly string[] media = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".heic", ".svg", ".psd",
                                           ".ai", ".fig", ".xd", ".mp4", ".mov", ".mkv", ".avi", ".webm", ".mp3", ".wav", ".flac",
                                           ".m4a", ".ogg" };

        static readonly string[] devExe = {
            "code.exe", "code - insiders.exe", "cursor.exe", "windsurf.exe", "zed.exe", "devenv.exe", "rider64.exe", "idea64.exe",
            "pycharm64.exe", "webstorm64.exe", "phpstorm64.exe", "clion64.exe", "goland64.exe", "datagrip64.exe", "studio64.exe",
            "sublime_text.exe", "notepad++.exe", "githubdesktop.exe", "gitkraken.exe", "sourcetree.exe", "fork.exe", "git-bash.exe",
            "docker desktop.exe", "postman.exe", "insomnia.exe", "heidisql.exe", "dbeaver.exe", "mongodbcompass.exe", "tableplus.exe",
            "mysqlworkbench.exe", "pgadmin4.exe", "filezilla.exe", "winscp.exe", "putty.exe", "mobaxterm.exe", "termius.exe",
            "wt.exe", "windowsterminal.exe", "powershell.exe", "pwsh.exe", "cmd.exe", "wsl.exe", "ubuntu.exe", "unity hub.exe",
            "unrealeditor.exe", "eclipse.exe", "netbeans64.exe", "arduino ide.exe", "virtualbox.exe", "vmware.exe", "laragon.exe",
            "xampp-control.exe", "ngrok.exe", "bruno.exe", "android studio.exe", "godot.exe" };
        static readonly string[] devPath = { @"\jetbrains\", @"\microsoft visual studio\", @"\android studio\", @"\git\", @"\nodejs\",
                                             @"\docker\", @"\python3", @"\python\" };
        static readonly string[] creativeExe = {
            "photoshop.exe", "illustrator.exe", "afterfx.exe", "adobe premiere pro.exe", "indesign.exe", "lightroom.exe", "figma.exe",
            "blender.exe", "resolve.exe", "inkscape.exe", "krita.exe", "canva.exe", "obs64.exe", "obs32.exe", "streamlabs obs.exe",
            "capcut.exe", "audacity.exe", "fl64.exe", "fl.exe", "reaper.exe", "clipstudiopaint.exe", "paintdotnet.exe", "aseprite.exe",
            "sketchup.exe", "acad.exe", "handbrake.exe", "shotcut.exe", "kdenlive.exe", "vegas.exe", "davinci resolve.exe" };
        static readonly string[] creativePath = { @"\adobe\", @"\affinity", @"\blender foundation\", @"\blackmagic design\",
                                                  @"\obs-studio\", @"\figma\", @"\image-line\", @"\ableton\", @"\autodesk\", @"\maxon",
                                                  @"\gimp", @"\capcut\" };
        static readonly string[] utilityExe = {
            "taskmgr.exe", "control.exe", "mmc.exe", "regedit.exe", "cleanmgr.exe", "ccleaner.exe", "ccleaner64.exe", "7zfm.exe",
            "winrar.exe", "everything.exe", "powertoys.exe", "cpuz.exe", "cpuz_x64.exe", "hwinfo64.exe", "hwmonitor.exe",
            "msiafterburner.exe", "crystaldiskinfo.exe", "diskinfo64.exe", "diskmark64.exe", "speccy64.exe", "revouninpro.exe",
            "revouninstaller.exe", "bleachbit.exe", "anydesk.exe", "teamviewer.exe", "parsecd.exe", "qbittorrent.exe",
            "icue.exe", "steelseriesgg.exe", "iriunwebcam.exe", "nordvpn.exe", "protonvpn.exe", "radeonsoftware.exe",
            "nvidia app.exe", "armourycrate.exe", "hyperx ngenuity.exe", "wootility.exe", "rufus.exe", "ventoy2disk.exe",
            "treesizefree.exe", "wiztree64.exe", "sharex.exe", "lightshot.exe", "autohotkey.exe" };
        static readonly string[] utilityPath = { @"\windows\system32\", @"\logitech", @"\lghub\", @"\corsair\", @"\razer\",
                                                 @"\steelseries\", @"\nzxt", @"\nvidia corporation\", @"\amd\", @"\asus\", @"\msi\",
                                                 @"\cpuid\", @"\7-zip\", @"\winrar\", @"\powertoys\", @"\hyperx", @"\ccleaner\",
                                                 @"\iriun webcam\", @"\teamviewer\", @"\anydesk" };
        static readonly string[] uninstallers = { "uninstall", "desinstalar", "deinstallieren", "désinstaller", "disinstalla", "unins000" };
        static readonly string[] installerWords = { "setup", "install", "installer", "instalador", "update" };

        static readonly Dictionary<string, string[]> hints = new Dictionary<string, string[]> {
            [Play] = new[] { "jogos", "jogo", "games", "game", "gaming", "spiele", "juegos", "jeux", "giochi" },
            [Dev] = new[] { "dev", "desenvolvimento", "development", "programacao", "programming", "code", "coding", "entwicklung",
                            "desarrollo", "developpement", "sviluppo" },
            [Creative] = new[] { "design", "criacao", "creative", "edicao", "editing", "stream", "streaming", "kreativ", "creativo",
                                 "creation", "creazione" },
            [Utilities] = new[] { "utilitarios", "utilities", "utils", "tools", "ferramentas", "perifericos", "peripherals", "drivers",
                                  "sistema", "system", "scripts", "herramientas", "outils", "strumenti", "werkzeuge", "utilidades",
                                  "utilitaires", "utilita" },
            [Apps] = new[] { "apps", "aplicativos", "programas", "programs", "applications", "anwendungen", "aplicaciones",
                             "applicazioni", "programme", "logiciels", "software" },
        };

        // The category a folder's own name asks for, such as "Jogos" or "Dev tools".
        public static string hintFor(string folder)
        {
            string name = plain(Path.GetFileName(folder.TrimEnd('\\')));
            string[] words = name.Split(new[] { ' ', '-', '_', '.', '&', '+' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var h in hints)
                if (words.Any(w => h.Value.Contains(w)))
                    return h.Key;
            return null;
        }

        static string plain(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in (text ?? "").Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(Char.ToLowerInvariant(c));
            return sb.ToString();
        }

        // A folder that only holds shortcuts and scripts, directly or one level down (like Shortcuts\Games), is a
        // way of sorting programs, so its contents are sorted instead of the folder. Anything else is a work folder.
        static bool launcherFolder(string folder, List<KeyValuePair<string, string>> inside)
        {
            string hint = hintFor(folder);
            foreach (string path in Shell.list(folder))
            {
                if (Directory.Exists(path))
                {
                    string subHint = hintFor(path) ?? hint;
                    foreach (string child in Shell.list(path))
                    {
                        if (Directory.Exists(child) || !launchables.Contains(extension(child)))
                            return false;
                        inside.Add(new KeyValuePair<string, string>(child, subHint));
                    }
                }
                else if (!launchables.Contains(extension(path)))
                    return false;
                else
                    inside.Add(new KeyValuePair<string, string>(path, hint));
                if (inside.Count > 300)
                    return false;
            }
            return inside.Count > 0;
        }

        static string extension(string path) => Path.GetExtension(path).ToLowerInvariant();

        // The category of one desktop item. A hint from the folder it was sorted into wins over guessing.
        public static string classify(string path, string hint = null)
        {
            if (Directory.Exists(path))
                return Folders;
            string ext = extension(path);
            string name = plain(Path.GetFileNameWithoutExtension(path));
            if (scripts.Contains(ext))
                return hint ?? Utilities;
            if (ext == ".lnk" || ext == ".url")
            {
                string target = Games.targetOf(path);
                if (!String.IsNullOrEmpty(target) && !target.Contains("://") && Directory.Exists(target))
                    return Folders;
                if (uninstallers.Any(name.Contains))
                    return Utilities;
                return hint ?? program(target);
            }
            if (ext == ".exe")
                return installerWords.Any(name.Contains) ? Files : hint ?? program(path);
            if (ext == ".msi")
                return Files;
            if (documents.Contains(ext))
                return Documents;
            if (media.Contains(ext))
                return Media;
            return Files;
        }

        // What kind of program a shortcut target or executable is.
        public static string program(string target)
        {
            string t = (target ?? "").ToLowerInvariant();
            string exe = Games.fileName(t);
            if (Games.isGameTarget(t))
                return Play;
            if (devExe.Contains(exe) || devPath.Any(t.Contains))
                return Dev;
            if (creativeExe.Contains(exe) || creativePath.Any(t.Contains))
                return Creative;
            if (utilityExe.Contains(exe) || utilityPath.Any(t.Contains))
                return Utilities;
            return Apps;
        }
    }
}
