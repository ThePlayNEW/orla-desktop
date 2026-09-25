using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Orla
{
    // Loads, validates and atomically saves the layout. It only stores references: nothing here moves,
    // renames or deletes the files a panel points to.
    public class Store
    {
        public const int MaxGroups = 40, MaxItems = 3000;

        public string filePath;
        public Layout data;
        public string recoveryNotice;

        public Store(string path)
        {
            filePath = path;
        }

        public static JavaScriptSerializer json()
        {
            return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
        }

        // Returns false when there was no saved layout, so the caller can run the welcome flow.
        public bool load(string seed)
        {
            if (File.Exists(filePath))
            {
                string text = readShared(filePath);
                try
                {
                    data = parse(text);
                }
                catch (Exception e) when (!(e is IOException || e is UnauthorizedAccessException))
                {
                    string corrupt = filePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    File.Copy(filePath, corrupt, false);
                    if (File.Exists(filePath + ".bak"))
                        try
                        {
                            data = parse(readShared(filePath + ".bak"));
                            File.Copy(filePath + ".bak", filePath, true);
                            recoveryNotice = Text.get("notice.recovered");
                            return true;
                        }
                        catch (Exception)
                        {
                        }
                    throw new InvalidDataException(Text.format("error.layoutUnreadable", corrupt));
                }
                return true;
            }
            data = File.Exists(seed) ? parse(File.ReadAllText(seed)) : new Layout();
            return File.Exists(seed);
        }

        // Antivirus and sync tools can hold the file for a moment; that is not corruption, so wait a little.
        static string readShared(string path)
        {
            for (int attempt = 0; ; attempt++)
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException) when (attempt < 10)
                {
                    System.Threading.Thread.Sleep(150);
                }
        }

        public static Layout parse(string text)
        {
            var raw = json().DeserializeObject(text) as Dictionary<string, object>;
            if (raw == null || !(raw.TryGetValue("Version", out object version) && version is int))
                throw new InvalidDataException("Invalid layout.");
            if ((int)version != Layout.CurrentVersion)
                throw new InvalidDataException("Unsupported layout version.");

            Layout d = json().Deserialize<Layout>(text);
            if (d != null && d.SortedFolders == null)
                d.SortedFolders = new List<string>();
            if (d != null)
                d.Learned = new Dictionary<string, string>(d.Learned ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            // Rules about panels that no longer exist go away.
            if (d?.Groups != null)
                foreach (string stale in d.Learned.Where(r => !d.Groups.Any(g => g != null && g.Id == r.Value)).Select(r => r.Key).ToList())
                    d.Learned.Remove(stale);
            if (d == null || d.Groups == null || d.Groups.Count > MaxGroups)
                throw new InvalidDataException("Invalid layout.");

            d.Opacity = clamp(d.Opacity, Layout.MinOpacity, Layout.MaxOpacity, Layout.DefaultOpacity);
            if (!new[] { "system", "light", "dark" }.Contains(d.Theme))
                d.Theme = "system";
            if (d.Language != "system" && !Text.Languages.Contains(d.Language))
                d.Language = "system";
            if (!new[] { "small", "medium", "large" }.Contains(d.IconSize))
                d.IconSize = "medium";

            var ids = new HashSet<string>();
            foreach (Group g in d.Groups)
            {
                if (g == null || String.IsNullOrWhiteSpace(g.Id) || !ids.Add(g.Id) || g.Items == null ||
                    g.Items.Count > MaxItems)
                    throw new InvalidDataException("Invalid panel.");
                if (g.Kind != PanelKind.Collection && g.Kind != PanelKind.Folder && g.Kind != PanelKind.Sensors)
                    throw new InvalidDataException("Invalid panel kind.");
                if (g.IsFolder && String.IsNullOrWhiteSpace(g.FolderPath))
                    throw new InvalidDataException("Folder panel without a folder.");
                if (g.IsSensors)
                    g.Items.Clear();
                if (String.IsNullOrWhiteSpace(g.Name))
                    g.Name = "Orla";
                g.Name = g.Name.Trim().Substring(0, Math.Min(60, g.Name.Trim().Length));
                if (!Tints.All.Contains(g.Tint))
                    g.Tint = Tints.SeaGlass;
                g.X = finite(g.X, 24);
                g.Y = finite(g.Y, 64);
                g.Columns = g.Columns == 0 ? Group.DefaultColumns : Math.Max(Group.MinColumns, Math.Min(Group.MaxColumns, g.Columns));
                g.Rows = g.Rows == 0 ? Group.DefaultRows : Math.Max(Group.MinRows, Math.Min(Group.MaxRows, g.Rows));
                foreach (Entry e in g.Items)
                {
                    if (e == null || String.IsNullOrWhiteSpace(e.Path) || String.IsNullOrWhiteSpace(e.Id) ||
                        !ids.Add(e.Id))
                        throw new InvalidDataException("Invalid item.");
                    if (String.IsNullOrWhiteSpace(e.Name))
                        e.Name = Path.GetFileName(e.Path);
                }
            }
            d.Version = Layout.CurrentVersion;
            return d;
        }

        static double finite(double value, double fallback)
        {
            return Double.IsNaN(value) || Double.IsInfinity(value) ? fallback : value;
        }

        static double clamp(double value, double min, double max, double fallback)
        {
            return Math.Max(min, Math.Min(max, finite(value, fallback)));
        }

        public void save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            string temp = filePath + ".tmp";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json().Serialize(data));
            using (var s = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                s.Write(bytes, 0, bytes.Length);
                s.Flush(true);
            }
            for (int attempt = 0; ; attempt++)
                try
                {
                    if (File.Exists(filePath))
                        File.Replace(temp, filePath, filePath + ".bak", true);
                    else
                        File.Move(temp, filePath);
                    return;
                }
                catch (IOException) when (attempt < 5)
                {
                    // Antivirus or sync tools sometimes hold the file for a moment.
                    System.Threading.Thread.Sleep(120);
                }
        }

        public Group owner(string itemId)
        {
            return data.Groups.FirstOrDefault(g => g.Items.Any(e => e.Id == itemId));
        }

        public bool add(Group group, string path)
        {
            if (!group.IsCollection || group.Items.Count >= MaxItems)
                return false;
            if (!Shell.exists(path))
                return false;
            string full = Shell.isVirtual(path) ? path : Path.GetFullPath(path);
            if (group.Items.Any(e => String.Equals(e.Path, full, StringComparison.OrdinalIgnoreCase)))
                return false;
            group.Items.Add(new Entry { Name = Shell.displayName(full), Path = full });
            return true;
        }

        public bool move(string itemId, Group target, int index)
        {
            Group from = owner(itemId);
            if (from == null || target == null || !target.IsCollection || !data.Groups.Contains(target))
                return false;
            Entry item = from.Items.First(e => e.Id == itemId);
            if (from != target && target.Items.Count >= MaxItems)
                return false;
            if (from != target &&
                target.Items.Any(e => String.Equals(e.Path, item.Path, StringComparison.OrdinalIgnoreCase)))
                return false;
            int old = from.Items.IndexOf(item);
            from.Items.Remove(item);
            if (from == target && index > old)
                index--;
            target.Items.Insert(Math.Max(0, Math.Min(index, target.Items.Count)), item);
            return true;
        }

        public bool remove(string itemId)
        {
            Group from = owner(itemId);
            return from != null && from.Items.RemoveAll(e => e.Id == itemId) > 0;
        }

        // Paths already placed in a collection panel, used by folder panels that show only what is left over.
        public Organized organized() => Organized.of(data);
    }

    // What the panels already show, so the desktop inbox lists only what is new. An item counts when a panel points at
    // it or at something inside it, such as a folder of shortcuts whose shortcuts are in panels.
    public class Organized
    {
        readonly List<string> paths;

        public static Organized of(Layout layout)
        {
            var shown = layout.Groups.Where(g => g.IsCollection).SelectMany(g => g.Items).Select(e => e.Path)
                              .Concat(layout.Groups.Where(g => g.IsFolder && g.FolderPath != Shell.DesktopFolder).Select(g => g.FolderPath));
            return new Organized(shown.Where(p => !Shell.isVirtual(p)));
        }

        public Organized(IEnumerable<string> shown)
        {
            paths = shown.Where(p => !String.IsNullOrEmpty(p)).ToList();
        }

        public bool Contains(string path) => paths.Any(p => Shell.within(p, path));
    }
}
