/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak)              │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// MCP tools for driving Unity's Multiplayer Play Mode (MPPM) virtual players, so an agent can spin up
    /// and tear down clones for multiplayer dev testing. Each clone connects to the main editor's MCP server
    /// as its own read-only instance (see <see cref="MppmUtils"/> and the clone write-tool gating in
    /// <c>Startup.DisableWriteToolsForClone</c>). The activate/deactivate tools are disabled inside clones,
    /// so a clone can never spawn further clones.
    /// </summary>
    [AiToolType]
    public partial class Tool_Mppm
    {
        public static class Error
        {
            public static string RunningInsideClone(string action)
                => $"Cannot {action} from inside an MPPM virtual player ('{MppmUtils.CloneName}'). " +
                   "Run MPPM control tools from the main Unity editor instead.";
        }
    }
}
