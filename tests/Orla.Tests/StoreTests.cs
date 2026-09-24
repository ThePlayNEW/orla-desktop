using System;
using System.IO;
using Xunit;

namespace Orla.Tests
{
    public sealed class StoreTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "Orla-unit-" + Guid.NewGuid().ToString("N"));
        readonly string file;
        readonly Store store;
        readonly Group first = new Group { Name = "A" };
        readonly Group second = new Group { Name = "B" };

        public StoreTests()
        {
            Directory.CreateDirectory(root);
            file = Path.Combine(root, "ação com espaço.txt");
            File.WriteAllText(file, "conteudo preservado");
            store = new Store(Path.Combine(root, "layout.json")) { data = new Layout() };
            store.data.Groups.Add(first);
            store.data.Groups.Add(second);
        }

        public void Dispose()
        {
            Directory.Delete(root, true);
        }

        string createFile(string name, string content)
        {
            string path = Path.Combine(root, name);
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public void addsUnicodePathOnceAndRejectsMissingPath()
        {
            Assert.True(store.add(first, file));
            Assert.False(store.add(first, file));
            Assert.False(store.add(first, Path.Combine(root, "missing")));
        }

        [Fact]
        public void movesReferenceBetweenGroupsWithoutTouchingFile()
        {
            store.add(first, file);
            Assert.True(store.move(first.Items[0].Id, second, 999));
            Assert.Empty(first.Items);
            Assert.Single(second.Items);
            Assert.Equal("conteudo preservado", File.ReadAllText(file));
        }

        [Fact]
        public void reordersWithinGroup()
        {
            string other = createFile("segundo.txt", "2");
            store.add(second, file);
            store.add(second, other);
            store.move(second.Items[1].Id, second, 0);
            Assert.Equal(other, second.Items[0].Path);
        }

        [Fact]
        public void rejectsMoveThatWouldDuplicateTargetReference()
        {
            store.add(first, file);
            store.add(second, file);
            Assert.False(store.move(first.Items[0].Id, second, 0));
        }

        [Fact]
        public void savesAtomicallyWithBackupAndReloads()
        {
            store.add(second, file);
            store.save();
            first.Name = "Alterado";
            store.save();
            Assert.True(File.Exists(store.filePath + ".bak"));

            var read = new Store(store.filePath);
            read.load("missing-seed");
            Assert.Equal("Alterado", read.data.Groups[0].Name);
            Assert.Single(read.data.Groups[1].Items);
        }

        [Fact]
        public void recoversCorruptLayoutFromBackupAcrossRestarts()
        {
            store.save();
            first.Name = "Alterado";
            store.save();
            File.WriteAllText(store.filePath, "{broken");

            var recovered = new Store(store.filePath);
            recovered.load("missing");
            Assert.NotNull(recovered.recoveryNotice);
            Assert.Equal("A", recovered.data.Groups[0].Name);

            var restarted = new Store(store.filePath);
            restarted.load("missing");
            Assert.Equal("A", restarted.data.Groups[0].Name);
        }

        [Fact]
        public void enablesStartupByDefaultUntilConfigured()
        {
            var layout = new Layout();
            Assert.True(layout.StartupEnabled);
            Assert.False(layout.StartupConfigured);
        }

        [Fact]
        public void clampsPanelSize()
        {
            first.Width = 9000;
            first.Height = -100;
            Layout bounded = Store.parse(Store.json().Serialize(store.data));
            Assert.Equal(620, bounded.Groups[0].Width);
            Assert.Equal(180, bounded.Groups[0].Height);
        }

        [Fact]
        public void parsesEmptyLayout()
        {
            Assert.Empty(Store.parse(Store.json().Serialize(new Layout())).Groups);
        }

        [Fact]
        public void rejectsUnknownVersion()
        {
            Assert.Throws<InvalidDataException>(() => Store.parse("{\"Version\":2,\"Groups\":[]}"));
        }

        [Fact]
        public void rejectsDuplicateIds()
        {
            store.add(first, file);
            first.Items.Add(new Entry { Id = first.Items[0].Id, Path = file, Name = "duplicado" });
            Assert.Throws<InvalidDataException>(() => Store.parse(Store.json().Serialize(store.data)));
        }
    }
}
