/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Mppm
    {
        public const string MppmActivatePlayerToolId = "mppm-activate-player";
        [AiTool
        (
            MppmActivatePlayerToolId,
            Title = "MPPM / Activate Player"
        )]
        [AiSkillDescription("Activate (launch) a Multiplayer Play Mode virtual player so it boots as a " +
            "separate editor instance and connects to this MCP server. Use '" + MppmListPlayersToolId +
            "' to see available players first.")]
        [AiSkillBody("Launches the MPPM clone at `playerIndex` (2..4; player 1 is the main editor and cannot " +
            "be activated). The clone boots as its own OS process, compiles, and connects to THIS editor's " +
            "MCP server as a separate read-only instance — after it registers you can select it with the " +
            "server's instance-selection tool and inspect its live runtime independently.\n\n" +
            "Activation is asynchronous: the clone takes time to boot + compile before it appears as a " +
            "connected instance. This tool is disabled inside clones (a clone cannot spawn clones) and " +
            "requires the `com.unity.multiplayer.playmode` package.")]
        [Description("Activate (launch) a Multiplayer Play Mode virtual player (clone). playerIndex is 2..4; " +
            "player 1 is the main editor and cannot be activated. The clone boots as a separate editor " +
            "instance and connects to this MCP server. Requires the MPPM package.")]
        public MppmControl.PlayerInfo ActivatePlayer
        (
            [Description("Index of the virtual player to activate. Valid range: 2..4 (clones).")]
            int playerIndex
        )
        {
            if (MppmUtils.IsMppmClone)
                throw new InvalidOperationException(Error.RunningInsideClone("activate a player"));

            return MainThread.Instance.Run(() => MppmControl.ActivatePlayer(playerIndex));
        }
    }
}
