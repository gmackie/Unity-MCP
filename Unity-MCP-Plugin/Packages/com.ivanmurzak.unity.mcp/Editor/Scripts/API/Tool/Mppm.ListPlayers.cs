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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Mppm
    {
        public const string MppmListPlayersToolId = "mppm-list-players";
        [AiTool
        (
            MppmListPlayersToolId,
            Title = "MPPM / List Players",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [AiSkillDescription("List Unity's Multiplayer Play Mode (MPPM) virtual players and their current " +
            "state, so you can pick one to activate for multiplayer testing. Player 1 is the main editor; " +
            "players 2..4 are clones. Use '" + MppmActivatePlayerToolId + "' to launch a clone.")]
        [AiSkillBody("Returns the four MPPM player slots with their `Name`, `State` (e.g. Launched / " +
            "NotLaunched) and `Type` (Main / Clone).\n\n" +
            "Each activated clone connects to THIS editor's MCP server as its own instance and can be " +
            "inspected independently — select it with the server's instance-selection tool, then read its " +
            "scene / hierarchy / screenshots. Requires the `com.unity.multiplayer.playmode` package; returns " +
            "an error when MPPM is not installed.")]
        [Description("List Multiplayer Play Mode (MPPM) virtual players (slots 1..4) with their name, state " +
            "and type. Player 1 is the main editor; players 2..4 are clones. Requires the MPPM package.")]
        public List<MppmControl.PlayerInfo> ListPlayers()
        {
            return MainThread.Instance.Run(() => MppmControl.ListPlayers());
        }
    }
}
