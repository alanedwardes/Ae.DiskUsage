﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DiskUsage
{
    public sealed class TreeItem
    {
        public TreeItem(DirectoryInfo directory, TreeItem parent)
        {
            Directory = directory;
            Parent = parent;
            Refresh();
        }

        public IList<TreeItem> Children => _children;
        public DirectoryInfo Directory { get; }
        public TreeItem Parent { get; }

        private long _fileSize;
        private IList<TreeItem> _children = new List<TreeItem>();
        private long _totalSize;
        public long TotalSize => _totalSize;
        private void ChildrenChanged() => Interlocked.Exchange(ref _totalSize, _fileSize + _children.Sum(x => x.TotalSize));
        private Task RefreshTask { get; set; } = Task.CompletedTask;

        public void Refresh()
        {
            if (RefreshTask.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            RefreshTask = Task.Run(() => Calculate());
        }

        public bool IsCalculating => RefreshTask.Status != TaskStatus.RanToCompletion;

        public async Task Calculate()
        {
            FileSystemInfo[] children;
            try
            {
                children = Directory.EnumerateFileSystemInfos().Where(x => !x.Attributes.HasFlag(FileAttributes.ReparsePoint)).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            Interlocked.Exchange(ref _fileSize, children.OfType<FileInfo>().Sum(x => x.Length));
            Interlocked.Exchange(ref _children, children.OfType<DirectoryInfo>().Select(x => new TreeItem(x, this)).ToArray());

            foreach (var child in Children)
            {
                await child.RefreshTask;
                child.ChildrenChanged();
            }

            var owner = this;
            while (owner != null)
            {
                owner.ChildrenChanged();
                owner = owner.Parent;
            }
        }

        public override string ToString() => Directory.Name;
    }
}
