# Unifying Theory: Causality Matrices and Actor Graphs

This document explores the relationships between **narrative causality** (calendar events, causal links, 4D volumes) and **actor graphs** (temporal graphs of physics cards / good sections). The goal is to ease integration so that narrative state and time can constrain or guide actor pathfinding and action selection.

---

## 1. Introduction

Two main subsystems coexist:

- **Causality / narrative**: Events live in space-time; causal links define *A enables B*. A 4D grid encodes *where and when* narrative is active and at what *causal depth*.
- **Actor graphs**: A graph of *GoodSection*s (physics cards) with transitions; pathfinding finds a sequence of actions from current state to a goal section.

Integration means: **actor paths and available actions should respect narrative causality**—e.g., a section is only feasible if the actor is inside an active narrative volume at the right time, or causal depth can modulate edge cost.

---

## 2. Key Concepts and Notation

### 2.1 Space-time (4D)

- **Bounds4**: A region in space-time with spatial extent and time window.
  - \( \mathcal{V} = (c, s, t_{\min}, t_{\max}) \): center \(c \in \mathbb{R}^3\), size \(s \in \mathbb{R}^3\), time interval \([t_{\min}, t_{\max}]\).
  - Contains: \((p, t) \in \mathcal{V} \iff p \in [c - s/2, c + s/2]\) and \(t \in [t_{\min}, t_{\max}]\).

- **Events**: Each narrative event \(e\) has an optional **spatiotemporal volume** \(V_e\) (a `Bounds4`). When set, the event is “placed” in space-time for triggers and 4D queries.

### 2.2 Causality matrix (event graph)

- **Events**: \(E = \{e_1, \ldots, e_n\}\) with unique IDs.
- **Causal links**: \(R \subseteq E \times E\). \((e_i, e_j) \in R\) means “\(e_i\) enables \(e_j\)” (`NarrativeCausalLink.fromEventId` → `toEventId`).
- **Causal order / depth**: A function \(\mathrm{depth}: E \to \mathbb{Z}_{\ge 0}\) (or rank) consistent with the DAG implied by \(R\). Used when building the 4D grid: volumes are rasterized with a *causal depth* per event.

So the “causality matrix” is the directed graph \((E, R)\) plus an optional depth assignment; the 4D grid then exposes a **causal gradient** in space-time.

### 2.3 4D grid (occupancy and causal gradient)

- **NarrativeVolumeGrid4D**: Discretized \((x, y, z, t)\) with:
  - **Occupancy** \(\mathrm{occ}(p, t) \in [0, 1]\): max over volumes (e.g. 1 inside any volume, 0 outside).
  - **Causal depth** \(\mathrm{causal}(p, t) \in \mathbb{R}_{\ge 0}\): per-cell value from rasterizing volumes with a causal order (e.g. max depth over volumes covering that cell).

- **Build**: `BuildFromVolumes(volumes, causalOrder)` iterates volumes, rasterizes each with its depth; occupancy and causal depth are updated per cell.

- **Query API**: `NarrativeVolumeQuery.Sample4D(position, t)` returns \((\mathrm{occ}, \mathrm{causal})\) at a point. Implemented via `Sample4DImpl` (e.g. backed by `NarrativeVolumeGrid4D.Sample4D` when the 4D generator is active).

### 2.4 Actor graph (temporal graph of good sections)

- **TemporalGraph**: 
  - **Nodes**: GoodSection (physics cards / actions).
  - **Edges**: Connections between sections (from `connectedSections` or explicit `AddEdge`); optional **edge weights**.
  - **State**: Each node can have an associated `RagdollState` (`stateTransitions`).

- **Pathfinding**: `FindPath(currentState, goal)` uses A* from the closest feasible node to the goal. Cost is based on state distance and edge weights; **time and narrative are not yet part of the cost**.

- **GoodSection**: Represents an achievable “good” pose/action; may be tagged for traversability (e.g. climb, throw) and used by `ToolTraversabilityPlanner` to bridge gaps.

---

## 3. Relationships Between Systems

### 3.1 Events → 4D volumes

- **NarrativeCalendarEvent.spatiotemporalVolume** (optional `Bounds4?`): When set, this event is placed in space-time. The same set of volumes (and optional causal order derived from `NarrativeCalendarAsset.causalLinks`) is used to build `NarrativeVolumeGrid4D`.
- So: **Event set \(E\) and causal links \(R\)** drive both the **event graph** and the **4D causal gradient** via volume list and causal order.

### 3.2 4D grid → global query

- **NarrativeVolumeQuery.Sample4DImpl**: When set (e.g. by `SpatialGenerator4D`), any system can call `NarrativeVolumeQuery.Sample4D(position, t)` to get \((\mathrm{occ}, \mathrm{causal})\) at \((p, t)\).
- **NarrativeVolumeQuery.IsInsideNarrativeVolume(position, t)** uses the same 4D data to decide if \((p, t)\) is inside any narrative volume.
- So: **One 4D representation** (occupancy + causal depth) serves triggers, UI, and can serve **actor graph integration**.

### 3.3 Actor graph and causality (current touch points)

- **ToolTraversabilityPlanner**: Builds path plans (walk + tool-use segments). It uses **causality (position, t)** to filter which good sections are valid in the current narrative/time context when bridging gaps.
- **PathfindingNode** (and similar): Can use causal checks to accept or reject sections. So **feasibility of an action** can depend on \((p, t)\) via the 4D query.

Relationship in symbols:

- Let \(S\) be the set of actor states (e.g. ragdoll states).
- Let \(A\) be the set of actions (GoodSections). A path is a sequence of actions.
- **Current**: Feasibility of using action \(a\) at \((p, t)\) can be gated by \(\mathrm{occ}(p, t)\) or \(\mathrm{causal}(p, t)\) (e.g. “only if inside narrative volume” or “only if causal depth \(\ge\) threshold”).

---

## 4. Integrating Causality Matrices with Actor Graphs

### 4.1 Feasibility filter (already in spirit)

- For each candidate GoodSection (or edge), evaluate **narrative feasibility** at the **actor’s (position, time)** (and optionally at the **section’s target position/time**).
- Use `NarrativeVolumeQuery.Sample4D(position, t)` (and optionally `IsInsideNarrativeVolume`). For example:
  - Require \(\mathrm{occ}(p, t) \ge \theta\) (inside some volume), or
  - Require \(\mathrm{causal}(p, t) \ge d_{\min}\) (only after certain causal depth).
- **ToolTraversabilityPlanner** already does this when filtering sections with `queryPosition` and `queryT`. The same idea can be applied inside **TemporalGraph** when enumerating or scoring neighbors.

### 4.2 Cost modulation (integration step)

- Extend cost so that **time and causality** are explicit:
  - State representation: from “current ragdoll state” to “(state, t)” or “(state, t, causal_depth at (p, t))”.
  - Edge cost from \(u\) to \(v\): combine physical/angular cost with a **narrative term**:
    - e.g. \(c(u,v) = c_{\mathrm{phys}}(u,v) + \lambda \cdot \phi(\mathrm{occ}(p_v, t_v), \mathrm{causal}(p_v, t_v))\).
  - Examples for \(\phi\): \(-\log(\epsilon + \mathrm{occ})\) to penalize being outside volumes; or \(-\mathrm{causal}\) to prefer higher causal depth.
- **A* in TemporalGraph**: If `FindPath` takes or infers \((p, t)\) and has access to `NarrativeVolumeQuery.Sample4D`, it can incorporate \(\phi\) into the heuristic or edge cost so that paths prefer narrative-valid (or causal-depth-consistent) regions.

### 4.3 Dynamic graph (optional)

- **Causal links** define which events “enable” which. One can:
  - Associate **subgraphs of GoodSections** with events (e.g. “climb ladder” only after “place ladder”).
  - When building or updating the TemporalGraph, **add or enable** nodes/edges only when the corresponding event is “active” or its causal depth is reached at the actor’s \((p, t)\).
- So the **actor graph** becomes a function of **(current time, current position)** via the 4D grid: \(G = G(t, p)\) where visibility of nodes/edges depends on \(\mathrm{occ}(p,t)\) and \(\mathrm{causal}(p,t)\).

### 4.4 Time-aware pathfinding (formal)

- **State space**: \(\mathcal{X} = S \times \mathbb{R} \times \mathbb{R}^3\) (state, time, position) or a subset.
- **Transition**: \((s, t, p) \to (s', t', p')\) with cost \(c((s,t,p), (s',t',p'))\) that may depend on narrative:
  - \(c = c_{\mathrm{phys}} + \lambda \cdot \phi(\mathrm{occ}(p',t'), \mathrm{causal}(p',t'))\).
- **Goal**: Reach a goal section (and optionally a goal time/region). A* (or similar) on this extended state space with narrative-aware costs yields **time-aware, causality-aware** paths that align with the causality matrix and 4D volumes.

---

## 5. Mathematical Summary

| Concept | Symbol / definition |
|--------|----------------------|
| Events | \(E\), IDs; optional \(V_e = \mathrm{Bounds4}\) |
| Causal links | \(R \subseteq E \times E\) (from → to) |
| Causal depth | \(\mathrm{depth}: E \to \mathbb{Z}_{\ge 0}\) (from DAG of \(R\)) |
| 4D occupancy | \(\mathrm{occ}(p, t)\) from NarrativeVolumeGrid4D |
| 4D causal gradient | \(\mathrm{causal}(p, t)\) from same grid |
| Actor states | \(S\) (e.g. RagdollState) |
| Actions / sections | \(A\) (GoodSection set) |
| Actor graph | \(G = (A, \mathrm{Edges}, w)\); pathfinding from state to goal section |
| Narrative feasibility | Gate on \(\mathrm{occ}(p,t)\), \(\mathrm{causal}(p,t)\) |
| Integrated cost | \(c = c_{\mathrm{phys}} + \lambda \phi(\mathrm{occ}, \mathrm{causal})\) |

**Core idea**: The causality matrix \((E, R)\) and depths define **when/where** narrative is active. The 4D grid turns that into a **scalar field** \((\mathrm{occ}, \mathrm{causal})\) on space-time. Actor graphs can **filter** (feasibility) and **weight** (cost) using this field so that paths and actions respect narrative causality.

---

## 6. Next Steps and Open Questions

1. **Causal order algorithm**: How is `causalOrder` (list of depth indices per volume) derived from `NarrativeCalendarAsset.causalLinks`? Ensure it is a valid topological order of the event DAG.
2. **Heuristic for A***: Design an admissible heuristic for time–causality–state space (e.g. lower bound on remaining cost that respects causal depth).
3. **Multiple agents**: If several actors share the same narrative grid, how to avoid conflicting use of “one-time” events (e.g. one ladder placement)?
4. **Inverse use**: Use actor graph structure (e.g. required sections to reach a goal) to **suggest** or **validate** causal links (e.g. “place ladder” must precede “climb ladder” in the calendar).
5. **Smooth causal depth**: Causal depth is currently rasterized per cell. For smoother cost, consider interpolating causal depth (and occupancy) in the 4D grid or using a small analytical kernel.

---

*This document is a living reference for integrating narrative causality with actor graphs. Implementation references: `NarrativeCalendarAsset`, `NarrativeVolumeGrid4D`, `NarrativeVolumeQuery`, `TemporalGraph`, `ToolTraversabilityPlanner`, `PhysicsCardSolver`, `SpatialGenerator4D`.*
