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
        public bool migrated;

        public Store(string path)
        {
            filePath = path;
        }

        public static JavaScriptSerializer json()
        {
            return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
        }

        // Returns false when there was no saved layout, so the caller can run the welcome flow.
        public bool load(string seed, double legacyScale = 1)
        {
            if (File.Exists(filePath))
            {
                string text = readShared(filePath);
                try
                {
                    data = parse(text, legacyScale, out migrated);
                }
                catch (Exception e) when (!(e is IOException || e is UnauthorizedAccessException))
                {
                    string corrupt = filePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    File.Copy(filePath, corrupt, false);
                    if (File.Exists(filePath + ".bak"))
                        try
                        {
                            data = parse(readShared(filePath + ".bak"), legacyScale, out migrated);
                            File.Copy(filePath + ".bak", filePath, true);
                            recoveryNotice = Text.get("notice.recovered");
                            return true;
                        }
                        catch (Exception)
                        {
                        }
                    throw new InvalidDataException(Text.format("error.layoutUnreadable", corrupt));
                }
                if (migrated)
                    try
                    {
                        // Keep the version 1 file as it was, once, before the first save in the new format.
                        if (!File.Exists(filePath + ".v1"))
                            File.Copy(filePath, filePath + ".v1");
                        save();
                    }
                    catch (IOException)
                    {
                        // The migrated layout is in memory; it is saved with the next change.
                    }
                return true;
            }
            data = File.Exists(seed) ? parse(File.ReadAllText(seed), legacyScale, out migrated) : new Layout();
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
            return parse(text, 1, out _);
        }

        public static Layout parse(string text, double legacyScale, out bool migrated)
        {
            var raw = json().DeserializeObject(text) as Dictionary<string, object>;
            if (raw == null || !(raw.TryGetValue("Version", out object version) && version is int))
                throw new InvalidDataException("Invalid layout.");
            migrated = (int)version == 1;
            if (!migrated && (int)version != Layout.CurrentVersion)
                throw new InvalidDataException("Unsupported layout version.");

            Layout d = json().Deserialize<Layout>(text);
            if (d == null || d.Groups == null || d.Groups.Count > MaxGroups)
                throw new InvalidDataException("Invalid layout.");
            if (migrated)
                migrate(d, raw, legacyScale);

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
                if (g.Kind != PanelKind.Collection && g.Kind != PanelKind.Folder)
                    throw new InvalidDataException("Invalid panel kind.");
                if (g.IsFolder && String.IsNullOrWhiteSpace(g.FolderPath))
                    throw new InvalidDataException("Folder panel without a folder.");
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

        // Version 1 stored every group as references, sized and positioned in device-independent units on the
        // primary monitor, and always hid the Windows icons. Groups become collections of about the same size, and
        // the Windows icons come back: Orla now shares the desktop with them unless people choose a clean desktop.
        static void migrate(Layout d, Dictionary<string, object> raw, double legacyScale)
        {
            var oldGroups = raw.TryGetValue("Groups", out object list) ? list as object[] : null;
            for (int i = 0; i < d.Groups.Count; i++)
            {
                Group g = d.Groups[i];
                if (g == null)
                    continue;
                g.Kind = PanelKind.Collection;
                var old = oldGroups != null && i < oldGroups.Length ? oldGroups[i] as Dictionary<string, object> : null;
                string color = old != null && old.TryGetValue("Color", out object c) ? c as string : null;
                g.Tint = legacyTint(color);
                g.X = finite(g.X, 24) * legacyScale;
                g.Y = finite(g.Y, 64) * legacyScale;
                double width = old != null && old.TryGetValue("Width", out object w) ? Convert.ToDouble(w) : 332;
                double height = old != null && old.TryGetValue("Height", out object h) ? Convert.ToDouble(h) : 306;
                g.Columns = (int)Math.Round((width - PanelMetrics.ChromeWidth) / PanelMetrics.tile("medium"));
                g.Rows = (int)Math.Round((height - PanelMetrics.ChromeHeight) / PanelMetrics.tile("medium"));
            }
            d.CleanDesktop = false;
            d.Welcomed = true;
            d.OverlayHotkey = true;
            d.Theme = "system";
            d.Language = "system";
            d.IconSize = "medium";
        }

        static string legacyTint(string color)
        {
            switch ((color ?? "").ToUpperInvariant())
            {
                case "#8BBEFF": return Tints.Sky;
                case "#E9C58A": return Tints.Sand;
                case "#B6A3F5": return Tints.Coral;
                default: return Tints.SeaGlass;
            }
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

        public Group find(string groupId)
        {
            return data.Groups.FirstOrDefault(g => g.Id == groupId);
        }

        public Group owner(string itemId)
        {
            return data.Groups.FirstOrDefault(g => g.Items.Any(e => e.Id == itemId));
        }

        public bool add(Group group, string path)
        {
            if (group.IsFolder || group.Items.Count >= MaxItems)
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
            if (from == null || target == null || target.IsFolder || !data.Groups.Contains(target))
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
        public HashSet<string> organizedPaths()
        {
            return new HashSet<string>(data.Groups.Where(g => !g.IsFolder).SelectMany(g => g.Items).Select(e => e.Path),
                                       StringComparer.OrdinalIgnoreCase);
        }
    }
}
