using Ae.ImGuiBootstrapper;
using Humanizer;
using ImGuiNET;
using System;
using System.Collections.Concurrent;
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

            var io = ImGui.GetIO();
            io.Fonts.AddFontFromFileTTF(@"Fonts\NotoSans-Regular.ttf", 20);

            ImGui.GetStyle().WindowRounding = 0.0f;
            ImGui.GetStyle().ChildRounding = 0.0f;
            ImGui.GetStyle().FrameRounding = 0.0f;
            ImGui.GetStyle().GrabRounding = 0.0f;
            ImGui.GetStyle().PopupRounding = 0.0f;
            ImGui.GetStyle().ScrollbarRounding = 0.0f;

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            Exception exception = null;

            var accessErrors = new BlockingCollection<string>();

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
                        if (analyser == null)
                        {
                            RenderSelectDrive(ref analyser, accessErrors);
                        }
                        else
                        {
                            RenderTree(ref analyser, accessErrors);
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }
        }

        private static void RenderSelectDrive(ref TreeItem analyser, BlockingCollection<string> logMessages)
        {
            var viewport = ImGui.GetMainViewport();

            var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;

            ImGui.SetNextWindowPos(viewport.Pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(viewport.Size, ImGuiCond.Always);
            ImGui.Begin("Select Drive", flags);
            ImGui.Text("Select drive to analyse.");

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (ImGui.Button($"{drive.Name}"))
                {
                    analyser = new TreeItem(drive.RootDirectory, null, logMessages);
                }

                ImGui.SameLine();
                try
                {
                    ImGui.Text($"{drive.TotalFreeSpace.Bytes().Humanize("#.#")} free");
                }
                catch (Exception)
                {
                    ImGui.Text("(unable to calculate free space)");
                }
            }

            ImGui.End();
        }

        private static void RenderTree(ref TreeItem analyser, BlockingCollection<string> logMessages)
        {
            var viewport = ImGui.GetMainViewport();

            var flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;

            var firstPosition = viewport.Pos;
            var firstSize = viewport.Size * new Vector2(1, 0.75f);

            ImGui.SetNextWindowPos(firstPosition, ImGuiCond.Always);
            ImGui.SetNextWindowSize(firstSize, ImGuiCond.Always);
            ImGui.Begin($"Results for {analyser.Directory} ({GetTag(analyser)})###ResultsWindow", flags);

            if (!analyser.IsCalculating)
            {
                if (ImGui.Button("Refresh all"))
                {
                    analyser.Refresh();
                }
            }

            RenderTreeItem(analyser);
            ImGui.End();

            var secondPosition = viewport.Size * new Vector2(0, 0.75f);
            var secondSize = viewport.Size * new Vector2(1, 0.25f);

            ImGui.SetNextWindowPos(secondPosition, ImGuiCond.Always);
            ImGui.SetNextWindowSize(secondSize, ImGuiCond.Always);
            ImGui.Begin("Console", flags);
            foreach (var logMessage in logMessages)
            {
                ImGui.Text(logMessage);
            }
            ImGui.SetScrollHereY();
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