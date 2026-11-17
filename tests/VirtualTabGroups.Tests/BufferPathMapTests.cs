using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    /// <summary>
    /// The map is the second anchor for relinking: NPPN_FILESAVED carries only the new
    /// path, and NPPN_FILEBEFORESAVE is not guaranteed to arrive for every save. These
    /// tests pin the contract the plugin relies on — a save reports the path the buffer
    /// had before, and the map never invents a relink on its own.
    /// </summary>
    public class BufferPathMapTests
    {
        [Fact]
        public void TakePrevious_UnknownBuffer_ReturnsNullAndRemembersPath()
        {
            var map = new BufferPathMap();

            var previous = map.TakePrevious(1, @"C:\docs\a.txt");

            Assert.Null(previous);
            Assert.True(map.TryGetPath(1, out var remembered));
            Assert.Equal(@"C:\docs\a.txt", remembered);
        }

        [Fact]
        public void TakePrevious_ChangedPath_ReturnsThePathTheBufferHadBefore()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");

            var previous = map.TakePrevious(1, @"C:\docs\b.txt");

            Assert.Equal(@"C:\docs\a.txt", previous);
            Assert.True(map.TryGetPath(1, out var remembered));
            Assert.Equal(@"C:\docs\b.txt", remembered);
        }

        [Fact]
        public void TakePrevious_SecondSave_ComparesAgainstThePathOfTheFirstSave()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");
            map.TakePrevious(1, @"C:\docs\b.txt");

            var previous = map.TakePrevious(1, @"C:\docs\c.txt");

            Assert.Equal(@"C:\docs\b.txt", previous);
        }

        [Fact]
        public void TakePrevious_SamePath_ReturnsNullBecauseNothingMoved()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");

            Assert.Null(map.TakePrevious(1, @"C:\docs\a.txt"));
        }

        [Fact]
        public void TakePrevious_PathDiffersOnlyInCase_ReturnsNull()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\Docs\A.TXT");

            Assert.Null(map.TakePrevious(1, @"c:\docs\a.txt"));
        }

        [Fact]
        public void TakePrevious_UnsavedBufferName_ReturnsTheNameSoTheEntryCanBeAdopted()
        {
            var map = new BufferPathMap();
            map.Remember(7, "новый 1");

            var previous = map.TakePrevious(7, @"C:\docs\saved.txt");

            Assert.Equal("новый 1", previous);
        }

        [Fact]
        public void TakePrevious_EmptyCurrentPath_ReturnsNullAndKeepsTheKnownPath()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");

            Assert.Null(map.TakePrevious(1, string.Empty));
            Assert.Null(map.TakePrevious(1, null));

            Assert.True(map.TryGetPath(1, out var remembered));
            Assert.Equal(@"C:\docs\a.txt", remembered);
            Assert.Equal(1, map.Count);
        }

        [Fact]
        public void Remember_EmptyPath_DropsTheEntry()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");

            map.Remember(1, null);

            Assert.False(map.TryGetPath(1, out _));
            Assert.Equal(0, map.Count);
        }

        [Fact]
        public void Remember_ZeroBufferId_IsIgnored()
        {
            var map = new BufferPathMap();

            map.Remember(0, @"C:\docs\a.txt");
            map.Forget(0);

            Assert.Equal(0, map.Count);
            Assert.Null(map.TakePrevious(0, @"C:\docs\b.txt"));
        }

        [Fact]
        public void Forget_DropsOnlyThatBuffer()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");
            map.Remember(2, @"C:\docs\b.txt");

            map.Forget(1);

            Assert.False(map.TryGetPath(1, out _));
            Assert.True(map.TryGetPath(2, out var other));
            Assert.Equal(@"C:\docs\b.txt", other);
        }

        [Fact]
        public void Clear_DropsEveryBuffer()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");
            map.Remember(2, "новый 1");

            map.Clear();

            Assert.Equal(0, map.Count);
            Assert.Null(map.TakePrevious(1, @"C:\docs\b.txt"));
        }

        [Fact]
        public void Buffers_DoNotSharePaths()
        {
            var map = new BufferPathMap();
            map.Remember(1, @"C:\docs\a.txt");
            map.Remember(2, @"C:\docs\b.txt");

            var first = map.TakePrevious(1, @"C:\docs\c.txt");

            Assert.Equal(@"C:\docs\a.txt", first);
            Assert.True(map.TryGetPath(2, out var untouched));
            Assert.Equal(@"C:\docs\b.txt", untouched);
        }
    }
}
