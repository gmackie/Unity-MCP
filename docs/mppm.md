# Multiplayer Play Mode (MPPM)

Unity-MCP can drive Unity's [Multiplayer Play Mode](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@latest) (MPPM) virtual players, letting an AI agent launch, inspect, and tear down extra editor instances for multiplayer testing — all through MCP.

## How it works

- MPPM **virtual players** ("clones") are separate editor processes that Unity launches from the same project.
- When the plugin detects it is running inside a clone — via the launch arguments `-editor-mode com.unity.mppm.clone` (plus `-name` and `-vpId`) — the clone does **not** start its own MCP server. Instead it connects to the **main** editor's server as an additional instance.
- Clones are **read-only**: every mutating/write tool is disabled on them. A clone shares the main project's assets read-only, cannot modify them, and can never spawn or tear down other clones. This keeps a single source of truth for edits (the main editor) while clones are used for observation.

## Tools

| Tool | ID | Description |
| :--- | :--- | :--- |
| **MPPM / List Players** | `mppm-list-players` | List MPPM virtual players (slots 1–4) with their name, state, and type. Player 1 is the main editor; 2–4 are clones. Read-only. |
| **MPPM / Activate Player** | `mppm-activate-player` | Launch an MPPM virtual-player clone (slot 2–4) as a separate editor instance. |
| **MPPM / Deactivate Player** | `mppm-deactivate-player` | Shut down a running MPPM virtual-player clone. |

`mppm-list-players` takes an optional `includeMainEditor` flag (default `true`); set it to `false` to return only the clone slots (2–4) — usually what you want when orchestrating clones.

## Inspecting a specific player

Activating a clone connects it to the main editor's MCP server as its own instance, so each player can be inspected independently — its scene, hierarchy, and screenshots are its own.

To route a tool to a specific instance (the main editor vs. a particular clone), the server must be running in **multi-instance mode**: select the target with the server's instance-selection tool, then read that instance's state. Independent multi-editor routing therefore requires a GameDev-MCP-Server build that supports multi-instance mode.

## Requirements

- Unity's Multiplayer Play Mode — the `com.unity.multiplayer.playmode` package. It is built in to Unity 6; on earlier versions it is available as a package.
- If MPPM is not installed or otherwise not usable, the tools return a clear error instead of failing silently.

## Typical workflow

1. `mppm-list-players` — see slots 1–4 and their current state.
2. `mppm-activate-player` with `playerIndex: 2` — launches Player 2; it connects to the main editor's server as a read-only instance.
3. Select the clone instance and inspect it (scene, hierarchy, screenshots), independently of the main editor.
4. `mppm-deactivate-player` with `playerIndex: 2` — shuts the clone down.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
