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
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Control surface for Unity's Multiplayer Play Mode (MPPM) virtual players — list / activate /
    /// deactivate the clones an agent uses for multiplayer dev testing.
    ///
    /// REFLECTION JUSTIFICATION (project style forbids reflection for private access): the shipped
    /// <c>com.unity.multiplayer.playmode</c> package is documentation-only — the implementation lives in
    /// Unity's built-in editor assemblies as <b>internal</b> types
    /// (<c>Unity.Multiplayer.PlayMode.Editor.MultiplayerPlaymode</c> /
    /// <c>...VirtualProjectWorkflow</c>) with no public API surface. There is no compile-time-referenceable
    /// entry point to activate a virtual player, so reflection is the only option. All of it is confined to
    /// THIS file so the rest of the plugin stays reflection-free; every member access degrades gracefully
    /// (a missing type/member surfaces a clear "MPPM not available" error rather than throwing raw
    /// reflection exceptions). Callers MUST invoke these on the Unity main thread.
    /// </summary>
    public static class MppmControl
    {
        const string MultiplayerPlaymodeTypeName = "Unity.Multiplayer.PlayMode.Editor.MultiplayerPlaymode";
        const string VirtualProjectWorkflowTypeName = "Unity.Multiplayer.PlayMode.Editor.VirtualProjectWorkflow";

        const BindingFlags StaticMembers = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        const BindingFlags InstanceMembers = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>The four MPPM player slots, indexed 1..4 (slot 1 is the main editor).</summary>
        public const int MinPlayerIndex = 1;
        public const int MaxPlayerIndex = 4;

        /// <summary>Non-secret snapshot of a single MPPM virtual player, returned by the list tool.</summary>
        public class PlayerInfo
        {
            public int Index { get; set; }
            public string Name { get; set; } = string.Empty;
            /// <summary>e.g. "Launched" / "NotLaunched" — the raw MPPM PlayerState enum name.</summary>
            public string State { get; set; } = string.Empty;
            /// <summary>e.g. "Main" / "Clone" — the raw MPPM player-type enum name.</summary>
            public string Type { get; set; } = string.Empty;
        }

        /// <summary>True when MPPM's editor types are present in the loaded assemblies (package installed).</summary>
        public static bool IsAvailable => FindType(MultiplayerPlaymodeTypeName) != null;

        /// <summary>
        /// Ensure MPPM's runtime state is armed so the player slots are populated. Idempotent — safe to call
        /// before every operation. No-op (returns false) when MPPM is not installed.
        /// </summary>
        public static bool EnsureInitialized()
        {
            var workflow = FindType(VirtualProjectWorkflowTypeName);
            if (workflow == null)
                return false;

            var isInitialized = workflow.GetProperty("IsInitialized", StaticMembers)?.GetValue(null) as bool?;
            if (isInitialized == true)
                return true;

            var update = workflow.GetMethod("UpdateMPPMRuntimeState", StaticMembers);
            update?.Invoke(null, new object[] { true });

            return workflow.GetProperty("IsInitialized", StaticMembers)?.GetValue(null) as bool? ?? false;
        }

        /// <summary>List all four player slots. Throws <see cref="MppmUnavailableException"/> when MPPM is absent.</summary>
        public static List<PlayerInfo> ListPlayers()
        {
            RequireAvailable();
            EnsureInitialized();

            var players = new List<PlayerInfo>(MaxPlayerIndex);
            for (int index = MinPlayerIndex; index <= MaxPlayerIndex; index++)
            {
                var player = GetPlayer(index);
                players.Add(new PlayerInfo
                {
                    Index = index,
                    Name = ReadInstanceString(player, "Name"),
                    State = ReadInstanceString(player, "PlayerState"),
                    Type = ReadInstanceString(player, "Type"),
                });
            }
            return players;
        }

        /// <summary>
        /// Activate (launch) the virtual player at <paramref name="index"/>. Returns the resulting player
        /// snapshot. Throws for an out-of-range index, missing MPPM, the main-editor slot, or an activation
        /// failure (with the reason MPPM reported).
        /// </summary>
        public static PlayerInfo ActivatePlayer(int index)
            => InvokePlayerToggle(index, "Activate", expectedArgCount: 2, verb: "activate");

        /// <summary>Deactivate (shut down) the virtual player at <paramref name="index"/>.</summary>
        public static PlayerInfo DeactivatePlayer(int index)
            => InvokePlayerToggle(index, "Deactivate", expectedArgCount: 1, verb: "deactivate");

        static PlayerInfo InvokePlayerToggle(int index, string methodName, int expectedArgCount, string verb)
        {
            RequireAvailable();
            ValidateIndex(index);
            EnsureInitialized();

            var player = GetPlayer(index)
                ?? throw new MppmUnavailableException($"MPPM player {index} could not be resolved.");

            var playerType = ReadInstanceString(player, "Type");
            if (string.Equals(playerType, "Main", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Player {index} is the Main editor and cannot be {verb}d. Use indices 2..{MaxPlayerIndex} for clones.");

            var method = player.GetType().GetMethod(methodName, InstanceMembers)
                ?? throw new MppmUnavailableException($"MPPM '{methodName}' method not found on player {index}.");

            // MPPM's Activate(out error, out _) / Deactivate(out error) report the failure reason via the
            // first by-ref argument and success via the bool return value.
            var args = new object?[expectedArgCount];
            var ok = method.Invoke(player, args) as bool? ?? false;
            if (!ok)
            {
                var reason = args.Length > 0 ? args[0]?.ToString() : null;
                throw new InvalidOperationException(
                    $"Failed to {verb} player {index}{(string.IsNullOrEmpty(reason) ? "." : $": {reason}")}");
            }

            return new PlayerInfo
            {
                Index = index,
                Name = ReadInstanceString(player, "Name"),
                State = ReadInstanceString(player, "PlayerState"),
                Type = playerType,
            };
        }

        static object? GetPlayer(int index)
        {
            var mp = FindType(MultiplayerPlaymodeTypeName);
            var propertyName = index switch
            {
                2 => "PlayerTwo",
                3 => "PlayerThree",
                4 => "PlayerFour",
                _ => "PlayerOne"
            };
            return mp?.GetProperty(propertyName, StaticMembers)?.GetValue(null);
        }

        static string ReadInstanceString(object? player, string propertyName)
        {
            if (player == null)
                return string.Empty;
            return player.GetType().GetProperty(propertyName, InstanceMembers)?.GetValue(player)?.ToString() ?? string.Empty;
        }

        static void ValidateIndex(int index)
        {
            if (index < MinPlayerIndex || index > MaxPlayerIndex)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Player index must be between {MinPlayerIndex} and {MaxPlayerIndex}.");
        }

        static void RequireAvailable()
        {
            if (!IsAvailable)
                throw new MppmUnavailableException(
                    "Multiplayer Play Mode (MPPM) is not available in this project. " +
                    "Install the 'com.unity.multiplayer.playmode' package to use MPPM tools.");
        }

        /// <summary>Scan every loaded assembly for a type — MPPM's internal types are not in a fixed assembly.</summary>
        static Type? FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = null;
                try { type = assembly.GetType(fullName); }
                catch { /* dynamic/reflection-only assemblies can throw; skip them */ }
                if (type != null)
                    return type;
            }
            return null;
        }
    }

    /// <summary>Thrown when an MPPM operation is requested but Multiplayer Play Mode is not installed/available.</summary>
    public class MppmUnavailableException : Exception
    {
        public MppmUnavailableException(string message) : base(message) { }
    }
}
