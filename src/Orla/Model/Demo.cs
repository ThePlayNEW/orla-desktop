using System;
using System.IO;
using System.Linq;

namespace Orla
{
    // Non-personal sample content for screenshots and the smoke test. Files are created in a temporary folder so
    // their icons come from Windows like real ones.
    public static class Demo
    {
        public static Layout create()
        {
            string root = Path.Combine(Path.GetTempPath(), "Orla-demo");
            Directory.CreateDirectory(root);
            string file(string name)
            {
                string path = Path.Combine(root, name);
                if (!File.Exists(path))
                    File.WriteAllText(path, "");
                return path;
            }
            string folder(string name)
            {
                string path = Path.Combine(root, name);
                Directory.CreateDirectory(path);
                return path;
            }

            var layout = new Layout { Welcomed = true, StartupConfigured = true, OverlayHotkey = false };
            var projects = new Group { Name = Text.get("demo.projects"), Tint = Tints.Coral, Columns = 4, Rows = 2 };
            foreach (string name in new[] { "Site novo", "Identidade", "Viagem 2026", "Apresentações" })
                projects.Items.Add(new Entry { Name = name, Path = folder(name) });
            var documents = new Group { Name = Text.get("demo.documents"), Tint = Tints.Sand, Columns = 4, Rows = 2 };
            foreach (string name in new[] { "Proposta.pdf", "Orçamento 2026.xlsx", "Contrato.docx", "Notas.txt" })
                documents.Items.Add(new Entry { Name = Path.GetFileNameWithoutExtension(name), Path = file(name) });
            var inbox = new Group { Kind = PanelKind.Folder, Name = Text.get("demo.inbox"), FolderPath = folder("Entrada"),
                                    Tint = Tints.Sky, Columns = 4, Rows = 2 };
            foreach (string name in new[] { "Captura de tela.png", "Relatório final.pdf", "Mapa.jpg" })
                File.WriteAllText(Path.Combine(inbox.FolderPath, name), "");
            var quick = new Group { Name = Text.get("preset.quickAccess"), Tint = Tints.Moss, Columns = 4, Rows = 1 };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                quick.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            layout.Groups.AddRange(new[] { projects, documents, inbox, quick });
            return layout;
        }

        // What "Let Orla organize" would find on a typical desktop, for the documentation images.
        public static Organizer.Plan plan() => plan(false);

        // A crowded desktop, a couple of hundred items, to check that the organizer still lays it out calmly.
        public static Organizer.Plan crowded() => plan(true);

        static Organizer.Plan plan(bool crowded)
        {
            var plan = new Organizer.Plan { QuickAccess = Presets.All[0].Create() };
            var sample = new[] {
                new { Key = Organizer.Apps, Names = new[] { "Navegador", "Discord", "Spotify", "WhatsApp", "VLC", "Notion" } },
                new { Key = Organizer.Dev, Names = new[] { "VS Code", "GitHub Desktop", "Docker", "Postman", "Terminal" } },
                new { Key = Organizer.Utilities, Names = new[] { "Limpeza", "Mouse", "Teclado" } },
                new { Key = Organizer.Play, Names = new[] { "Steam", "Epic Games", "Minecraft", "EA", "Battle.net", "Riot", "GOG" } },
                new { Key = Organizer.Folders, Names = new[] { "Site novo", "Identidade", "Viagem 2026", "Apresentações" } },
                new { Key = Organizer.Documents, Names = new[] { "Proposta.pdf", "Contrato.docx", "Orçamento.xlsx", "Notas.txt" } },
                new { Key = Organizer.Media, Names = new[] { "Captura.png", "Logo.svg", "Vídeo.mp4" } },
            };
            int[] extra = { 22, 9, 14, 39, 19, 27, 35 };
            int i = 0;
            foreach (var s in sample)
            {
                var names = s.Names.ToList();
                if (crowded)
                    names.AddRange(Enumerable.Range(1, extra[i]).Select(n => s.Names[0] + " " + n));
                i++;
                plan.add(Organizer.panel(s.Key, names.Select(n => Path.Combine(Path.GetTempPath(), n))));
            }
            if (crowded)
            {
                plan.add(Organizer.panel(Organizer.Creative, Enumerable.Range(1, 9).Select(n => Path.Combine(Path.GetTempPath(), "Editor " + n))));
                plan.add(Organizer.panel(Organizer.Files, Enumerable.Range(1, 19).Select(n => Path.Combine(Path.GetTempPath(), "Pacote " + n + ".zip"))));
                plan.Groups.Sort((a, b) => Array.FindIndex(Organizer.Categories, c => c.Key == a.AutoCategory)
                                               .CompareTo(Array.FindIndex(Organizer.Categories, c => c.Key == b.AutoCategory)));
            }
            return plan;
        }
    }
}
