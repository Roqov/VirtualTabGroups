using System;
using System.IO;
using System.Linq;
using System.Threading;
using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    public class StateSnapshotTests
    {
        private static string NewTempStatePath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "VTG-snap-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "state.json");
        }

        private static string[] SnapshotsOf(string statePath)
        {
            var dir = Path.GetDirectoryName(statePath);
            return Directory.GetFiles(dir, Path.GetFileName(statePath) + ".bak-*.json");
        }

        [Fact]
        public void Take_CreatesSnapshotWithTheSameContent()
        {
            var path = NewTempStatePath();
            File.WriteAllText(path, "{\"schemaVersion\":1}");

            var snapshot = StateSnapshot.Take(path);

            Assert.NotNull(snapshot);
            Assert.True(File.Exists(snapshot));
            Assert.Equal(File.ReadAllText(path), File.ReadAllText(snapshot));
        }

        [Fact]
        public void Take_IdenticalContent_DoesNotCreateAnotherSnapshot()
        {
            var path = NewTempStatePath();
            File.WriteAllText(path, "{\"schemaVersion\":1}");

            Assert.NotNull(StateSnapshot.Take(path));
            Assert.Null(StateSnapshot.Take(path));
            Assert.Single(SnapshotsOf(path));
        }

        [Fact]
        public void Take_ChangedContent_CreatesAnotherSnapshot()
        {
            var path = NewTempStatePath();
            File.WriteAllText(path, "{\"schemaVersion\":1}");

            Assert.NotNull(StateSnapshot.Take(path));
            Thread.Sleep(20);
            File.WriteAllText(path, "{\"schemaVersion\":1,\"x\":2}");

            Assert.NotNull(StateSnapshot.Take(path));
            Assert.Equal(2, SnapshotsOf(path).Length);
        }

        [Fact]
        public void Take_KeepsOnlyTheRequestedNumberOfSnapshots()
        {
            var path = NewTempStatePath();

            for (int i = 0; i < 6; i++)
            {
                File.WriteAllText(path, "{\"schemaVersion\":1,\"i\":" + i + "}");
                Assert.NotNull(StateSnapshot.Take(path, keep: 3));
                Thread.Sleep(20);
            }

            var snapshots = SnapshotsOf(path);
            Assert.Equal(3, snapshots.Length);

            // The kept files must be the newest three (i = 3, 4, 5).
            var contents = snapshots.Select(File.ReadAllText).ToList();
            Assert.Contains(contents, text => text.Contains("\"i\":5"));
            Assert.DoesNotContain(contents, text => text.Contains("\"i\":0"));
        }

        [Fact]
        public void Take_MissingFile_ReturnsNull()
        {
            Assert.Null(StateSnapshot.Take(NewTempStatePath()));
        }

        [Fact]
        public void Take_EmptyFile_ReturnsNull()
        {
            var path = NewTempStatePath();
            File.WriteAllText(path, "");

            Assert.Null(StateSnapshot.Take(path));
            Assert.Empty(SnapshotsOf(path));
        }

        [Fact]
        public void Take_NullPath_ReturnsNull()
        {
            Assert.Null(StateSnapshot.Take(null));
        }
    }
}
