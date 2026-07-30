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
        public const string MppmDeactivatePlayerToolId = "mppm-deactivate-player";
        [AiTool
        (
            MppmDeactivatePlayerToolId,
            Title = "MPPM / Deactivate Player",
            IdempotentHint = true
        )]
        [AiSkillDescription("Deactivate (shut down) a Multiplayer Play Mode virtual player. Use '" +
            MppmListPlayersToolId + "' to see which players are currently launched.")]
        [AiSkillBody("Shuts down the MPPM clone at `playerIndex` (2..4; player 1 is the main editor and " +
            "cannot be deactivated). Its editor process exits and its MCP-server instance disconnects. " +
            "This tool is disabled inside clones and requires the `com.unity.multiplayer.playmode` package.")]
        [Description("Deactivate (shut down) a Multiplayer Play Mode virtual player (clone). playerIndex is " +
            "2..4; player 1 is the main editor and cannot be deactivated. Requires the MPPM package.")]
        public MppmControl.PlayerInfo DeactivatePlayer
        (
            [Description("Index of the virtual player to deactivate. Valid range: 2..4 (clones).")]
            int playerIndex
        )
        {
            if (MppmUtils.IsMppmClone)
                throw new InvalidOperationException(Error.RunningInsideClone("deactivate a player"));

            return MainThread.Instance.Run(() => MppmControl.DeactivatePlayer(playerIndex));
        }
    }
}
