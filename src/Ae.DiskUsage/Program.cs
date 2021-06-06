﻿using Ae.ImGuiBootstrapper;
using Humanizer;
using ImGuiNET;
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

            while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
            {
                if (analyser == null)
                {
                    ImGui.OpenPopup("Select Drive");
                    ImGui.BeginPopupModal("Select Drive");

                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (ImGui.Button(drive.Name))
                        {
                            analyser = new TreeItem(drive.RootDirectory, null);
                        }
                    }                   

                    ImGui.EndPopup();
                }
                else
                {
                    ImGui.SetNextWindowPos(new Vector2(32, 32));
                    ImGui.SetNextWindowSize(new Vector2(1200, 650));
                    ImGui.Begin($"Results for {analyser.Directory}");

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
            }
        }

        public static void RenderTreeItem(TreeItem treeItem)
        {
            foreach (var node in treeItem.Children.OrderByDescending(x => x.TotalSize))
            {
                var size = node.TotalSize.Bytes().Humanize("#.#");

                var tag = node.IsCalculating ? $"calculating - {size}" : size;

                var flags = ImGuiTreeNodeFlags.None;

                if (node.Children.Count == 0)
                {
                    flags |= ImGuiTreeNodeFlags.Leaf;
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