# Body interior, eating, and bathroom hygiene

## Mouth / teeth

- `MouthInteriorRuntime` — upper/lower spline teeth, gum height maps, saliva edge loop, food-in-mouth sphere, mouthfeel longevity
- Default adult set (32): incisors/canines = **Front**; premolars+ = **MolarBack**
- Front bite = up/down; molar chew = 3D roll; cheese tongue = parabola; meat = progressive back molars + preferred side
- `DeveloperRespectsSeed` — preferred chew side in **[0.50, 0.55]**
- Lip wrap: `Locomotion/LipEdgeWrap` + `LipEdgeWrapDriver` (capsule tracks for brush/finger/tools)
- Gum maps: `GumHeightMapGenerator` (tongue channel + bezel from 50% tooth height)

## Eating

- `FoodItem` + `FoodKind` (Meat / Cheese / FruitVegetable)
- `ChewConvexTreeBakeService` — section breakup vs front-teeth ellipsoid
- BT: `EatFoodNode`, `BiteNode`, `ChewNode`, `SwallowNode`, `AnimationChewNode`
- `GoalType.Eat`
- Narrative: `BiteNarrativeAction` / `ChewNarrativeAction` / `SwallowNarrativeAction` / `AnimationChewNarrativeAction` + `NarrativeBehaviorSpec` builders
- IK categories: `Bite`, `Chew`, `Swallow`

## Digestion

- `FoodProcessorBioRhythmService` — swallow → nutrients; adjust-to-normal setpoints; smell whitelist; optional poop queue
- Organs: `bladder`, `intestines`, `urethra` + channels `bladder_fill` / `bowel_fill`
- `BowelBladderRuntime` / `IOrganSystemHost` (ragdoll + vehicle weak hosts)

## Bathroom

- `PaperScrollSystem` — spool-style cylinder, sheet/empty textures, off-roll length
- `ScrunchToiletPaperNode` — 3–5 sheets + Mandelbrot bun fold
- `ToiletStation` — `includesBidet=true` default; before/after sit nodes; optional TP BT
- `PeeStreamDirector` — urethra tip; jitter over first 90° (`peeDirectionJitterDegrees`, 0 = off)
- `PoopRuntime` — wetness/smell/texture; SDF or coil visuals
- `FreeExcreteNode` / `ExcreteOnToiletNode`

## Hygiene BTs

| Node | Role |
|------|------|
| `BrushTeethNode` | Per tooth × 3 sides |
| `BrushTongueNode` | Tongue curl/pocket |
| `FlossTeethNode` | Adjacent pairs |
| `WashHandsNode` | Sink open/close + hand smell clear + manifold whitelist |
| `ShowerNode` | Whole-body smell clear + manifold lists (skin blacklisted) |

Clear APIs: `HygieneSmellClearService`, `HygieneManifoldClearService`.

## Editor

`Window/System Drawer/Hygiene/Hygiene Editor` — Mouth (RT preview, tooth sliders, gum generate), Toilet, Sink, Shower. Hub: `SystemDrawerHubMenuCatalog`.

## Cross-links

- Drink mouth/nozzle (sip alignment) — eat bridges LifeSystems (drink still does not)
- Open/close topologies for peels/lids/sinks/showers (duck-typed from Runtime to avoid asm cycles)
- Rope spool APIs inspired `PaperScrollSystem` (not `rope_grapple`)
- `SLOW_TIME_GAMBIT.md` / wrestling docs for narrative registration pattern
