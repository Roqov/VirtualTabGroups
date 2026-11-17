using System;
using System.Linq;
using VirtualTabGroups.Core;
using Xunit;

namespace VirtualTabGroups.Tests
{
    public class EditHistoryTests
    {
        [Fact]
        public void RecordRemoval_ThenTakeLast_ReturnsNodeWithPosition()
        {
            var root = new FolderNode("");
            var file = new FileNode("a.txt", @"C:\src\a.txt");
            root.Children.Add(file);

            var history = new EditHistory();
            history.RecordRemoval(file, root, 0);

            var record = history.TakeLast();

            Assert.Same(file, record.Node);
            Assert.Equal(root.Id, record.ParentId);
            Assert.Equal(0, record.Index);
            Assert.Equal(0, history.Count);
        }

        [Fact]
        public void TakeLast_IsLastInFirstOut()
        {
            var root = new FolderNode("");
            var first = new FileNode("a.txt", @"C:\src\a.txt");
            var second = new FileNode("b.txt", @"C:\src\b.txt");
            root.Children.Add(first);
            root.Children.Add(second);

            var history = new EditHistory();
            history.RecordRemoval(first, root, 0);
            history.RecordRemoval(second, root, 1);

            Assert.Same(second, history.TakeLast().Node);
            Assert.Same(first, history.TakeLast().Node);
            Assert.Null(history.TakeLast());
        }

        [Fact]
        public void Capacity_DropsTheOldestRecord()
        {
            var root = new FolderNode("");
            var history = new EditHistory(capacity: 2);

            for (int i = 0; i < 3; i++)
            {
                var file = new FileNode("f" + i + ".txt", @"C:\src\f" + i + ".txt");
                root.Children.Add(file);
                history.RecordRemoval(file, root, i);
            }

            Assert.Equal(2, history.Count);
            Assert.Equal("f2.txt", history.TakeLast().Node.Name);
            Assert.Equal("f1.txt", history.TakeLast().Node.Name);
        }

        [Fact]
        public void Clear_DropsEverything()
        {
            var root = new FolderNode("");
            var file = new FileNode("a.txt", @"C:\src\a.txt");
            root.Children.Add(file);

            var history = new EditHistory();
            history.RecordRemoval(file, root, 0);
            history.Clear();

            Assert.Equal(0, history.Count);
        }

        [Fact]
        public void InsertNode_RestoresAtTheRecordedIndex()
        {
            var root = new FolderNode("");
            var a = new FileNode("a.txt", @"C:\src\a.txt");
            var b = new FileNode("b.txt", @"C:\src\b.txt");
            var c = new FileNode("c.txt", @"C:\src\c.txt");
            root.Children.Add(a);
            root.Children.Add(b);
            root.Children.Add(c);

            var history = new EditHistory();
            history.RecordRemoval(b, root, 1);
            Assert.True(TreeMutator.RemoveNode(b, root));

            var record = history.TakeLast();
            Assert.True(TreeMutator.InsertNode(root, record.ParentId, record.Index, record.Node));

            Assert.Equal(new[] { "a.txt", "b.txt", "c.txt" }, root.Children.Select(n => n.Name));
        }

        [Fact]
        public void InsertNode_RestoresAFolderWithItsContents()
        {
            var root = new FolderNode("");
            var folder = new FolderNode("Work");
            var child = new FolderNode("Sub");
            child.Children.Add(new FileNode("deep.txt", @"C:\deep.txt"));
            folder.Children.Add(child);
            root.Children.Add(folder);

            var history = new EditHistory();
            history.RecordRemoval(folder, root, 0);
            Assert.True(TreeMutator.RemoveNode(folder, root));

            var record = history.TakeLast();
            Assert.True(TreeMutator.InsertNode(root, record.ParentId, record.Index, record.Node));

            var restored = Assert.IsType<FolderNode>(Assert.Single(root.Children));
            var restoredSub = Assert.IsType<FolderNode>(Assert.Single(restored.Children));
            Assert.Equal("deep.txt", Assert.Single(restoredSub.Children).Name);
        }

        [Fact]
        public void InsertNode_WhenParentIsGone_ReturnsFalse()
        {
            var root = new FolderNode("");
            var parent = new FolderNode("Work");
            var file = new FileNode("a.txt", @"C:\src\a.txt");
            parent.Children.Add(file);
            root.Children.Add(parent);

            var history = new EditHistory();
            history.RecordRemoval(file, parent, 0);
            Assert.True(TreeMutator.RemoveNode(file, root));
            Assert.True(TreeMutator.RemoveNode(parent, root));

            var record = history.TakeLast();
            Assert.False(TreeMutator.InsertNode(root, record.ParentId, record.Index, record.Node));
        }

        [Fact]
        public void InsertNode_AlreadyInTree_ReturnsFalse()
        {
            var root = new FolderNode("");
            var file = new FileNode("a.txt", @"C:\src\a.txt");
            root.Children.Add(file);

            Assert.False(TreeMutator.InsertNode(root, root.Id, 0, file));
            Assert.Single(root.Children);
        }

        [Fact]
        public void InsertNode_ClampsIndexToTheEnd()
        {
            var root = new FolderNode("");
            var file = new FileNode("a.txt", @"C:\src\a.txt");

            Assert.True(TreeMutator.InsertNode(root, root.Id, 99, file));
            Assert.Single(root.Children);
        }

        [Fact]
        public void FindById_FindsNestedFolder()
        {
            var root = new FolderNode("");
            var parent = new FolderNode("Work");
            var child = new FolderNode("Sub");
            child.Children.Add(new FileNode("a.txt", @"C:\a.txt"));
            parent.Children.Add(child);
            root.Children.Add(parent);

            Assert.Same(child, TreeMutator.FindById(root, child.Id));
            Assert.Null(TreeMutator.FindById(root, Guid.NewGuid()));
        }
    }
}
