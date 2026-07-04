# Music Section Theory

Theory backing for causality-linked sectional music assembly.

## References

- Krumhansl (1990) — tonal hierarchy and key profiles
- Lerdahl (2001) — tonal pitch space and voice leading
- Temperley — metric sync and hypermeter
- Lerdahl & Jackendoff — metric hierarchy

## Circle of fifths

Tonic steps of ±7 semitones (mod 12) are fifth-related. The modulation savings bank rewards consecutive fifth motion (G→D→A) and penalizes oscillation (A→G→A).

## Krumhansl key profiles

`MusicTheory.TonalDistance(a, b)` uses correlation of major key profiles rotated to each tonic. C→G is closer than C→F♯.

## Voice leading

`VoiceLeadingPenalty` uses minimal circular distance between chord roots on the pitch-class circle.

## Common tones

Pivot modulation is cheap when `CommonToneCount >= 2`.

## Metric hierarchy

Stem swaps align on downbeat phase (`barPhase`). Hypermeter: prefer 4- and 8-bar phrases.

## Poetic meter vs musical meter

Iambic feet map to 2-beat groups; pentameter (5 feet) overlays ~10 beat groups without forcing 4/4.

## Golden test vectors

| Case | Expectation |
|------|-------------|
| C→G | Lower cost than C→Gb |
| Fifth chain G→D→A | Higher savings, lower modulation spend |
| G→C→G oscillation | Higher oscillation penalty |
| Same seed + causality leaf | Identical rhythm quad walk |
