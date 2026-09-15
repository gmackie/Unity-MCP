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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests for the MPPM (Multiplayer Play Mode) control tools. These are availability-aware: the
    /// plugin's own project does not install the MPPM package, so the "unavailable" branches are the
    /// ones normally exercised. When MPPM IS present (e.g. a consuming project), the live branches assert
    /// the real behaviour instead — so the suite is green either way.
    /// </summary>
    [TestFixture]
    public class MppmToolTests : BaseTest
    {
        static readonly string[] ExpectedToolIds =
        {
            Tool_Mppm.MppmListPlayersToolId,
            Tool_Mppm.MppmActivatePlayerToolId,
            Tool_Mppm.MppmDeactivatePlayerToolId,
        };

        [Test]
        public void ToolIds_HaveExpectedStableValues()
        {
            Assert.AreEqual("mppm-list-players", Tool_Mppm.MppmListPlayersToolId);
            Assert.AreEqual("mppm-activate-player", Tool_Mppm.MppmActivatePlayerToolId);
            Assert.AreEqual("mppm-deactivate-player", Tool_Mppm.MppmDeactivatePlayerToolId);
        }

        [Test]
        public void AllMppmTools_AreRegistered()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            var registered = toolManager!.GetAllTools().Select(t => t.Name).ToHashSet();
            foreach (var id in ExpectedToolIds)
                Assert.IsTrue(registered.Contains(id), $"Tool '{id}' should be registered");
        }

        [Test]
        public void Activate_InvalidIndex_ThrowsArgumentOutOfRange([Values(0, 5, -1, 99)] int index)
        {
            // Input validation must run before any environment probing, so this holds whether or not
            // MPPM is installed.
            Assert.Throws<ArgumentOutOfRangeException>(() => MppmControl.ActivatePlayer(index));
        }

        [Test]
        public void Deactivate_InvalidIndex_ThrowsArgumentOutOfRange([Values(0, 5, -1, 99)] int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MppmControl.DeactivatePlayer(index));
        }

        [Test]
        public void ListPlayers_DegradesCleanly()
        {
            // Contract: either MPPM is usable and we get the four slots, or it degrades to a typed
            // MppmUnavailableException. It must NEVER surface a raw reflection/NullReferenceException —
            // which is exactly what happens in Unity 6 when the MPPM types are built in but the project
            // has no MPPM data store (package not installed).
            try
            {
                var players = MppmControl.ListPlayers();
                Assert.AreEqual(MppmControl.MaxPlayerIndex, players.Count, "MPPM exposes four player slots");
                Assert.AreEqual(1, players[0].Index, "The first slot is player 1 (the main editor)");
            }
            catch (MppmUnavailableException)
            {
                Assert.Pass("MPPM is present but not usable in this project — degraded cleanly.");
            }
        }

        [Test]
        public void Operations_OnUnusableMppm_ThrowTypedError()
        {
            // Determine usability WITHOUT side effects by probing the read-only list first.
            bool usable;
            try { MppmControl.ListPlayers(); usable = true; }
            catch (MppmUnavailableException) { usable = false; }

            if (usable)
                Assert.Ignore("MPPM is usable here; skipping unusable-path checks (activating would launch a clone).");

            // A valid clone index passes validation, so the usability guard is what surfaces — a typed
            // error, and (critically) no clone is launched.
            Assert.Throws<MppmUnavailableException>(() => MppmControl.ActivatePlayer(2));
            Assert.Throws<MppmUnavailableException>(() => MppmControl.DeactivatePlayer(2));
        }
    }
}
