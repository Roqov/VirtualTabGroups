using System;
using System.IO;
using System.Linq;
using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    /// <summary>
    /// Two Notepad++ windows share one plugins\config folder, so both write the same
    /// state.json. These tests pin the rule that the later writer never destroys the
    /// other window's file without leaving a copy behind.
    /// </summary>
    public class StateStoreMultiInstanceTests
    {
        private static string NewTempStatePath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "VTG-multi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "state.json");
        }

        private sealed class CapturingObserver : IStateStoreObserver
        {
            public string ForeignBackupPath;
            public Exception SaveError;

            public void OnRecoveredFromCorruptFile(string backupPath) { }
            public void OnFutureSchemaVersion(int versionFound, int currentVersion) { }
            public void OnSaveFailed(Exception ex) => SaveError = ex;
            public void OnForeignStateDetected(string backupPath) => ForeignBackupPath = backupPath;
        }

        [Fact]
        public void Save_FileChangedOnDisk_CopiesForeignVersionAndReportsIt()
        {
            var path = NewTempStatePath();
            var observer = new CapturingObserver();

            using (var store = new StateStore(path, observer, TimeSpan.FromSeconds(10)))
            {
                store.Load();
                store.MarkDirty(new FolderNode(""), null);
                store.Flush();
                Assert.Null(observer.ForeignBackupPath);

                // The other window writes a different tree while this one is alive.
                File.WriteAllText(path,
                    "{\"schemaVersion\":1,\"marker\":\"other-instance\",\"root\":{\"type\":\"folder\"," +
                    "\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"\",\"children\":[]}}");

                var mine = new FolderNode("");
                mine.Children.Add(new FileNode("mine.txt", @"C:\mine.txt"));
                store.MarkDirty(mine, null);
                store.Flush();
            }

            Assert.NotNull(observer.ForeignBackupPath);
            Assert.True(File.Exists(observer.ForeignBackupPath));
            Assert.Contains("other-instance", File.ReadAllText(observer.ForeignBackupPath));

            // This session's own tree still wins the live file.
            Assert.Contains("mine.txt", File.ReadAllText(path));
        }

        [Fact]
        public void Save_RepeatedWrites_DoNotProduceForeignCopies()
        {
            var path = NewTempStatePath();
            var observer = new CapturingObserver();

            using (var store = new StateStore(path, observer, TimeSpan.FromSeconds(10)))
            {
                store.Load();
                for (int i = 0; i < 3; i++)
                {
                    var root = new FolderNode("");
                    root.Children.Add(new FileNode("f" + i + ".txt", @"C:\f" + i + ".txt"));
                    store.MarkDirty(root, null);
                    store.Flush();
                }
            }

            Assert.Null(observer.ForeignBackupPath);
            var dir = Path.GetDirectoryName(path);
            Assert.Empty(Directory.GetFiles(dir, "state.json.foreign-*"));
        }

        [Fact]
        public void Save_UsesUniqueTempFilesAndLeavesNoneBehind()
        {
            var path = NewTempStatePath();
            var dir = Path.GetDirectoryName(path);

            using (var store = new StateStore(path, null, TimeSpan.FromSeconds(10)))
            {
                store.Load();
                for (int i = 0; i < 5; i++)
                {
                    store.MarkDirty(new FolderNode(""), null);
                    store.Flush();
                }
            }

            Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void Load_ValidFile_KeepsAStartupSnapshot()
        {
            var path = NewTempStatePath();
            File.WriteAllText(path,
                "{\"schemaVersion\":1,\"root\":{\"type\":\"folder\"," +
                "\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"\",\"children\":[]}}");

            using (var store = new StateStore(path))
            {
                store.Load();
            }

            var snapshots = Directory.GetFiles(Path.GetDirectoryName(path), "state.json.bak-*.json");
            Assert.Single(snapshots);
            Assert.Contains("schemaVersion", File.ReadAllText(snapshots[0]));
        }

        [Fact]
        public void Load_MissingFile_CreatesNoSnapshot()
        {
            var path = NewTempStatePath();

            using (var store = new StateStore(path))
            {
                store.Load();
            }

            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path), "state.json.bak-*.json"));
        }
    }
}
