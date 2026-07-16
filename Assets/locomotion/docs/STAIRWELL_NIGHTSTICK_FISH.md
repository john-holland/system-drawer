# Stairwell Nightstick Fish

Spy KO → claim dual nightsticks → topological rail deflection descent → beat the elevator.

## Pieces

- `StairwellTopologyAsset` — floors + railing ids (depth-first from top)
- `StairwellRailingNode` — strike points, friction/mass hints, registers as `PathingAperture` (`stair_rail`)
- `NightstickWeapon` — grip + dual-wield pair
- `MuscularFatigueAdrenalineState` — fatigue / adrenaline / strength
- `RailDeflectionSuccessEstimator` — P(success) + suggested activation
- `RailDingRadialCache` + `RailDingChainPlayer` — prebaked azimuth×band DING DONG chains
- `StairwellNightstickFishDirector` — sequence orchestration; reuses gambit aperture registry for rail targets

## DSP

Prebake via `RailDingRadialCache.PrebakeRailing`. On strike, `PlayDingChain(count)` fires timed DING/DONG pitches with transmission falloff. Pair with Resonance Metal / `SoundEffectCache` binary ids in production content.
