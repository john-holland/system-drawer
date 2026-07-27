# Kissing + HeavyPetting IK

Lip-midpoint kiss clinch for `LoveMakingMoveKind.Kiss`, multi-actor heavy-petting training assets, kiss lemmas with intensity / animation-key overrides, and LifeSystems chemistry (serotonin / oxytocin).

## Runtime flow

1. `LoveCard` with `loveMoveKind = Kiss` → `LoveMakeObjectNode` (or `KissingBehaviorTreeNode`)
2. `KissingExecution.Begin` resolves actors via `HeavyPettingIKActorRegistry` (or ephemeral mouth/ragdoll)
3. Lip anchors from `MouthInteriorRuntime.GetLipLoopMidpointWorld` / `EnsureLipMidAnchor`
4. Dual `IkTowLink` lip↔lip; jaw open from `kissAnimationIntensity` (or `kissJawOpen01`)
5. On commit: `LoveMakingPsychEffectService` applies romance + kiss chemistry

Head↔Head remains the fallback when mouths are missing. Embrace topology path is unchanged for non-Kiss moves.

## HeavyPettingIKActorRegistry

Scene component: keyed entries with actor, ragdoll, mouth, enabled/disabled ragdoll sections, and **optional** open/close topology (`ScriptableObject` slot for `OpenCloseTopologyAsset`) + root target (same keys; bake still via `OpenCloseTopologyBtBuilder`).

## Animation selection

| Precedence | Source |
|---|---|
| 1 | `kissAnimationKey` from `{P:kiss\|kiss-animation=...}` |
| 2 | Intensity band tag |
| 3 | `.intimate` suffix when `intimateStyle` |

| Intensity | Tag |
|---|---|
| 0.0–0.2 | `lovemaking.kiss.peck` |
| 0.2–0.45 | `lovemaking.kiss` |
| 0.45–0.7 | `lovemaking.kiss.smooch` |
| 0.7–1.0 | `lovemaking.kiss.making_out` |

Default intensity: **0.35**. Lemma defaults: peck 0.12, kiss 0.35, smooch 0.55, making out 0.85.

## Lemma grammar

Canonical Continuuuum form (`{P=...}` also works where `LemmaWatch` accepts it):

```text
just giving Egon a {P:smooch|kiss-animation=slimer-kiss}.
{P:kiss|kiss-animation=animation-key}
{P:peck|kiss-animation-intensity=0.12}
{P:making-out|kiss-animation-intensity=0.85}
```

Keys: `kiss-animation`, `kiss-animation-intensity` (aliases `kissAnimation`, `kiss_intensity`).

Built-in verbs: kiss, peck, smooch, smooching, making-out, make-out.

## Chemistry

On Kiss commit (scaled by `kissAnimationIntensity`):

- `serotonin`, `oxytocin`, affection, morale bumps
- Making-out band also bumps arousal slightly
- Unrequited + poor response (`kissResponseNegative`, `harshRejectionResponse`, or FriendZone/OnTheRocks unrequited): large **blood pressure** drop + `acidity` / `reflux` spike

## PhysicsIK training

Categories: `LoveKiss`, `LoveHeavyPetting`. Asset: `HeavyPettingIKAnimation` (Create → Locomotion → Love Making → Heavy Petting IK Animation) with actor keys, contact specs, section include/exclude, intensity band.

## Editors

Card Planning: Kiss move exposes intensity slider, animation key, jaw override, actor keys, HeavyPetting asset, negative-response flag.
