using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ae.DiskUsage.Analyser
{
    public struct LightFileInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public override string ToString() => Name;
    }

    public class TreeItem
    {
        public TreeItem(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public IList<LightFileInfo> Files { get; } = new List<LightFileInfo>();
        public IList<TreeItem> Children { get; } = new List<TreeItem>();
        public DirectoryInfo Directory { get; }
        public long Size => Files.Sum(x => x.Size) + Children.Sum(x => x.Size);
        public IEnumerable<TreeItem> ChildrenBySizeDescending => Children.OrderByDescending(x => x.Size);
        public override string ToString() => Directory.Name;
    }

    public sealed class DiskUsageAnalyser
    {
        private async Task Analyse(TreeItem parent, DirectoryInfo root)
        {
            FileSystemInfo[] children;
            try
            {
                children = root.EnumerateFileSystemInfos()
                    .Where(x => x.IsValidForAnalysis())
                    .ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var file in children.OfType<FileInfo>())
            {
                parent.Files.Add(new LightFileInfo
                {
                    Name = file.Name,
                    Size = file.Length
                });
            }

            var tasks = new List<Task>();

            foreach (var directory in children.OfType<DirectoryInfo>())
            {
                var child = new TreeItem(directory);
                tasks.Add(Task.Run(() => Analyse(child, directory)));
                parent.Children.Add(child);
            }

            await Task.WhenAll(tasks);
        }

        public async Task<TreeItem> Analyse(DirectoryInfo root)
        {
            var top = new TreeItem(root);
            await Analyse(top, root);
            return top;
        }
    }

    public static class FileSystemInfoExtensions
    {
        public static bool IsValidForAnalysis(this FileSystemInfo fileSystemInfo)
        {
            return !fileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}
