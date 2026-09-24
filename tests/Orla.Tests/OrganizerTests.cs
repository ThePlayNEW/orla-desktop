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

        static Organizer.Plan planOf(params (string key, string[] names)[] panels)
        {
            var plan = new Organizer.Plan();
            foreach (var p in panels)
                plan.add(Organizer.panel(p.key, p.names.Select(n => @"C:\Desk\" + n)));
            return plan;
        }

        static readonly Screen screen = new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 1920, Bottom = 1040 } };

        [Fact]
        public void organizingAgainAddsOnlyWhatIsNewAndKeepsPeoplesWork()
        {
            Layout current = Organizer.build(planOf((Organizer.Apps, new[] { "a.lnk", "b.lnk" })), new Layout(), true, true, screen);
            Group apps = current.Groups.First(g => g.AutoCategory == Organizer.Apps);
            apps.Name = "Meus apps";
            apps.Tint = Tints.Coral;
            double x = apps.X, y = apps.Y;
            var done = new Organizer.Completion();
            Layout next = Organizer.complete(planOf((Organizer.Apps, new[] { "a.lnk", "b.lnk", "c.lnk" }),
                                                   (Organizer.Play, new[] { "g1.url", "g2.url" })), current, true, true, new[] { screen }, done);
            Group sameApps = next.Groups.First(g => g.Id == apps.Id);
            Assert.Equal(new[] { "a", "b", "c" }, sameApps.Items.Select(e => e.Name));
            Assert.Equal("Meus apps", sameApps.Name);
            Assert.Equal(Tints.Coral, sameApps.Tint);
            Assert.Equal(x, sameApps.X);
            Assert.Equal(y, sameApps.Y);
            Group games = next.Groups.Single(g => g.AutoCategory == Organizer.Play);
            Assert.Contains(games.Id, done.Fresh);
            Assert.Equal(3, done.Added);
            // The live layout is untouched until the preview is applied.
            Assert.Equal(2, apps.Items.Count);
            var rects = next.Groups.Where(g => g.Visible).Select(g => Screens.rectOf(g, next.IconSize, 1)).ToList();
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.False(Screens.overlaps(rects[i], rects[j]));
        }

        [Fact]
        public void whatPeopleMovedGoesWhereTheyPutIt()
        {
            Layout current = Organizer.build(planOf((Organizer.Apps, new[] { "a.lnk", "b.lnk" }), (Organizer.Play, new[] { "g1.url", "g2.url" })),
                                             new Layout(), true, true, screen);
            Group games = current.Groups.First(g => g.AutoCategory == Organizer.Play);
            current.Learned["wallpaper.exe"] = games.Id;
            Organizer.Plan again = planOf((Organizer.Apps, new[] { "a.lnk", "b.lnk", "Wallpaper.lnk" }), (Organizer.Play, new[] { "g1.url", "g2.url" }));
            again.Identities[@"C:\Desk\Wallpaper.lnk"] = "wallpaper.exe";
            Layout next = Organizer.complete(again, current, true, true, new[] { screen }, new Organizer.Completion());
            Assert.Contains("Wallpaper", next.Groups.First(g => g.Id == games.Id).Items.Select(e => e.Name));
            Assert.DoesNotContain("Wallpaper", next.Groups.First(g => g.AutoCategory == Organizer.Apps).Items.Select(e => e.Name));
        }

        [Fact]
        public void rulesFollowTheirCategoryToAFreshLayoutAndRulesAboutGonePanelsGoAway()
        {
            Layout current = Organizer.build(planOf((Organizer.Play, new[] { "g1.url", "g2.url" })), new Layout(), true, true, screen);
            Group games = current.Groups.First(g => g.AutoCategory == Organizer.Play);
            current.Learned["x"] = games.Id;
            current.Learned["y"] = "a panel that is gone";
            Layout fresh = Organizer.build(planOf((Organizer.Play, new[] { "g1.url", "g2.url" })), current, true, true, screen);
            Assert.Equal(fresh.Groups.First(g => g.AutoCategory == Organizer.Play).Id, fresh.Learned["x"]);
            Assert.False(fresh.Learned.ContainsKey("y"));
            Layout loaded = Store.parse(Store.json().Serialize(current));
            Assert.True(loaded.Learned.ContainsKey("x"));
            Assert.False(loaded.Learned.ContainsKey("y"));
        }

        [Fact]
        public void organizingAgainNeverGoesPastThePanelLimit()
        {
            var current = new Layout { Welcomed = true };
            for (int i = 0; i < Store.MaxGroups - 1; i++)
                current.Groups.Add(new Group { Name = "P" + i, X = 24, Y = 24 });
            Layout next = Organizer.complete(planOf((Organizer.Play, new[] { "g1.url", "g2.url" }), (Organizer.Dev, new[] { "d1.lnk", "d2.lnk" })),
                                             current, true, true, new[] { screen }, new Organizer.Completion());
            Assert.True(next.Groups.Count <= Store.MaxGroups);
            Store.parse(Store.json().Serialize(next));
        }

        [Fact]
        public void withASecondMonitorTheToolsMoveThereAndWorkStaysOnTheMainScreen()
        {
            var main = new Screen { Dpi = 96, Primary = true, Work = new RECT { Right = 2560, Bottom = 1040 } };
            var other = new Screen { Dpi = 96, Work = new RECT { Left = -1920, Right = 0, Bottom = 1040 } };
            Layout next = Organizer.build(planOf((Organizer.Apps, new[] { "a.lnk", "b.lnk" }), (Organizer.Documents, new[] { "d.pdf", "e.pdf" })),
                                          new Layout(), true, true, new[] { main, other }, true);
            Group apps = next.Groups.First(g => g.AutoCategory == Organizer.Apps);
            Group docs = next.Groups.First(g => g.AutoCategory == Organizer.Documents);
            Assert.True(apps.X < 0 && Screens.rectOf(apps, next.IconSize, 1).Right <= 0);
            Assert.True(docs.X >= 0);
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
