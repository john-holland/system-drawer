# Seated IK Towing

Physics-card seated / stand-on occupancy with hierarchical CoG towing. Compatible with ragdoll ambulation (`PhysicsCardSolver` / muscle impulses) — not FinalIK.

## Occupancy modes

| Mode | Tow chain | Primary contact |
|------|-----------|-----------------|
| `Sit` | support → seat → pelvis → torso | Pelvis on seat plane |
| `StandOn` | support → seat → feet → pelvis | Both soles on seat plane |

`SitSurfaceContact` describes the plane + polygon (chair, stack, wall ledge, books). `IkTowChain` keeps the occupant pose stable while the support may rotate/translate underneath.

## Runtime

- `SeatedOccupancyRuntime` — per-actor tow + feet-reach probe + CoG evaluate
- `SeatedCogStabilizer` — tip risk vs polygon; restore impulses
- Free-hang (sit, feet miss ground): arms + legs + abs via `SitBalanceCard`
- Stand-on tip recovery: ankles/hips first, then arms/abs

## Goals / cards / nodes

- `GoalType.Sit`, `GoalType.StandOnSurface`
- `SitCard`, `SitBalanceCard`, `StandOnSurfaceCard`, `ChairRotateCard`, `ChairSchoochCard`
- `SitOnSurfaceNode`, `StandOnSurfaceNode`, `ChairRotateNode`, `ChairSchoochNode`

## IK training categories

`PhysicsIKTrainingCategory.Sit`, `StandOnSurface`, `ChairRotate`, `ChairSchooch`
