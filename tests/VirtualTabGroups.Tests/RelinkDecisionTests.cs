using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    public class RelinkDecisionTests
    {
        [Fact]
        public void Decide_PreSaveCaptureWinsOverBufferMap()
        {
            var decision = RelinkDecision.Decide(
                @"C:\src\old.txt", @"C:\src\other.txt", @"C:\src\new.txt");

            Assert.Equal(@"C:\src\old.txt", decision.PreviousPath);
            Assert.Equal(PathAnchor.PreSaveCapture, decision.Anchor);
            Assert.True(decision.CanRelink);
            Assert.Equal("pre-save capture", decision.AnchorLabel);
        }

        [Fact]
        public void Decide_FallsBackToBufferMapWhenCaptureIsMissing()
        {
            var decision = RelinkDecision.Decide(null, @"C:\src\draft.txt", @"C:\src\draft-as.txt");

            Assert.Equal(@"C:\src\draft.txt", decision.PreviousPath);
            Assert.Equal(PathAnchor.BufferMap, decision.Anchor);
            Assert.True(decision.CanRelink);
            Assert.Equal("buffer map", decision.AnchorLabel);
        }

        [Fact]
        public void Decide_EmptyCaptureFallsBackToBufferMap()
        {
            var decision = RelinkDecision.Decide("", @"C:\src\draft.txt", @"C:\src\draft-as.txt");

            Assert.Equal(PathAnchor.BufferMap, decision.Anchor);
            Assert.Equal(@"C:\src\draft.txt", decision.PreviousPath);
        }

        [Fact]
        public void Decide_NoSource_ReportsNoneAndNoRelink()
        {
            var decision = RelinkDecision.Decide(null, null, @"C:\src\new.txt");

            Assert.Null(decision.PreviousPath);
            Assert.Equal(PathAnchor.None, decision.Anchor);
            Assert.False(decision.CanRelink);
            Assert.Equal("no source", decision.AnchorLabel);
        }

        [Fact]
        public void Decide_PlainSave_DoesNotRelink()
        {
            var decision = RelinkDecision.Decide(
                @"C:\src\file.txt", @"C:\src\file.txt", @"C:\src\file.txt");

            Assert.False(decision.CanRelink);
            Assert.Equal(PathAnchor.PreSaveCapture, decision.Anchor);
        }

        [Fact]
        public void Decide_PathComparisonIsCaseInsensitive()
        {
            var decision = RelinkDecision.Decide(
                @"C:\src\File.TXT", null, @"c:\SRC\file.txt");

            Assert.False(decision.CanRelink);
        }

        [Fact]
        public void Decide_UnrootedDraftNameIsRelinkedToItsNewPath()
        {
            var decision = RelinkDecision.Decide("new 1", @"new 1", @"C:\work\saved.txt");

            Assert.True(decision.CanRelink);
            Assert.Equal("new 1", decision.PreviousPath);
        }

        [Fact]
        public void Decide_MissingCurrentPath_DoesNotRelink()
        {
            var decision = RelinkDecision.Decide(@"C:\src\old.txt", null, null);

            Assert.False(decision.CanRelink);
        }
    }
}
