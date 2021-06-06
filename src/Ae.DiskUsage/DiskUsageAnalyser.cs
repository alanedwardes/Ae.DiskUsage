using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.DiskUsage
{
    public struct LightFileInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public override string ToString() => Name;
    }

    public sealed class TreeItem
    {
        public TreeItem(DirectoryInfo directory, TreeItem parent)
        {
            Directory = directory;
            Parent = parent;
            Refresh();
        }

        public ConcurrentBag<LightFileInfo> Files { get; } = new ConcurrentBag<LightFileInfo>();
        public ConcurrentBag<TreeItem> Children { get; } = new ConcurrentBag<TreeItem>();
        public DirectoryInfo Directory { get; }
        public TreeItem Parent { get; }

        private long _cachedSize;
        private long CalculatedSize => Files.Sum(x => x.Size) + Children.Sum(x => x.CachedSize);
        public long CachedSize => _cachedSize;

        private IReadOnlyList<TreeItem> _cachedSizes = new TreeItem[0];
        public IReadOnlyList<TreeItem> CachedChildrenBySizeDescending => _cachedSizes;
        private IReadOnlyList<TreeItem> CalculatedChildrenBySizeDescending => Children.OrderByDescending(x => x.CachedSize).ToArray();

        private void FilesChanged()
        {
            Interlocked.Exchange(ref _cachedSize, CalculatedSize);
            Interlocked.Exchange(ref _cachedSizes, CalculatedChildrenBySizeDescending);
        }

        private void ChildrenChanged()
        {
            Interlocked.Exchange(ref _cachedSize, Files.Sum(x => x.Size) + Children.Sum(x => x.CachedSize));
            Interlocked.Exchange(ref _cachedSizes, CalculatedChildrenBySizeDescending);
        }

        public void Refresh()
        {
            if (RefreshTask.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            RefreshTask = Task.Run(() => Calculate());
        }

        public bool IsCalculating => RefreshTask.Status != TaskStatus.RanToCompletion;

        private Task RefreshTask { get; set; } = Task.CompletedTask;

        public async Task Calculate()
        {
            FileSystemInfo[] children;
            try
            {
                children = Directory.EnumerateFileSystemInfos()
                    .Where(x => x.IsValidForAnalysis())
                    .ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            Files.Clear();

            foreach (var file in children.OfType<FileInfo>())
            {
                Files.Add(new LightFileInfo
                {
                    Name = file.Name,
                    Size = file.Length
                });
            }

            Children.Clear();

            foreach (var directory in children.OfType<DirectoryInfo>())
            {
                var child = new TreeItem(directory, this);
                Children.Add(child);
            }

            foreach (var child in Children)
            {
                await child.RefreshTask;
                child.FilesChanged();
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

    public static class FileSystemInfoExtensions
    {
        public static bool IsValidForAnalysis(this FileSystemInfo fileSystemInfo)
        {
            return !fileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}
