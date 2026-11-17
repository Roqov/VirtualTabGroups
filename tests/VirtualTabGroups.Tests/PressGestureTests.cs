using System;
using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    public class PressGestureTests
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Release_BeforeThreshold_IsAClick()
        {
            var gesture = new PressGesture(650);
            gesture.Press(Start);

            Assert.Equal(PressRelease.Click, gesture.Release(Start.AddMilliseconds(200)));
        }

        [Fact]
        public void Release_AfterThreshold_IsALongPress()
        {
            var gesture = new PressGesture(650);
            gesture.Press(Start);

            Assert.Equal(PressRelease.LongPress, gesture.Release(Start.AddMilliseconds(650)));
        }

        [Fact]
        public void Cancel_ThenRelease_DoesNothing()
        {
            var gesture = new PressGesture(650);
            gesture.Press(Start);
            gesture.Cancel();

            Assert.False(gesture.IsPressed);
            Assert.Equal(PressRelease.None, gesture.Release(Start.AddSeconds(5)));
        }

        [Fact]
        public void Cancel_ThenPressAgain_UsesTheNewPress()
        {
            var gesture = new PressGesture(650);
            gesture.Press(Start);
            gesture.Cancel();
            gesture.Press(Start.AddMinutes(1));

            Assert.Equal(PressRelease.Click, gesture.Release(Start.AddMinutes(1).AddMilliseconds(10)));
        }

        [Fact]
        public void Release_WithoutPress_DoesNothing()
        {
            var gesture = new PressGesture();
            Assert.Equal(PressRelease.None, gesture.Release(Start));
        }

        [Fact]
        public void Threshold_IsReachedOnlyWhilePressed()
        {
            var gesture = new PressGesture(650);
            Assert.False(gesture.IsThresholdReached(Start));

            gesture.Press(Start);
            Assert.False(gesture.IsThresholdReached(Start.AddMilliseconds(500)));
            Assert.True(gesture.IsThresholdReached(Start.AddMilliseconds(700)));

            gesture.Cancel();
            Assert.False(gesture.IsThresholdReached(Start.AddMilliseconds(700)));
        }

        [Fact]
        public void ThresholdBelowOneMillisecond_IsClamped()
        {
            var gesture = new PressGesture(0);
            gesture.Press(Start);

            // The threshold is clamped to one millisecond, so the release must not
            // happen at the very same instant.
            Assert.Equal(PressRelease.LongPress, gesture.Release(Start.AddMilliseconds(1)));
        }
    }
}
