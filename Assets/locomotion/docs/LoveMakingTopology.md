# Love-making topology

Intimacy stack adapted from wrestling: `LoveCard : WrestlingCard`, slow-time card pick, gentler `IkTow` embrace topology, romance LifeSystems channels, and calendar biorhythm tint.

## Modes

| Mode | Intent |
|------|--------|
| Tender | Soft approach / hold / comfort |
| Passionate | Higher physicality (calendar pink→red) |
| Playful | DanceClose / nuzzle / light caress |

Move kinds: Approach, Embrace, Kiss, Hold, Caress, Nuzzle, DanceClose, Part.  
Anim tags: `lovemaking.*` via `LoveMakingAnimationGroup`.

## Cards

`LoveCard` under `pathing/lovemaking/`:

- Desires: Affection, Closeness, Pleasure, Comfort, Trust, Play
- `physicality01` (calendar pink→red), `requiresConsent`, `maxParticipants`
- `MeetsLoveRequirements` = wrestling limb/size gate + `RomanceProfile.AllowsIntimacyWith`
- `ConsiderLoveMakingCards` emits into `PhysicsCardSolver`; `GoalType.LoveMaking`

## Session + planner

| Type | Role |
|------|------|
| `LoveMakingSession` | Participants, time budget, `LoveMakingTopologyGoal`s |
| `LoveMakingPlannerSolver` | Sequence LoveCards for time/desire/anim topology |
| `LoveMakingPlannerService` | `ITravelRiskPlannerService` id `"lovemaking"` |
| `ConsentWardenPlannerService` | Soft-cap physicality / force consent flag |

Wired through `TravelRiskPlannerPipeline` after wrestling/referee.

## BT / narrative

- `LoveMakingCardSelectionSession` + `WaitForLoveMakingCardSelectNode`
- `LoveMakeObjectNode` → `LoveMakingTopologyRuntime.BeginEmbrace`
- Narrative: EnterSlowTime / Choose / Commit / BioRhythm love actions (Tree Editor kinds)

## Psychology + society

Romance channels: affection, intimacy, trust, attachment, jealousy, arousal.  
`LoveMakingPsychEffectService` on card commit.  
`RomanceSocietalImpactService`: default `SocietalImpact = 1/population`; override to `1` for big-G.

## Romance model

`RomanceProfile` + admirer links, fidelity/mesh/baggage enums.  
Severity ladder: FriendZone → Notion → Crush → GoingOut → GoingSteady → HotAndHeavy → OnAgainOffAgain → Newlywed → Married → OnTheRocks → Estranged → Separated → Divorced.  
Bases: `RomanceBase` NA (default) → First → Second → Third → Home (per profile + per admirer link).  
Stubs: `SeductionDialogNode`, `RomanceGroupDynamicsStub`, `LemmaWatch` (`{P=…|non-ik-animation=true}`).

## Calendar biorhythm

`NarrativeCalendarAsset.showBioRhythmEvents` (+ `NarrativeScheduler` forwarder):

- **Blue** — health / clinical
- **Pink→Red** — love physicality × participants
- **Purple** — political / society spikes

Kind mapping: `RomanceBioRhythmCalendarColors`.

## Card Planning Editor

KindTint: Goal blue, Card pink, Action purple; LoveMaking move chips on the defaults bar.
