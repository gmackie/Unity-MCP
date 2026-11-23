<<<<<<< HEAD
/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

=======
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)
#nullable enable
using System;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MppmUtils
    {
        static readonly bool _isMppmClone;
        static readonly string? _cloneName;
        static readonly string? _cloneId;
        static readonly string _cloneSuffix;

        public static bool IsMppmClone => _isMppmClone;
        public static string? CloneName => _cloneName;
        public static string? CloneId => _cloneId;
        public static string CloneSuffix => _cloneSuffix;

        static MppmUtils()
        {
<<<<<<< HEAD
            var parsed = Parse(Environment.GetCommandLineArgs());
            _isMppmClone = parsed.IsMppmClone;
            _cloneName = parsed.CloneName;
            _cloneId = parsed.CloneId;
            _cloneSuffix = parsed.CloneSuffix;
        }

        /// <summary>
        /// The clone identity derived from a process's command-line args. Kept as a plain value
        /// so <see cref="Parse"/> can be exercised in tests without touching the real process args
        /// (which are fixed for the lifetime of the editor and can't be a clone under the runner).
        /// </summary>
        internal readonly struct CloneArgs
        {
            public readonly bool IsMppmClone;
            public readonly string? CloneName;
            public readonly string? CloneId;
            public readonly string CloneSuffix;

            public CloneArgs(bool isMppmClone, string? cloneName, string? cloneId, string cloneSuffix)
            {
                IsMppmClone = isMppmClone;
                CloneName = cloneName;
                CloneId = cloneId;
                CloneSuffix = cloneSuffix;
            }
        }

        /// <summary>
        /// Parses MPPM virtual-player identity from Unity's command-line args. A clone is launched
        /// with <c>-editor-mode com.unity.mppm.clone</c> plus <c>-name "Player N"</c> and a
        /// <c>-vpId</c> (either <c>-vpId=ID</c> or <c>-vpId ID</c>). This is the sole mechanism by
        /// which a clone is distinguished from the main editor — get it wrong and a clone would be
        /// treated as the main editor (and keep the write/mutating tools it must not have).
        /// </summary>
        internal static CloneArgs Parse(string[] args)
        {
            var isMppmClone = false;
            string? cloneName = null;
            string? cloneId = null;
=======
            var args = Environment.GetCommandLineArgs();
            _isMppmClone = false;
            _cloneName = null;
            _cloneId = null;
            _cloneSuffix = string.Empty;
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-editor-mode" && i + 1 < args.Length && args[i + 1] == "com.unity.mppm.clone")
                {
<<<<<<< HEAD
                    isMppmClone = true;
=======
                    _isMppmClone = true;
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)
                    i++;
                }
                else if (arg == "-name" && i + 1 < args.Length)
                {
<<<<<<< HEAD
                    cloneName = args[i + 1];
=======
                    _cloneName = args[i + 1];
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)
                    i++;
                }
                else if (arg.StartsWith("-vpId=", StringComparison.Ordinal))
                {
<<<<<<< HEAD
                    cloneId = arg.Substring("-vpId=".Length);
                }
                else if (arg == "-vpId" && i + 1 < args.Length)
                {
                    cloneId = args[i + 1];
=======
                    _cloneId = arg.Substring("-vpId=".Length);
                }
                else if (arg == "-vpId" && i + 1 < args.Length)
                {
                    _cloneId = args[i + 1];
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)
                    i++;
                }
            }

<<<<<<< HEAD
            var cloneSuffix = cloneName != null ? ToKebabCase(cloneName) : string.Empty;
            return new CloneArgs(isMppmClone, cloneName, cloneId, cloneSuffix);
=======
            if (_cloneName != null)
                _cloneSuffix = ToKebabCase(_cloneName);
>>>>>>> b1b74bf8 (feat: add MPPM (Multiplayer Play Mode) support)
        }

        static string ToKebabCase(string name)
        {
            var lower = name.Trim().ToLowerInvariant();
            var kebab = Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(kebab) ? string.Empty : $"-{kebab}";
        }
    }
}
