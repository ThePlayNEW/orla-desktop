using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Orla.Tests
{
    public sealed class OrganizerTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "Orla-organizer-" + Guid.NewGuid().ToString("N"));

        public OrganizerTests()
        {
            Directory.CreateDirectory(root);
        }

        public void Dispose()
        {
            Directory.Delete(root, true);
        }

        string file(string name, string content = "")
        {
            string path = Path.Combine(root, name);
            File.WriteAllText(path, content);
            return path;
        }

        string link(string name, string url) => file(name + ".url", "[InternetShortcut]\r\nURL=" + url + "\r\n");

        [Fact]
        public void sortsFilesByKind()
        {
            Assert.Equal(Organizer.Folders, Organizer.classify(root));
            Assert.Equal(Organizer.Documents, Organizer.classify(file("Proposta.pdf")));
            Assert.Equal(Organizer.Media, Organizer.classify(file("Captura.PNG")));
            Assert.Equal(Organizer.Utilities, Organizer.classify(file("limpar.bat")));
            Assert.Equal(Organizer.Files, Organizer.classify(file("pacote.zip")));
            Assert.Equal(Organizer.Files, Organizer.classify(file("ProgramSetup.exe")));
        }

        [Fact]
        public void sortsShortcutsByWhatTheyOpen()
        {
            Assert.Equal(Organizer.Play, Organizer.classify(link("GTA", "steam://rungameid/271590")));
            Assert.Equal(Organizer.Apps, Organizer.classify(link("Site", "https://example.com")));
            Assert.Equal(Organizer.Dev, Organizer.program(@"C:\Users\A\AppData\Local\Programs\Microsoft VS Code\Code.exe"));
            Assert.Equal(Organizer.Dev, Organizer.program(@"C:\Program Files\JetBrains\Rider\bin\rider64.exe"));
            Assert.Equal(Organizer.Creative, Organizer.program(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe"));
            Assert.Equal(Organizer.Utilities, Organizer.program(@"C:\Program Files\LGHUB\system_tray\lghub_system_tray.exe"));
            Assert.Equal(Organizer.Play, Organizer.program(@"C:\Users\A\AppData\Local\FiveM\FiveM.exe"));
            Assert.Equal(Organizer.Apps, Organizer.program(@"C:\Program Files\Google\Chrome\Application\chrome.exe"));
        }

        [Fact]
        public void aFolderNameTellsTheCategory()
        {
            Assert.Equal(Organizer.Play, Organizer.hintFor(@"C:\Desktop\Atalhos\Jogos"));
            Assert.Equal(Organizer.Utilities, Organizer.hintFor(@"C:\Desktop\Utilitários"));
            Assert.Equal(Organizer.Dev, Organizer.hintFor(@"C:\Desktop\Dev tools"));
            Assert.Null(Organizer.hintFor(@"C:\Desktop\Atalhos"));
            // A hint from the folder wins over guessing, the way the person sorted it.
            Assert.Equal(Organizer.Play, Organizer.classify(link("Wallpaper", "https://example.com"), Organizer.Play));
        }

        [Fact]
        public void newItemsGoToTheirPanelOrItsFallback()
        {
            var apps = new Group { AutoCategory = Organizer.Apps };
            var games = new Group { AutoCategory = Organizer.Play };
            var manual = new Group();
            string game = link("Jogo", "steam://rungameid/1");
            string tool = file("script.cmd");
            Assert.Same(games, Organizer.home(game, new[] { manual, apps, games }));
            // No utilities panel was made, so a script joins the apps.
            Assert.Same(apps, Organizer.home(tool, new[] { manual, apps, games }));
            Assert.Null(Organizer.home(file("Notas.txt"), new[] { manual, apps, games }));
        }

        [Fact]
        public void defaultNamesFollowTheLanguageAndTypedNamesStay()
        {
            var layout = new Layout();
            layout.Groups.Add(new Group { Name = "Jogos" });
            layout.Groups.Add(new Group { Name = "Acesso rápido" });
            layout.Groups.Add(new Group { Name = "Meus jogos" });
            layout.Groups.Add(new Group { Name = "games" });
            layout.Groups.Add(new Group { Name = "Applications", AutoCategory = Organizer.Apps });
            try
            {
                Text.load("en");
                Assert.True(Organizer.retitle(layout));
                // Exact names only, and the organizer's own category before a preset with the same name.
                Assert.Equal(new[] { "Games", "Quick access", "Meus jogos", "games", "Apps" }, layout.Groups.Select(g => g.Name));
                Assert.False(Organizer.retitle(layout));
                Text.load("pt-BR");
                Organizer.retitle(layout);
                Assert.Equal("Apps", layout.Groups[4].Name);
                Assert.Equal("Jogos", layout.Groups[0].Name);
            }
            finally
            {
                Text.load("system");
            }
        }

        [Fact]
        public void aLayoutFromBeforeTheOrganizerStillLoads()
        {
            Layout layout = Store.parse("{\"Version\":2,\"Groups\":[]}");
            Assert.NotNull(layout.SortedFolders);
            Assert.False(layout.AutoOrganize);
        }

        [Fact]
        public void anItemCountsAsOrganizedWhenAPanelShowsItOrSomethingInside()
        {
            var organized = new Organized(new[] { @"C:\Desk\Atalhos\Jogos\Steam.lnk", @"C:\Desk\Notas.txt" });
            Assert.True(organized.Contains(@"C:\Desk\Notas.txt"));
            Assert.True(organized.Contains(@"C:\Desk\Atalhos"));
            Assert.True(organized.Contains(@"C:\desk\atalhos\"));
            Assert.False(organized.Contains(@"C:\Desk\Atalhos2"));
            Assert.False(organized.Contains(@"C:\Desk\Outros"));
        }
    }
}
