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
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    // Exercises MppmUtils.Parse directly — the sole mechanism that distinguishes an MPPM clone
    // from the main editor. The public MppmUtils.IsMppmClone reads the real process args (fixed
    // for the editor's lifetime, never a clone under the test runner), so the parsing is only
    // reachable through the internal Parse seam (visible via InternalsVisibleTo). A mis-parse here
    // would let a clone be treated as the main editor and keep the write tools it must not have.
    [TestFixture]
    public class MppmUtilsTests
    {
        [Test]
        public void Parse_NoArgs_IsNotClone()
        {
            var result = MppmUtils.Parse(new string[0]);

            Assert.IsFalse(result.IsMppmClone);
            Assert.IsNull(result.CloneName);
            Assert.IsNull(result.CloneId);
            Assert.AreEqual(string.Empty, result.CloneSuffix);
        }

        [Test]
        public void Parse_TypicalMainEditorArgs_IsNotClone()
        {
            // Args resembling a normal editor launch — no MPPM markers.
            var result = MppmUtils.Parse(new[]
            {
                "/Applications/Unity/Unity.app/Contents/MacOS/Unity",
                "-projectPath", "/Users/dev/MyGame",
                "-logFile", "-",
            });

            Assert.IsFalse(result.IsMppmClone);
            Assert.IsNull(result.CloneName);
            Assert.AreEqual(string.Empty, result.CloneSuffix);
        }

        [Test]
        public void Parse_EditorModeCloneFlag_IsClone()
        {
            var result = MppmUtils.Parse(new[] { "-editor-mode", "com.unity.mppm.clone" });

            Assert.IsTrue(result.IsMppmClone);
        }

        [Test]
        public void Parse_EditorModeOtherValue_IsNotClone()
        {
            // -editor-mode present but NOT the MPPM clone value: must not be treated as a clone.
            var result = MppmUtils.Parse(new[] { "-editor-mode", "com.unity.something.else" });

            Assert.IsFalse(result.IsMppmClone);
        }

        [Test]
        public void Parse_EditorModeFlagAtEnd_DoesNotThrow_IsNotClone()
        {
            // Dangling -editor-mode with no following token: must not read past the array.
            var result = MppmUtils.Parse(new[] { "-editor-mode" });

            Assert.IsFalse(result.IsMppmClone);
        }

        [Test]
        public void Parse_Name_SetsNameAndKebabSuffix()
        {
            var result = MppmUtils.Parse(new[] { "-name", "Player 2" });

            Assert.AreEqual("Player 2", result.CloneName);
            Assert.AreEqual("-player-2", result.CloneSuffix);
        }

        [Test]
        public void Parse_NameFlagAtEnd_DoesNotThrow_NameNull()
        {
            var result = MppmUtils.Parse(new[] { "-name" });

            Assert.IsNull(result.CloneName);
            Assert.AreEqual(string.Empty, result.CloneSuffix);
        }

        [Test]
        public void Parse_VpIdEqualsForm_SetsId()
        {
            var result = MppmUtils.Parse(new[] { "-vpId=abc123" });

            Assert.AreEqual("abc123", result.CloneId);
        }

        [Test]
        public void Parse_VpIdSpaceForm_SetsId()
        {
            var result = MppmUtils.Parse(new[] { "-vpId", "abc123" });

            Assert.AreEqual("abc123", result.CloneId);
        }

        [Test]
        public void Parse_FullCloneArgSet_PopulatesAllFields()
        {
            // How MPPM actually launches a virtual player.
            var result = MppmUtils.Parse(new[]
            {
                "/Applications/Unity/Unity.app/Contents/MacOS/Unity",
                "-projectPath", "/Users/dev/MyGame/Library/VP/mppm-2",
                "-editor-mode", "com.unity.mppm.clone",
                "-name", "Player 2",
                "-vpId", "player-two-guid",
            });

            Assert.IsTrue(result.IsMppmClone);
            Assert.AreEqual("Player 2", result.CloneName);
            Assert.AreEqual("player-two-guid", result.CloneId);
            Assert.AreEqual("-player-2", result.CloneSuffix);
        }

        [TestCase("Player 2", "-player-2")]
        [TestCase("Player 10", "-player-10")]
        [TestCase("  Trimmed  ", "-trimmed")]
        [TestCase("Weird!!Name??", "-weird-name")]
        [TestCase("MixedCASE", "-mixedcase")]
        [TestCase("already-kebab", "-already-kebab")]
        public void Parse_Name_KebabCaseSuffix(string name, string expectedSuffix)
        {
            var result = MppmUtils.Parse(new[] { "-name", name });

            Assert.AreEqual(expectedSuffix, result.CloneSuffix);
        }

        [Test]
        public void Parse_NameWithOnlySymbols_EmptySuffix()
        {
            // Nothing kebab-able remains -> empty suffix, never a bare "-".
            var result = MppmUtils.Parse(new[] { "-name", "!!!" });

            Assert.AreEqual(string.Empty, result.CloneSuffix);
        }
    }
}
