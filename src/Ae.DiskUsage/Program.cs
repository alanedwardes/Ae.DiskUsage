using Ae.ImGuiBootstrapper;
using Humanizer;
using ImGuiNET;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.DiskUsage
{
    class Program
    {
        static void Main()
        {
            var directory = new DirectoryInfo(@"C:\");

            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Ae.DiskUsage");

            TreeItem analyser = null;

            using var window = new ImGuiWindow(windowInfo);

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                ImGui.SetNextWindowPos(new Vector2(32, 32));
                ImGui.SetNextWindowSize(new Vector2(1200, 650));
                ImGui.Begin($"Results for {directory}");

                if (!analyser?.IsCalculating ?? false)
                {
                    if (ImGui.Button("Refresh all"))
                    {
                        analyser.Refresh();
                    }
                }

                if (analyser == null)
                {
                    analyser = new TreeItem(directory, null);
                }
                else
                {
                    RenderTreeItem(analyser);
                }

                ImGui.End();
            }
        }

        public static void RenderTreeItem(TreeItem treeItem)
        {
            foreach (var node in treeItem.CachedChildrenBySizeDescending)
            {
                var size = node.CachedSize.Bytes().Humanize("#.#");

                var tag = node.IsCalculating ? $"calculating - {size}" : size;

                var flags = ImGuiTreeNodeFlags.None;

                if (node.Children.Count == 0)
                {
                    flags &= ImGuiTreeNodeFlags.Leaf;
                }

                bool open = ImGui.TreeNodeEx(node.Directory.FullName, flags, $"{node.Directory.Name} ({tag})");

                if (ImGui.BeginPopupContextItem())
                {
                    ImGui.Text("Operations");
                    ImGui.Separator();

                    if (!node.IsCalculating)
                    {
                        if (ImGui.Button("Refresh"))
                        {
                            node.Refresh();
                        }
                    }

                    if (ImGui.Button("Explore"))
                    {
                        Process.Start(new ProcessStartInfo { FileName = node.Directory.FullName, UseShellExecute = true, Verb = "open" });
                    }

                    ImGui.EndPopup();
                }

                if (open)
                {
                    RenderTreeItem(node);
                    ImGui.TreePop();
                }
            }
        }
    }
}