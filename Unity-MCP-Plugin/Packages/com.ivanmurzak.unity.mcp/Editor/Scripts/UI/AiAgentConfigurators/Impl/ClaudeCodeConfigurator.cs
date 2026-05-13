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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Claude Code AI agent.
    /// </summary>
    public class ClaudeCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";
        public override string TutorialUrl => "https://youtu.be/Sknh2p12W8c";
        public override string? SkillsPath => ".claude/skills";

        protected override string? IconFileName => "claude-64.png";

        private static string LocalConfigPath => Path.Combine(ProjectRootPath, ".mcp.json");

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: "mcpServers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void ReconfigureDetectedConfigs()
        {
            base.ReconfigureDetectedConfigs();
            SyncMppmCloneConfigs();
        }

        protected override AiAgentConfigurator SetConfigureStatusIndicator()
        {
            var result = base.SetConfigureStatusIndicator();

            // Hook into both transport config buttons to register/unregister clones
            if (_configElementStdio != null)
                _subscriptionMppmStdio = _configElementStdio.OnConfigured.Subscribe(configured =>
                {
                    if (configured) SyncMppmCloneConfigs();
                    else UnregisterMppmClones();
                });
            if (_configElementHttp != null)
                _subscriptionMppmHttp = _configElementHttp.OnConfigured.Subscribe(configured =>
                {
                    if (configured) SyncMppmCloneConfigs();
                    else UnregisterMppmClones();
                });

            return result;
        }

        private IDisposable? _subscriptionMppmStdio;
        private IDisposable? _subscriptionMppmHttp;

        void SyncMppmCloneConfigs()
        {
            if (MppmUtils.IsMppmClone)
                return;
            RegisterMppmClones();
        }

        static List<(string name, string id, string path, int port)> DiscoverMppmClones()
        {
            var clones = new List<(string name, string id, string path, int port)>();
            var vpDir = Path.Combine(Application.dataPath, "..", "Library", "VP");
            if (!Directory.Exists(vpDir))
                return clones;

            var dirs = Directory.GetDirectories(vpDir, "mppm*")
                                .OrderBy(d => d)
                                .ToList();

            for (int i = 0; i < dirs.Count; i++)
            {
                var cloneDir = dirs[i];
                var cloneId = Path.GetFileName(cloneDir);
                var playerIndex = i + 2;
                var name = $"Player {playerIndex}";
                var port = UnityMcpPlugin.GeneratePortFromDirectory(cloneDir);
                clones.Add((name, cloneId, cloneDir, port));
            }
            return clones;
        }

        void RegisterMppmClones()
        {
            var clones = DiscoverMppmClones();
            if (!File.Exists(LocalConfigPath))
                return;

            try
            {
                var json = File.ReadAllText(LocalConfigPath);
                var rootObj = System.Text.Json.Nodes.JsonNode.Parse(json)?.AsObject();
                if (rootObj == null) return;

                var mcpServers = rootObj["mcpServers"]?.AsObject();
                if (mcpServers == null) return;

                // Remove stale clone entries
                var keysToRemove = new List<string>();
                foreach (var kv in mcpServers)
                {
                    if (kv.Key.StartsWith($"{AiAgentConfig.DefaultMcpServerName}-player-"))
                        keysToRemove.Add(kv.Key);
                }
                foreach (var key in keysToRemove)
                    mcpServers.Remove(key);

                // Add fresh entries for each discovered clone
                var command = McpServerManager.ExecutableFullPath.Replace('\\', '/');
                foreach (var clone in clones)
                {
                    var serverName = $"{AiAgentConfig.DefaultMcpServerName}{MppmCloneSuffix(clone.name)}";
                    mcpServers[serverName] = new JsonObject
                    {
                        ["command"] = command,
                        ["args"] = new JsonArray
                        {
                            $"{Args.Port}={clone.port}",
                            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
                            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
                            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
                            $"{Args.Token}={UnityMcpPluginEditor.Token}"
                        }
                    };
                }

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(LocalConfigPath, rootObj.ToJsonString(options));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error registering MPPM clones: {e.Message}");
            }
        }

        void UnregisterMppmClones()
        {
            if (!File.Exists(LocalConfigPath))
                return;

            try
            {
                var json = File.ReadAllText(LocalConfigPath);
                var rootObj = System.Text.Json.Nodes.JsonNode.Parse(json)?.AsObject();
                if (rootObj == null) return;

                var mcpServers = rootObj["mcpServers"]?.AsObject();
                if (mcpServers == null) return;

                var keysToRemove = new List<string>();
                foreach (var kv in mcpServers)
                {
                    if (kv.Key.StartsWith($"{AiAgentConfig.DefaultMcpServerName}-player-"))
                        keysToRemove.Add(kv.Key);
                }

                if (keysToRemove.Count == 0) return;

                foreach (var key in keysToRemove)
                    mcpServers.Remove(key);

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(LocalConfigPath, rootObj.ToJsonString(options));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error unregistering MPPM clones: {e.Message}");
            }
        }

        static string MppmCloneSuffix(string cloneName)
        {
            var lower = cloneName.Trim().ToLowerInvariant();
            var kebab = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(kebab) ? string.Empty : $"-{kebab}";
        }

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var isCloud = UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud;
            var isAuthRequired = isCloud || UnityMcpPluginEditor.AuthOption == AuthOption.required;

            // STDIO Configuration

            var startContainerStdio = TemplateFoldoutFirst("Start");
            startContainerStdio!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerStdio!.Add(TemplateLabelDescription("Launch Claude Code"));
            startContainerStdio!.Add(TemplateTextFieldReadOnly("claude"));
            ContainerStdio!.Add(startContainerStdio);

            var manualStepsContainer = TemplateFoldout("Manual Configuration Steps");

            var tokenStdio = !string.IsNullOrEmpty(UnityMcpPluginEditor.Token) ? UnityMcpPluginEditor.Token : "<token>";
            var authArgsStdio = isAuthRequired
                ? $" {Args.Authorization}={AuthOption.required} {Args.Token}={tokenStdio}"
                : string.Empty;

            var addMcpServerCommandStdio = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{McpServerManager.ExecutableFullPath}\" port={UnityMcpPluginEditor.Port} plugin-timeout={UnityMcpPluginEditor.TimeoutMs} client-transport=stdio{authArgsStdio}";

            manualStepsContainer!.Add(TemplateLabelDescription("Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(addMcpServerCommandStdio));
            manualStepsContainer!.Add(TemplateLabelDescription("Restart or start Claude Code to apply the configuration"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Check that the configuration file .mcp.json exists"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var startContainerHttp = TemplateFoldoutFirst("Start");
            startContainerHttp!.Add(TemplateLabelDescription("Navigate to project root"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startContainerHttp!.Add(TemplateLabelDescription("Launch Claude Code"));
            startContainerHttp!.Add(TemplateTextFieldReadOnly("claude"));
            ContainerHttp!.Add(startContainerHttp);

            var manualStepsContainerHttp = TemplateFoldout("Manual Configuration Steps");

            var tokenHttp = !string.IsNullOrEmpty(UnityMcpPluginEditor.Token) ? UnityMcpPluginEditor.Token : "<token>";
            var authHeaderHttp = isAuthRequired
                ? $" --header \"Authorization: Bearer {tokenHttp}\""
                : string.Empty;

            var addMcpServerCommandHttp = $"claude mcp add --transport http {AiAgentConfig.DefaultMcpServerName} {UnityMcpPluginEditor.Host}{authHeaderHttp}";

            manualStepsContainerHttp!.Add(TemplateLabelDescription("Run the following command in the folder of the Unity project to configure Claude Code"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(addMcpServerCommandHttp));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("Restart or start Claude Code to apply the configuration"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly("claude"));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is installed and accessible from terminal"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code CLI is started in the same folder where Unity project is located. This folder must contains Assets folder inside"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Claude Code is configured with the same port as it is in Unity right now"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Check that the configuration file .mcp.json exists"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Claude Code after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
