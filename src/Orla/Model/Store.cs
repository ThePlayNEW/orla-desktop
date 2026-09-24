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
                try
                {
                    data = parse(File.ReadAllText(filePath), legacyScale, out migrated);
                    if (migrated)
                        save();
                    return true;
                }
                catch (Exception)
                {
                    string corrupt = filePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    File.Copy(filePath, corrupt, false);
                    if (File.Exists(filePath + ".bak"))
                        try
                        {
                            data = parse(File.ReadAllText(filePath + ".bak"), legacyScale, out migrated);
                            File.Copy(filePath + ".bak", filePath, true);
                            recoveryNotice = Text.get("notice.recovered");
                            return true;
                        }
                        catch (Exception)
                        {
                        }
                    throw new InvalidDataException(Text.format("error.layoutUnreadable", corrupt));
                }
            }
            data = File.Exists(seed) ? parse(File.ReadAllText(seed), legacyScale, out migrated) : new Layout();
            return File.Exists(seed);
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
            if (!new[] { "system", "pt-BR", "en" }.Contains(d.Language))
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
                g.Width = clamp(g.Width, Group.MinWidth, Group.MaxWidth, Group.DefaultWidth);
                g.Height = clamp(g.Height, Group.MinHeight, Group.MaxHeight, Group.DefaultHeight);
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

        // Version 1 stored every group as references, positioned in device-independent units on the primary
        // monitor, and always hid the Windows icons. Keep that experience for people who upgrade.
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
            }
            d.CleanDesktop = true;
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
            if (File.Exists(filePath))
                File.Replace(temp, filePath, filePath + ".bak", true);
            else
                File.Move(temp, filePath);
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
