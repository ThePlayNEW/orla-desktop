using System;
using System.IO;
using System.Linq;

namespace Orla
{
    // Non-personal sample content for screenshots and the smoke test. Files are created in a temporary folder so
    // their icons come from Windows like real ones.
    public static class Demo
    {
        // A minute of a computer playing a game: steady graphics, a busy processor, a download in the background.
        public static Reading[] readings()
        {
            Hardware.demo();
            var random = new Random(7);
            var list = new System.Collections.Generic.List<Reading>();
            for (int i = 0; i < Sensors.HistoryLength; i++)
            {
                double t = i * Sensors.Interval / 1000.0, wave = Math.Sin(t / 6.0), game = t < 12 ? t / 12.0 : 1;
                var r = new Reading();
                r.Cpu = 18 + 30 * game + 6 * wave + random.NextDouble() * 5;
                r.Mhz = 4850 + random.Next(-60, 60);
                r.Cores = Enumerable.Range(0, 16).Select(c => Math.Min(100, r.Cpu * (c % 2 == 0 ? 1.3 : 0.7) + random.NextDouble() * 10)).ToArray();
                r.Processes = 214;
                r.Threads = 3120;
                r.Uptime = 3 * 3600 + 25 * 60 + t;
                r.MemoryTotal = 32.0 * (1UL << 30);
                r.MemoryUsed = (11.2 + 3.1 * game) * (1UL << 30);
                r.CommitLimit = 37.0 * (1UL << 30);
                r.Committed = r.MemoryUsed * 1.3;
                var g = new GpuReading { Adapter = Hardware.Gpus[0], Load = 8 + 82 * game + 4 * wave, Dedicated = (2.1 + 6.3 * game) * (1UL << 30),
                                         Shared = 0.4 * (1UL << 30), Temperature = 46 + 20 * game, Fan = game < 0.5 ? Double.NaN : 1400 + 300 * game,
                                         Power = 20 + 60 * game, MemoryClock = 10501,
                                         CoreClock = 2475 };
                g.Engines["3D"] = g.Load;
                g.Engines["Copy"] = 6 * game;
                g.Engines["VideoDecode"] = 2;
                r.Gpus.Add(g);
                r.Disks.Add(new DiskReading { Instance = "0 C:", Active = 4 + 3 * Math.Abs(wave) + (i == 30 ? 30 : 0), Read = 2.4e6 + (i == 30 ? 90e6 : 0), Write = 0.8e6 });
                r.Disks.Add(new DiskReading { Instance = "1 D:", Active = 0, Read = 0, Write = 0 });
                r.Down = 2.6e6 + 0.8e6 * wave + random.NextDouble() * 0.3e6;
                r.Up = 0.12e6 + random.NextDouble() * 0.05e6;
                if (i == Sensors.HistoryLength - 1)
                    r.Top = new System.Collections.Generic.List<ProcessReading> {
                        new ProcessReading { Name = "Game", Label = Text.get("demo.game"), Cpu = 31.4, Memory = 6.2 * (1UL << 30), Gpu = 86 },
                        new ProcessReading { Name = "Browser", Label = Text.get("demo.browser"), Cpu = 4.2, Memory = 2.1 * (1UL << 30), Gpu = 2 },
                        new ProcessReading { Name = "Voice", Label = Text.get("demo.voice"), Cpu = 2.8, Memory = 0.4 * (1UL << 30), Gpu = 1 },
                        new ProcessReading { Name = "Music", Label = Text.get("demo.music"), Cpu = 1.1, Memory = 0.3 * (1UL << 30), Gpu = 0 },
                        new ProcessReading { Name = "explorer", Label = "Windows Explorer", Cpu = 0.6, Memory = 0.15 * (1UL << 30), Gpu = 0 },
                        new ProcessReading { Name = "Orla", Label = "Orla Desktop", Cpu = 0.3, Memory = 0.06 * (1UL << 30), Gpu = 0 },
                    };
                list.Add(r);
            }
            return list.ToArray();
        }

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
