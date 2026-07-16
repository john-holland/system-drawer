# DSP-enabled instrumentation control

## Spine

- **InstrumentProxy / InstrumentProxyBank** — player & score events → `DSPParams`
- **PerformanceControlMap** — control surface → articulation channel
- **playerInteractionQuantize01** — `[0,1]` free → hard grid
- **InstrumentProfileCurves** — timbre + instrumentation response; `enforceTraditionalDefaults`
- **InstrumentFamily.Electronic** — DAC, drum machine, digital FX; owns **dry/wet**, **LFO**, **PWM**, wave shapes (`ElectronicOptionKeys`)
- **MusicCompositionPlayer** — wired from `CausalityMusicBridge` when plan has nodes
- **DigitalEffectsMachine** — Electronic family; GameObject + component DSP graph; nested `AudioEquipmentTrace`
- **AudioPowerBudget** — gathers requirements; unrealistic quality warning (no mute)
- **AnalogReferenceMachine** — Electronic (DAC default); TRS/XLR/DAC/amp/aux… throughput
- **Physical sims** — string/wind/free-reed/percussion/keyboard/resonance/**electronic** + case topology + attenuated open/close
- **Case / lid open** — Audio stays Open-free; wire via `Locomotion.Open.InstrumentOpenCloseBridge` (see `Assets/locomotion/open/docs/open-close-topology.md`)
- **Score** — MIDI, tabs, audio features, MusicXML; OCR feature-flagged
- **Editor** — Audio Equipment Timeline, Music Timeline Overlays
- **Rail ding radial cache** — `RailDingRadialCache` / `RailDingChainPlayer` prebake azimuth×listener-band metal DING DONG chains for stairwell railings (see `Assets/locomotion/docs/STAIRWELL_NIGHTSTICK_FISH.md`)

## Menus

- Window → System Drawer → Music → Composition Summary
- Window → System Drawer → Music → Audio Equipment Timeline
- Window → System Drawer → Music → Timeline Overlays
