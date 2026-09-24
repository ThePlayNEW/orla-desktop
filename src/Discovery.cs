using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Orla
{
    public static class Discovery
    {
        public static Layout create()
        {
            var layout = new Layout();
            var projects = new Group { Name = "Projetos", Color = "#B6A3F5", Visible = true };
            var apps = new Group { Name = "Aplicativos", Color = "#91DFC4", Visible = true };
            var files = new Group { Name = "Arquivos", Color = "#8BBEFF", Visible = true };
            var places = new Group { Name = "Acesso rápido", Color = "#E9C58A", Visible = true };
            layout.Groups.AddRange(new[] { projects, apps, files, places });
            foreach (string root in new[] {
                         Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                         Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                     })
            {
                if (!Directory.Exists(root))
                    continue;
                try
                {
                    foreach (string path in Directory.GetFileSystemEntries(root).OrderBy(p => p))
                    {
                        FileAttributes attr = File.GetAttributes(path);
                        if ((attr & (FileAttributes.Hidden | FileAttributes.System |
                                     FileAttributes.ReparsePoint)) != 0)
                            continue;
                        string extension = Path.GetExtension(path).ToLowerInvariant();
                        Group g = Directory.Exists(path)                         ? projects
                                  : (extension == ".lnk" || extension == ".url") ? apps
                                                                                 : files;
                        if (!g.Items.Any(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                            g.Items.Add(new Entry { Name = Directory.Exists(path)
                                                               ? Path.GetFileName(path)
                                                               : Path.GetFileNameWithoutExtension(path),
                                                    Path = path });
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }
            places.Items.Add(new Entry { Name = "Este Computador", Path = "shell:MyComputerFolder" });
            places.Items.Add(new Entry { Name = "Lixeira", Path = "shell:RecycleBinFolder" });
            places.Items.Add(new Entry { Name = "Downloads", Path = "shell:Downloads" });
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(documents))
                places.Items.Add(new Entry { Name = "Documentos", Path = documents });
            position(layout);
            return layout;
        }
        public static void position(Layout layout)
        {
            Rect a = SystemParameters.WorkArea;
            int i = 0;
            foreach (Group g in layout.Groups.Where(g => g.Visible))
            {
                g.Width = 332;
                g.Height = 306;
                g.X = i % 2 == 0 ? a.Left + 24 : a.Right - 356;
                g.Y = a.Top + 48 + (i / 2) * 326;
                i++;
            }
        }
    }
}
