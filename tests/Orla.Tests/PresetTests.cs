using System;
using System.IO;
using Xunit;

namespace Orla.Tests
{
    public sealed class PresetTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "Orla-presets-" + Guid.NewGuid().ToString("N"));

        public PresetTests()
        {
            Directory.CreateDirectory(root);
        }

        public void Dispose()
        {
            Directory.Delete(root, true);
        }

        string link(string name, string url)
        {
            string path = Path.Combine(root, name + ".url");
            File.WriteAllText(path, "[InternetShortcut]\r\nURL=" + url + "\r\n");
            return path;
        }

        [Fact]
        public void recognizesStoreLinksAsGames()
        {
            Assert.True(Games.isGame(link("Counter", "steam://rungameid/730")));
            Assert.True(Games.isGame(link("Fortnite", "com.epicgames.launcher://apps/Fortnite?action=launch")));
            Assert.False(Games.isGame(link("Site", "https://example.com")));
            Assert.False(Games.isGame(Path.Combine(root, "missing.lnk")));
        }

        [Fact]
        public void everyPresetHasTextAndBuildsAPanel()
        {
            foreach (Preset p in Presets.All)
            {
                Assert.NotEqual("preset." + p.Key, p.Name);
                Assert.NotEqual("preset." + p.Key + "Hint", p.Hint);
                if (p.Key == "games" || p.Key == "apps")
                    continue;
                Group g = p.Create();
                Assert.False(String.IsNullOrWhiteSpace(g.Name));
                Assert.Equal(g.IsFolder, !String.IsNullOrEmpty(g.FolderPath));
            }
        }
    }
}
