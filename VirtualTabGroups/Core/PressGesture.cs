using System;

namespace VirtualTabGroups.Core
{
    /// <summary>What a completed left press means.</summary>
    public enum PressRelease
    {
        None,
        Click,
        LongPress
    }

    /// <summary>
    /// Press state machine for the long-press gestures. The action runs on release, so
    /// the same press can be cancelled before the button comes up: a drag, a move beyond
    /// the drag rectangle, or a right click while the left button is still held.
    /// </summary>
    public sealed class PressGesture
    {
        public const int DefaultThresholdMs = 650;

        private readonly int _thresholdMs;
        private DateTime _pressedAt;
        private bool _pressed;

        public PressGesture(int thresholdMs = DefaultThresholdMs)
        {
            _thresholdMs = Math.Max(1, thresholdMs);
        }

        public bool IsPressed => _pressed;

        public void Press(DateTime now)
        {
            _pressed = true;
            _pressedAt = now;
        }

        /// <summary>Abandons the gesture; a later release must not act.</summary>
        public void Cancel()
        {
            _pressed = false;
        }

        /// <summary>True once the press has been held long enough to show the hint.</summary>
        public bool IsThresholdReached(DateTime now)
        {
            return _pressed && ElapsedMs(now) >= _thresholdMs;
        }

        /// <summary>
        /// Ends the press and reports whether it was a long press. A cancelled or
        /// never-started press reports <see cref="PressRelease.None"/>.
        /// </summary>
        public PressRelease Release(DateTime now)
        {
            if (!_pressed) return PressRelease.None;

            _pressed = false;
            return ElapsedMs(now) >= _thresholdMs ? PressRelease.LongPress : PressRelease.Click;
        }

        private double ElapsedMs(DateTime now) => (now - _pressedAt).TotalMilliseconds;
    }
}
