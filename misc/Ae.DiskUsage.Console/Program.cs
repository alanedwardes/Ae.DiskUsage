using Ae.DiskUsage.Analyser;
using Ae.ImGuiBootstrapper;
using Humanizer;
using ImGuiNET;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.DiskUsage.Console
{
    class Program
    {
        static async Task Main()
        {
            var analyser = new DiskUsageAnalyser();

            var directory = new DirectoryInfo(@"C:\Users\alan");

            var analysis = analyser.Analyse(directory);

            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program");

            using var window = new ImGuiWindow(windowInfo);

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                if (analysis.Status != TaskStatus.RanToCompletion)
                {
                    ImGui.Begin($"Analysing {directory}");
                    ImGui.Text("Please wait...");
                    ImGui.End();
                }
                else
                {
                    ImGui.Begin($"Results for {directory}");
                    RenderTreeItem(analysis.Result);
                    ImGui.End();
                }
            }
        }

        public static void RenderTreeItem(TreeItem treeItem)
        {
            foreach (var node in treeItem.ChildrenBySizeDescending)
            {
                if (ImGui.TreeNode($"{node.Directory.Name} ({node.Size.Bytes().Humanize()})"))
                {
                    RenderTreeItem(node);
                    ImGui.TreePop();
                }
            }
        }
    }
}