using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Orla
{
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
    public class Group
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
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
            Name = "Novo grupo";
            Color = "#83D9BB";
            Width = 332;
            Height = 306;
            Items = new List<Entry>();
        }
    }
    public class Layout
    {
        public int Version { get; set; }
        public List<Group> Groups { get; set; }
        public bool StartupConfigured { get; set; }
        public bool StartupEnabled { get; set; }
        public bool Animations { get; set; }
        public double Opacity { get; set; }
        public Layout()
        {
            Version = 1;
            Groups = new List<Group>();
            Animations = true;
            Opacity = .72;
            StartupEnabled = true;
        }
    }
    public class Store
    {
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
        public void load(string seed)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    data = parse(File.ReadAllText(filePath));
                    return;
                }
                catch (Exception)
                {
                    string corrupt = filePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    File.Copy(filePath, corrupt, false);
                    if (File.Exists(filePath + ".bak"))
                        try
                        {
                            data = parse(File.ReadAllText(filePath + ".bak"));
                            File.Copy(filePath + ".bak", filePath, true);
                            recoveryNotice = "Seu layout foi recuperado do backup.";
                            return;
                        }
                        catch (Exception)
                        {
                        }
                    throw new InvalidDataException(
                        "Não foi possível ler seu layout. Uma cópia foi preservada em " + corrupt +
                        ". Seus arquivos não foram alterados.");
                }
            }
            data = File.Exists(seed) ? parse(File.ReadAllText(seed)) : Discovery.create();
            if (data.Groups.Count == 0)
                data.Groups.Add(new Group { Name = "Meu espaço", Visible = true, X = 24, Y = 64 });
            save();
        }
        public static Layout parse(string text)
        {
            Layout d = json().Deserialize<Layout>(text);
            if (d == null || d.Version != 1 || d.Groups == null || d.Groups.Count > 40)
                throw new InvalidDataException("Formato de layout inválido.");
            if (Double.IsNaN(d.Opacity) || Double.IsInfinity(d.Opacity))
                d.Opacity = .72;
            d.Opacity = Math.Max(.55, Math.Min(.95, d.Opacity));
            var ids = new HashSet<string>();
            foreach (Group g in d.Groups)
            {
                if (g == null || String.IsNullOrWhiteSpace(g.Id) || !ids.Add(g.Id) || g.Items == null ||
                    g.Items.Count > 3000)
                    throw new InvalidDataException("Grupo inválido.");
                if (String.IsNullOrWhiteSpace(g.Name))
                    g.Name = "Grupo";
                g.Name = g.Name.Substring(0, Math.Min(60, g.Name.Length));
                if (Double.IsNaN(g.X) || Double.IsInfinity(g.X))
                    g.X = 24;
                if (Double.IsNaN(g.Y) || Double.IsInfinity(g.Y))
                    g.Y = 64;
                if (Double.IsNaN(g.Width) || Double.IsInfinity(g.Width))
                    g.Width = 332;
                if (Double.IsNaN(g.Height) || Double.IsInfinity(g.Height))
                    g.Height = 306;
                g.Width = Math.Max(280, Math.Min(620, g.Width));
                g.Height = Math.Max(180, Math.Min(700, g.Height));
                foreach (Entry e in g.Items)
                {
                    if (e == null || String.IsNullOrWhiteSpace(e.Path) || String.IsNullOrWhiteSpace(e.Id) ||
                        !ids.Add(e.Id))
                        throw new InvalidDataException("Item inválido.");
                    if (String.IsNullOrWhiteSpace(e.Name))
                        e.Name = System.IO.Path.GetFileName(e.Path);
                }
            }
            return d;
        }
        public void save()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
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
        public bool add(Group group, string path)
        {
            if (group.Items.Count >= 3000)
                return false;
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;
            string full = System.IO.Path.GetFullPath(path);
            if (group.Items.Any(e => String.Equals(e.Path, full, StringComparison.OrdinalIgnoreCase)))
                return false;
            group.Items.Add(new Entry { Name = Directory.Exists(full)
                                                   ? new DirectoryInfo(full).Name
                                                   : System.IO.Path.GetFileNameWithoutExtension(full),
                                        Path = full });
            return true;
        }
        public bool move(string itemId, Group target, int index)
        {
            Group from = data.Groups.FirstOrDefault(g => g.Items.Any(e => e.Id == itemId));
            if (from == null || target == null || !data.Groups.Contains(target))
                return false;
            Entry item = from.Items.First(e => e.Id == itemId);
            if (from != target && target.Items.Count >= 3000)
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
    }
}
