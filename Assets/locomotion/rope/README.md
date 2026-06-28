# Rope Physics Module

General-purpose rope simulation for Drawer 2 locomotion: grapple, serpent, and spool modes share one arc-length + ring-buffer core.

## Quick start

1. **GameObject → Locomotion → Rope System Demo** — creates scene hierarchy and saves `Prefabs/RopeSystemDemo.prefab`.
2. Assign `headAnchor`, `spoolAnchor`, optional `ropeMaterial` using shader `Locomotion/RopeStrainRadial`.
3. Drive wind with `RopeSystem.SetWindRate(signedMps)` (+ reel in, − pay out).
4. Add `ConsiderRopeCards` + `PhysicsCardSolver` on the same actor for AI cards.

## Tick order

1. `RopeWindingController` — arc window + ring slot swap  
2. Unity physics (active rigidbodies only)  
3. `RopeTensileModel` — tension, snap  
4. `RopeCordSolver` — overlap corrections  
5. `RopeRadialStrainCache` + `RopeAudioMap`  
6. `RopePathingFootprint` — path samples  

## Pathing

Enable **Rope Footprint Clearance** on `TravelAgentMultibodySettings` to relax plans against registered `RopePathingFootprint` samples.

## Tests

Run Edit Mode tests: `RopeRingBufferTests`, `RopeTensileCacheTests`, `RopeOverlapIndexTests`, `RopeSnapTests`, `RopePathingFootprintTests`.
