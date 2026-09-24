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
        public void clampsPanelSizeAndOpacity()
        {
            first.Columns = 90;
            first.Rows = -3;
            store.data.Opacity = 0.2;
            Layout bounded = Store.parse(Store.json().Serialize(store.data));
            Assert.Equal(Group.MaxColumns, bounded.Groups[0].Columns);
            Assert.Equal(Group.MinRows, bounded.Groups[0].Rows);
            Assert.Equal(Layout.MinOpacity, bounded.Opacity);
        }

        [Fact]
        public void sizesPanelsInWholeTiles()
        {
            double tile = PanelMetrics.tile("medium");
            Assert.Equal(PanelMetrics.ChromeWidth + 4 * tile, PanelMetrics.width(4, "medium"));
            Assert.Equal(4, PanelMetrics.columnsFor(PanelMetrics.width(4, "medium") + tile * 0.4, "medium"));
            Assert.Equal(5, PanelMetrics.columnsFor(PanelMetrics.width(4, "medium") + tile * 0.6, "medium"));
            Assert.Equal(Group.MinRows, PanelMetrics.rowsFor(0, "large"));
            Assert.Equal(Group.MaxColumns, PanelMetrics.columnsFor(100000, "small"));
        }

        [Fact]
        public void parsesEmptyLayout()
        {
            Assert.Empty(Store.parse(Store.json().Serialize(new Layout())).Groups);
        }

        [Fact]
        public void rejectsUnknownVersion()
        {
            Assert.Throws<InvalidDataException>(() => Store.parse("{\"Version\":3,\"Groups\":[]}"));
        }

        [Fact]
        public void migratesVersionOneGroupsToCollections()
        {
            string v1 = "{\"Version\":1,\"Opacity\":0.6,\"Groups\":[{\"Id\":\"g1\",\"Name\":\"Apps\",\"Color\":\"#8BBEFF\"," +
                        "\"Visible\":true,\"X\":24,\"Y\":64,\"Width\":332,\"Height\":306,\"Items\":[{\"Id\":\"e1\"," +
                        "\"Name\":\"Nota\",\"Path\":\"C:\\nota.txt\"}]}]}";
            Layout migrated = Store.parse(v1, 1.5, out bool wasMigrated);
            Assert.True(wasMigrated);
            Assert.Equal(Layout.CurrentVersion, migrated.Version);
            Assert.False(migrated.CleanDesktop);
            Assert.True(migrated.Welcomed);
            Assert.Equal(PanelKind.Collection, migrated.Groups[0].Kind);
            Assert.Equal(Tints.Sky, migrated.Groups[0].Tint);
            Assert.Equal(36, migrated.Groups[0].X);
            Assert.Equal(96, migrated.Groups[0].Y);
            Assert.Equal(Layout.MinOpacity, migrated.Opacity);
            Assert.Equal(3, migrated.Groups[0].Columns);
            Assert.Equal(3, migrated.Groups[0].Rows);
            Assert.Single(migrated.Groups[0].Items);
        }

        [Fact]
        public void rejectsFolderPanelWithoutFolderAndUnknownKind()
        {
            first.Kind = PanelKind.Folder;
            Assert.Throws<InvalidDataException>(() => Store.parse(Store.json().Serialize(store.data)));
            first.Kind = "gallery";
            Assert.Throws<InvalidDataException>(() => Store.parse(Store.json().Serialize(store.data)));
        }

        [Fact]
        public void keepsReferencesOutOfFolderPanels()
        {
            var folder = new Group { Kind = PanelKind.Folder, FolderPath = root };
            store.data.Groups.Add(folder);
            store.add(first, file);
            Assert.False(store.add(folder, file));
            Assert.False(store.move(first.Items[0].Id, folder, 0));
            Assert.True(store.organized().Contains(file));
        }

        [Fact]
        public void removesOnlyTheReference()
        {
            store.add(first, file);
            Assert.True(store.remove(first.Items[0].Id));
            Assert.Empty(first.Items);
            Assert.True(File.Exists(file));
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
