using System;
using System.Collections.Generic;

namespace VirtualTabGroups.Core
{
    /// <summary>
    /// Last known path of every document, keyed by Notepad++'s BufferID.
    ///
    /// NPPN_FILESAVED reports only the new path, so a tree entry can be relinked to it
    /// only while the path it used to have is still known at that moment.
    /// NPPN_FILEBEFORESAVE is the exact source for that, but notifications can arrive
    /// out of order or be missed entirely; remembering the path whenever a buffer is
    /// opened, activated or swept at startup gives the save handler a second anchor.
    ///
    /// Keys are plain numbers so the map stays independent of Notepad++ types: the
    /// plugin converts its BufferID before calling in.
    /// </summary>
    public sealed class BufferPathMap
    {
        private readonly Dictionary<long, string> _pathByBuffer = new Dictionary<long, string>();

        /// <summary>Number of buffers with a remembered path.</summary>
        public int Count => _pathByBuffer.Count;

        /// <summary>
        /// Records the path a buffer has right now, replacing whatever was known.
        /// An empty path drops the entry: a buffer without a path has nothing to
        /// compare a future save against.
        /// </summary>
        public void Remember(long bufferId, string path)
        {
            if (bufferId == 0) return;

            if (string.IsNullOrEmpty(path)) _pathByBuffer.Remove(bufferId);
            else _pathByBuffer[bufferId] = path;
        }

        /// <summary>True when the buffer is known, with its last path in <paramref name="path"/>.</summary>
        public bool TryGetPath(long bufferId, out string path)
        {
            return _pathByBuffer.TryGetValue(bufferId, out path);
        }

        /// <summary>Drops a buffer, for example when its document was closed.</summary>
        public void Forget(long bufferId)
        {
            if (bufferId != 0) _pathByBuffer.Remove(bufferId);
        }

        /// <summary>Drops every buffer; nothing may outlive the session.</summary>
        public void Clear() => _pathByBuffer.Clear();

        /// <summary>
        /// Reads the path the buffer had before <paramref name="currentPath"/> and then
        /// remembers the new one. Returns null when nothing was known yet or the path
        /// did not actually change (compared case-insensitively, as Windows does), so
        /// callers only relink on a real move.
        /// </summary>
        public string TakePrevious(long bufferId, string currentPath)
        {
            if (bufferId == 0 || string.IsNullOrEmpty(currentPath)) return null;

            if (!_pathByBuffer.TryGetValue(bufferId, out var previous))
            {
                _pathByBuffer[bufferId] = currentPath;
                return null;
            }

            _pathByBuffer[bufferId] = currentPath;

            if (string.IsNullOrEmpty(previous)) return null;
            if (string.Equals(previous, currentPath, StringComparison.OrdinalIgnoreCase)) return null;
            return previous;
        }
    }
}
