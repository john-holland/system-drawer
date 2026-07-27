# Body interior, eating, and bathroom hygiene

## Goal wiring (working loops)

Add [`BodyHygieneGoalRouterNode`](../nodes/BodyHygieneGoalRouterNode.cs) under the actor BT root (or as a Selector child). It dispatches:

| GoalType | Node | Card flags / tags |
|----------|------|-------------------|
| `Eat` | `EatObjectNode` → `EatFoodNode` | `isEatGoal`, tag `eat` |
| `Toilet` | `ToiletVisitNode` (before → excrete → after) | `isToiletGoal`, tag `toilet` |
| `Hygiene` | `HygieneGoalNode` | `isHygieneGoal` + `hygieneKind` |
| `Interaction` + `free_excrete` | `FreeExcreteNode` | — |

[`ConsiderBodyHygieneCards`](../ingestion/ConsiderBodyHygieneCards.cs) auto-emits default cards into `PhysicsCardSolver`. [`PhysicsCardSolver.FindCardMatchingGoal`](../PhysicsCardSolver.cs) matches Eat / Toilet / Hygiene.

Bowel threshold queue uses **`GoalType.Toilet`** (not Sit).

## Mouth / teeth

- `MouthInteriorRuntime` — upper/lower spline teeth, gum maps, saliva edge loop, food-in-mouth sphere/mesh, preferred chew side
- Spawns tooth visuals (`RebuildToothVisuals`); binds gum heightmaps via material property blocks
- Front bite = `DriveFrontBite` (vertical jaw); molar chew = `DriveMolarRoll` (3D roll + lateral)
- Default adult set (32): incisors/canines = **Front**; premolars+ = **MolarBack**
- `DeveloperRespectsSeed` — preferred chew side in **[0.50, 0.55]**
- Lip wrap: `Locomotion/LipEdgeWrap` + `LipEdgeWrapDriver`
- Brush faces: **buccal / lingual / occlusal** via `ResolveToothFaceNormals`

## Eating

- `FoodItem` + `FoodKind` (Meat / Cheese / FruitVegetable)
- `ChewConvexTreeBakeService` — public `ConvexMeshTreeCache.Leaves` bounds (no private reflection)
- BT: `EatFoodNode`, `BiteNode`, `ChewNode`, `SwallowNode`, `AnimationChewNode`
- `EatingAnimationDriver` maps `animationGroupTag` → `PhysicsIKTrainingCategory` Bite/Chew/Swallow
- Bake sections drive progressive molar chew count; fruit discard/put-back duck-types open/close

## Digestion

- `FoodProcessorBioRhythmService.OnSwallow` → nutrients + smell whitelist + `pendingPoop` payload
- `CreatePoop` / `SpawnPoopFromPayload` — explicit factory; clears bowel fill after spawn
- Organs: `bladder`, `intestines`, `urethra` + channels `bladder_fill` / `bowel_fill`
- `BowelBladderRuntime` / `IOrganSystemHost` / `VehicleOrganHost` (vehicles via `VehicleActor`)

## Bathroom

- `PaperScrollSystem` — spool cylinder, sheet/empty textures
- `ScrunchToiletPaperNode` — Mandelbrot bun fold
- `ToiletStation` — `includesBidet=true`; bidet clears groin smells; else TP BT
- `PeeStreamDirector` — urethra tip; jitter first 90° (`peeDirectionJitterDegrees`, 0 = off); duck-typed flood + stream renderer
- `PoopRuntime.SpawnInBowl` — rope-style coil capsules + SDF scale + smell emitter
- `FreeExcreteNode` — ground spawn + vehicle organ host

## Hygiene BTs

| Node | Role |
|------|------|
| `BrushTeethNode` | Per tooth × buccal/lingual/occlusal |
| `BrushTongueNode` | Tongue curl/pocket |
| `FlossTeethNode` | Adjacent pairs |
| `WashHandsNode` | Sink open/close + hand smell clear + manifold whitelist |
| `ShowerNode` | Whole-body smell clear + manifold lists (skin blacklisted) |

## Editor

`Window/System Drawer/Hygiene/Hygiene Editor` — Mouth (RT tooth preview, jaw/gum, rebuild visuals), Toilet, Sink/Shower (topology bake + manifold clear preview). Hub: `SystemDrawerHubMenuCatalog`.

## Cross-links

- Open/close topologies for peels/lids/sinks/showers (duck-typed; no Runtime→Open asmref)
- Drink nozzle/flood APIs used via reflection for saliva/pee
- Card Planning Editor can author Eat/Toilet/Hygiene goals as plan nodes
