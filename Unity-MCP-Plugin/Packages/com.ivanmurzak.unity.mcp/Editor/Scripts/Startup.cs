/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    [InitializeOnLoad]
    public static partial class Startup
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(Startup));

        static Startup()
        {
            if (MppmUtils.IsMppmClone)
            {
                UnityMcpPluginEditor.KeepConnected = true;
                UnityMcpPluginEditor.KeepServerRunning = true;

                // MPPM clones must connect to the main editor's MCP server.
                // Clone paths are <main-project>/Library/VP/<cloneId>, so we
                // derive the main project directory and compute its port.
                var cloneDataPath = Application.dataPath; // .../Library/VP/<id>/Assets
                var cloneProjectDir = System.IO.Path.GetDirectoryName(cloneDataPath)!;
                var mainProjectDir = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(cloneProjectDir, "..", "..", ".."));
                var mainPort = UnityMcpPlugin.GeneratePortFromDirectory(mainProjectDir);
                UnityMcpPluginEditor.LocalHost = $"http://localhost:{mainPort}";
                UnityMcpPluginEditor.AuthOption = AuthOption.none;
            }

            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPluginEditor.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());

            if (MppmUtils.IsMppmClone)
                DisableWriteToolsForClone();

            if (Application.dataPath.Contains(" "))
                Debug.LogError("The project path contains spaces, which may cause issues during usage of AI Game Developer. Please consider the move the project to a folder without spaces.");

            SubscribeOnEditorEvents();

            // Initialize sub-systems
            API.Tool_Tests.Init();
            UpdateChecker.Init();
            PackageUtils.Init();

            if (!MppmUtils.IsMppmClone)
            {
                // Auto-generate skill files for the selected agent if enabled
                var savedAgentId = MainWindowEditor.selectedAiAgentId.Value;
                var agent = AiAgentConfiguratorRegistry.GetByAgentId(savedAgentId);
                if (agent?.SupportsSkills == true && UnityMcpPluginEditor.IsAutoGenerateSkills(agent.AgentId))
                {
                    UnityMcpPluginEditor.SkillsPath = agent.SkillsPath!;
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
                }
            }
        }

        static void DisableWriteToolsForClone()
        {
            var tools = UnityMcpPluginEditor.Instance.Tools;
            if (tools == null) return;

            var writeTools = new HashSet<string>
            {
                "script-update-or-create", "script-delete",
                "assets-create-folder", "assets-delete", "assets-move", "assets-copy", "assets-modify",
                "assets-prefab-create", "assets-prefab-save", "assets-prefab-close", "assets-prefab-open",
                "assets-material-create",
                "scene-create", "scene-save",
                "package-add", "package-remove",
                "gameobject-create", "gameobject-destroy", "gameobject-duplicate",
                "gameobject-modify", "gameobject-set-parent",
                "gameobject-component-add", "gameobject-component-destroy", "gameobject-component-modify",
                "object-modify",
            };

            foreach (var toolName in writeTools)
                tools.SetToolEnabled(toolName, false);
        }
    }
}
