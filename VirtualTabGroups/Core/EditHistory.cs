using System;
using System.Collections.Generic;

namespace VirtualTabGroups.Core
{
    /// <summary>One removed node plus where it used to sit, so it can be put back.</summary>
    public sealed class RemovalRecord
    {
        public RemovalRecord(TreeNodeModel node, Guid parentId, int index)
        {
            Node = node;
            ParentId = parentId;
            Index = index;
        }

        public TreeNodeModel Node { get; }

        public Guid ParentId { get; }

        public int Index { get; }
    }

    /// <summary>
    /// Session-only undo for tree removals. Removing an entry is deliberate but easy to
    /// trigger by accident (Delete, long press), and the tree is hand-curated, so the
    /// last few removals can be put back. Nothing is persisted: a restart starts with
    /// an empty history, exactly like the tree itself.
    /// </summary>
    public sealed class EditHistory
    {
        private readonly int _capacity;
        private readonly List<RemovalRecord> _records = new List<RemovalRecord>();

        public EditHistory(int capacity = 50)
        {
            _capacity = Math.Max(1, capacity);
        }

        public int Count => _records.Count;

        /// <summary>
        /// Records a removal that is about to happen. <paramref name="index"/> is the
        /// node's position inside <paramref name="parent"/> before the removal.
        /// </summary>
        public void RecordRemoval(TreeNodeModel node, FolderNode parent, int index)
        {
            if (node == null || parent == null) return;

            _records.Add(new RemovalRecord(node, parent.Id, Math.Max(0, index)));
            if (_records.Count > _capacity) _records.RemoveAt(0);
        }

        /// <summary>Takes the most recent removal, or null when nothing can be undone.</summary>
        public RemovalRecord TakeLast()
        {
            if (_records.Count == 0) return null;

            var last = _records[_records.Count - 1];
            _records.RemoveAt(_records.Count - 1);
            return last;
        }

        public void Clear() => _records.Clear();
    }
}
