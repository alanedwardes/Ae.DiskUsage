using Ae.ImGuiBootstrapper;
using Humanizer;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.DiskUsage
{
    class Program
    {
        static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Ae.DiskUsage");

            TreeItem analyser = null;

            using var window = new ImGuiWindow(windowInfo);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            Exception exception = null;

            while (window.Loop(ref backgroundColor))
            {
                if (exception != null)
                {
                    ImGui.Begin("Unhandled Exception", ImGuiWindowFlags.AlwaysAutoResize);
                    ImGui.Text(exception.ToString());
                    ImGui.End();
                }
                else
                {
                    try
                    {
                        RenderLoop(ref analyser);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }
        }

        private static void RenderLoop(ref TreeItem analyser)
        {
            if (analyser == null)
            {
                RenderSelectDrive(ref analyser);
            }
            else
            {
                RenderTree(ref analyser);
            }
        }

        private static void RenderSelectDrive(ref TreeItem analyser)
        {
            ImGui.SetNextWindowSize(new Vector2(256, 128), ImGuiCond.Once);
            ImGui.Begin("Select Drive");
            ImGui.Text("Select drive to analyse.");

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (ImGui.Button($"{drive.Name}"))
                {
                    analyser = new TreeItem(drive.RootDirectory, null);
                }

                ImGui.SameLine();
                ImGui.Text($"{drive.TotalFreeSpace.Bytes().Humanize("#.#")} free");
            }

            ImGui.End();
        }

        private static void RenderTree(ref TreeItem analyser)
        {
            ImGui.SetNextWindowPos(new Vector2(32, 32), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(1200, 650), ImGuiCond.Once);
            ImGui.Begin($"Results for {analyser.Directory} ({GetTag(analyser)})");

            if (!analyser.IsCalculating)
            {
                if (ImGui.Button("Refresh all"))
                {
                    analyser.Refresh();
                }
            }

            RenderTreeItem(analyser);
            ImGui.End();
        }

        private static string GetTag(TreeItem node)
        {
            var size = node.TotalSize.Bytes().Humanize("#.#");
            return node.IsCalculating ? $"calculating - {size}" : size;
        }

        public static void RenderTreeItem(TreeItem treeItem)
        {
            foreach (var node in treeItem.Children.OrderByDescending(x => x.TotalSize))
            {
                var flags = ImGuiTreeNodeFlags.None;

                if (node.Children.Count == 0)
                {
                    flags |= ImGuiTreeNodeFlags.Leaf;
                }

                bool open = ImGui.TreeNodeEx(node.Directory.FullName, flags, $"{node.Directory.Name} ({GetTag(node)})");

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