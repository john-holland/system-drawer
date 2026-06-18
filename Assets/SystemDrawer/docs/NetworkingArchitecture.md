# System Drawer Networking Architecture

Implementation companion for the dual-transport networking model on top of System Drawer services.

## Overview

| Channel | Transport | Purpose |
|---------|-----------|---------|
| `TreeStreamChannel` | TCP (reliable, ordered) | LOD pre-warm, 4D/3D tree snapshots, audit/reconcile, save/load, scene load |
| `DecisionChannel` | UDP + ack handshake | Branch polls, ownership transfers, lockstep player decisions |

Assemblies:

- `SystemDrawer.Networking` — runtime orchestrators, transport, MenuRagdoll, adapters
- `SystemDrawer.Networking.Editor` — dedicated server window, menu setup, launch-arg helpers
- `SystemDrawer.Networking.Tests` — EditMode unit tests

BedogaGenerator does **not** reference Networking; [`NetworkSpatialOrchestratorAdapter`](NetworkSpatialOrchestratorAdapter.cs) resolves `Spatial4DOrchestrator` via `SystemDrawerService`.

## Service keys

| Key | Component |
|-----|-----------|
| `network.clientOrchestrator` | `ClientOrchestrator` |
| `network.serverOrchestrator` | `ServerOrchestrator` |
| `network.serverMode` | `ServerOrchestrator` (mode owner on server) |
| `network.lobbyServer` | registered while lobby host active |
| `menu.ragdoll` | `MenuRagdollBase` root |

Constants: [`SystemDrawerServiceKeys`](../SystemDrawerServiceKeys.cs).

## Server modes

| Mode | Trees | Server role |
|------|-------|-------------|
| **SinglePlayer** | Client-local; server is organizational | Loopback TCP bookkeeping, level load API |
| **Authoritative P2P** | Peer-owned + server causality 4D tree | LOD streaming, `PeerTransferable` ownership via UDP |
| **Classic Lockstep** | Client mirrors server snapshot | Causality-family audit; UDP decisions validated |

## Tree transmit policies

Register descriptors on `ServerOrchestrator.TreeRegistry`:

- `LocalOnly` — e.g. player controller BT (never replicated)
- `ServerAuthoritative` — e.g. scoring / rules BT
- `PeerTransferable` — e.g. elevator; `TransferTreeOwnership(treeId, clientId)`
- `SpectatorReadOnly` — spectators receive tree deltas but cannot send UDP decisions or take ownership

`NetworkLodScheduler` uses `NetworkSettings.clientLodRadius` / `serverLodRadius` (server radius > client) for TCP pre-warm.

## MenuRagdoll

Menu nodes are [`MenuRagdollNode`](Networking/MenuRagdollNode.cs) components extending [`SGBehaviorTreeNode2D`](../../BedogaGenerator/SGBehaviorTreeNode2D.cs), so the same hierarchy drives both event routing and 2D spatial placement (`fitX`, `stackDirection`, prefabs, etc.).

[`MainMenuSpatialGenerator`](Networking/MainMenuSpatialGenerator.cs) wraps [`SpatialGenerator`](../../BedogaGenerator/SpatialGenerator.cs) in 2D mode, ensures `SGTreeNodeContainer` on the menu root, and exposes:

- **`syncNetworkRequirements`** — when on, **Update Main Menu for Network Requirements** applies the canonical tree from [`MainMenuNetworkRequirements`](Networking/MainMenuNetworkRequirements.cs) and marks nodes `managedByNetworkRequirements` (spec-owned fields read-only in inspector). When off, developers edit spatial/menu fields manually on nodes.
- **`removeOrphansWhenSyncing`** — optional prune of managed nodes not in the spec (only when sync is on).

Editor actions:

- **Window → System Drawer → Networking → Create Main Menu Ragdoll** — creates root + wizard, runs sync
- **Window → System Drawer → Networking → Update Main Menu for Network Requirements** — refresh selected or scene menu from spec
- Inspector on `MainMenuSpatialGenerator`: **Update Main Menu for Network Requirements**, **Generate Menu Layout**

Named events (`MenuRagdollEvent.Name`) bubble to `MenuRagdollBase.HandleBubble`:

- `start`, `multiplayer`, `settings`
- `save`, `load`
- `lobby.connect`, `lobby.join`, `lobby.spectate.join`, `lobby.host.start`, `lobby.host.stop`, `lobby.game.start`, `lobby.game.end`
- `lobby.host.options`, `lobby.host.password`, `lobby.join.password`

Optional Tomba-style 2D hanging physics: `enableHangingPhysics` on `MenuRagdollBase` → anchor + plank `Rigidbody2D` + `DistanceJoint2D`.

Lobby join container uses event `lobby.join.group` (grouping only); leaf **Join Game** uses `lobby.join`.

## Lobby hosting

`LobbyServerHost` — lightweight TCP registration (protocol v2: `REGISTER role=player|spectator` / `QUERY`).

QUERY response includes `players=P/maxP`, `spectators=S/maxS`, `allowSpectators=0|1`, `passwordRequired=0|1`.

Optional lobby password: SHA-256 hash via [`LobbyPasswordHash`](Networking/LobbyPasswordHash.cs) with session-name salt. Wrong or missing password returns `ERR password` on REGISTER (same message for both cases).

Join flow:

1. `LobbyClientQuery.Query` → read caps and `passwordRequired`
2. If password required, bubble `lobby.join.password.required` when empty
3. `LobbyClientQuery.Register` with role + password
4. Connect to game port with hello `role=player|spectator` (+ optional `passwordHash=`)

Spectators connect **TCP only** (no UDP decisions). Tree stream uses `TreeTransmitPolicy.SpectatorReadOnly`.

Enabled via:

1. CLI: `--host-lobby [--lobby-port 7780] [--lobby-name "..."]`
2. Menu: `lobby.host.start` / `lobby.host.stop`
3. Runtime CLI: `lobby start [port]`, `lobby stop`, `lobby status`
4. Headed UI: **Dedicated Server Window**

Precedence: launch flags > runtime CLI > menu. `--no-lobby` blocks menu start.

## CLI flags

| Flag | Default | Meaning |
|------|---------|---------|
| `--dedicated-server` / `-ds` | off | Dedicated server entry |
| `--listen-port` / `-p` | 7777 | Game port |
| `--mode` / `-m` | `single` | `single` \| `p2p` \| `lockstep` |
| `--host-lobby` | off | Start lobby at boot |
| `--lobby-port` | 7780 | Lobby port |
| `--lobby-name` | Drawer 2 | Session name |
| `--lobby-password` | (empty) | Plaintext lobby password at boot (hashed before storage) |
| `--no-lobby` | off | Disable lobby even from menu |
| `--bind-address` | 0.0.0.0 | Bind address |

Example:

```bash
MyGame.exe -batchmode -nographics -ds -m p2p -p 7777 --host-lobby --lobby-port 7780 --lobby-name "Campaign Co-op"
```

## Facilitator wiring

[`SystemDrawerFacilitator`](../SystemDrawerFacilitator.cs) uses loose `Object` wizard refs (asmdef cycle safe):

- `networkServiceWizard` → `NetworkServiceWizard.RegisterAll()`
- `menuRagdollServiceWizard` → registers `menu.ragdoll`

## Narrative time travel (multiplayer)

Hybrid authority: dedicated server / lockstep → server validates rewind requests; P2P host may initiate. [`NarrativeTimeTravelNetworkBridge`](Networking/NarrativeTimeTravelNetworkBridge.cs) forwards `NarrativeTimeTravelCoordinator.RewindRequested` to `ClientOrchestrator.RequestNarrativeRewind`.

| TCP TreeStream type | Direction | Purpose |
|---------------------|-----------|---------|
| `narrativeRewindRequest` | client → server | `{ seq, targetTime, requesterId }` |
| `narrativeRewindApply` | server → all clients | full `NarrativeTimeTravelCheckpoint` JSON |
| `narrativeCheckpointPush` | server → clients | incremental ledger + weather frame for late joiners |

All peers (including spectators) apply checkpoints locally via `NarrativeTimeTravelCoordinator.ApplyRewindLocal`. Rewind uses `NarrativeRewindUndoWalker` for ledger trim, action undo, and snapshot restore.

## Causality audit (lockstep)

[`CausalityFamilyAudit`](CausalityFamilyAudit.cs) rejects bisecting snake forks: sibling leaf prefixes under the same parent chain without proper extension.

[`LockstepDecisionValidator`](LockstepDecisionValidator.cs) validates UDP decision packets against registry ownership.

## Impersonation

Headed server: `ServerOrchestrator.ImpersonateClient(clientId)` → `ImpersonationSession` loopback on `127.0.0.1:<port>`.

## Tests

EditMode: `Assets/SystemDrawer/Networking/Tests/` — menu gating, event routing, launch args, tree registry, causality audit, lobby protocol/password, spectate role, main menu network sync, spatial context, node inheritance.

Narrative/time-travel: `Assets/locomotion/narrative/Tests/`, `Assets/Planetary/Tests/PlanetaryWeatherTimeTravelTests.cs`.

## Related docs

- [SpatialGenerator4D_Setup.md](../../BedogaGenerator/SpatialGenerator4D_Setup.md) — multiplayer tree descriptors
- [README.md](../../../README.md) — services catalog
