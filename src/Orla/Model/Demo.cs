using System;
using System.IO;

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
            var quick = new Group { Name = Text.get("starter.quickAccess"), Tint = Tints.Moss, Columns = 4, Rows = 1 };
            foreach (string path in new[] { "shell:MyComputerFolder", "shell:Downloads", "shell:RecycleBinFolder" })
                quick.Items.Add(new Entry { Name = Shell.displayName(path), Path = path });
            layout.Groups.AddRange(new[] { projects, documents, inbox, quick });
            return layout;
        }
    }
}
