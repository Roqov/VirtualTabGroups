using System;

namespace VirtualTabGroups.Core
{
    /// <summary>Which notification, if any, supplied the path an entry used to have.</summary>
    public enum PathAnchor
    {
        None,
        PreSaveCapture,
        BufferMap
    }

    /// <summary>
    /// Chooses the old path a save should relink from. NPPN_FILESAVED reports only the
    /// new path, so the decision depends entirely on which anchor is still available:
    /// the exact capture from NPPN_FILEBEFORESAVE, or the last known path of the buffer.
    /// Kept out of the plugin so the precedence and the "nothing to relink" cases can be
    /// tested without a running Notepad++.
    /// </summary>
    public sealed class RelinkDecision
    {
        private RelinkDecision(string previousPath, PathAnchor anchor, bool canRelink)
        {
            PreviousPath = previousPath;
            Anchor = anchor;
            CanRelink = canRelink;
        }

        public string PreviousPath { get; }

        public PathAnchor Anchor { get; }

        /// <summary>True when an entry actually has to move to another path.</summary>
        public bool CanRelink { get; }

        public string AnchorLabel
        {
            get
            {
                switch (Anchor)
                {
                    case PathAnchor.PreSaveCapture: return "pre-save capture";
                    case PathAnchor.BufferMap: return "buffer map";
                    default: return "no source";
                }
            }
        }

        /// <summary>
        /// The capture wins because it is taken immediately before the write; the map is
        /// the fallback for a notification that never arrived. Either anchor may be null
        /// or empty, and a path equal to the new one means "same file" (a plain Ctrl+S),
        /// which must not touch the tree.
        /// </summary>
        public static RelinkDecision Decide(string capturedPath, string rememberedPath, string currentPath)
        {
            string previous;
            PathAnchor anchor;

            if (!string.IsNullOrEmpty(capturedPath))
            {
                previous = capturedPath;
                anchor = PathAnchor.PreSaveCapture;
            }
            else if (!string.IsNullOrEmpty(rememberedPath))
            {
                previous = rememberedPath;
                anchor = PathAnchor.BufferMap;
            }
            else
            {
                previous = null;
                anchor = PathAnchor.None;
            }

            bool canRelink = !string.IsNullOrEmpty(previous)
                && !string.IsNullOrEmpty(currentPath)
                && !string.Equals(previous, currentPath, StringComparison.OrdinalIgnoreCase);

            return new RelinkDecision(previous, anchor, canRelink);
        }
    }
}
