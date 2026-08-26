# Pen and Ink

Ink is a **profile + nib + lemmas + studio window** on the existing paint canvas, SPH hydro, drink/liquid nozzle, and open/close topology. There is no second canvas.

Window: `Window/System Drawer/Pen and Ink Studio`. Bake folder: `Assets/locomotion/painting/Baked/Ink`. Feature budget: `paint_ink` (`FeatureBudgetIds.PaintInk`) with prefixes `PenInk`, `InkDrying`, `QuillNib`.

## Defaults vs paint

| Knob | Paint today | Ink default |
|------|-------------|-------------|
| SPH dry | `0.02 * dt` | `sphDryRate = 0.45` |
| Layer thickness | ~ferrule stamp | `layerThicknessM = 0.0004` |
| Dilution / mix | implicit lerp, stacked films | `dilution = 0.75`, **single-layer mix** unless `paintlikeInk` |
| Specular | hydro film/beads | `specularWet 0.85` → `specularDry 0.08` |
| See-through dry | none | **30 s** at `seeThroughAlpha 0.12` (whiteboard spy gag) |

`PaintCanvas.ApplyInkProfile()` no-ops when `inkProfile` is null, so paint scenes stay unchanged.

Stamp mix: `PaintCanvasLayerStack.MixDeposit` lerps pigment into the top wet layer when `MixesIntoSingleLayer`. Paintlike ink keeps the stacked 0.35 lerp.

Quill/Nib brush kinds live on `PaintBrushDefinition.BrushKind` (pointed, angle-limited Gaussian). `QuillNibDefinition.maxBendDeg` defaults to **10°**. Spread uses `GaussianSpread01(contactAngle)`.

Canvas `surfaceKind`: `Plane` (default) or `CurvedDecal` (`PaintCanvasCurvedDecal` cylinder-section UV along the arc). Hydro still writes viscosity RGBA; curved UV only changes stamp mapping.

## Spy gag (see-through dry)

`InkDryingLayer` shader (`_Dry01`, `_Specular`, `_SeeThrough`) plus `InkDryingLayerDriver`: on dry start the film stays see-through for `seeThroughDrySeconds` (30), then opaque. `InkDryingNarrativeBridge` enqueues calendar events `ink-dry-start` / `ink-dry-opaque` with tags `ink`, `dry`.

## Lemmas

Placeholders: `pen`, `quill`, `nib`, `ink`, `write`, `dip`, `cap`, `open`, `close`, `wet`, `dry`, `paint`, `towel`, `whiteboard`.

Properties: `paintlike`, `dilution`, `single-layer-mix`, `max-bend-deg`, `see-through-sec`, `aperture`, `cap-open`.

`PenInkLemmaResolver` lives in locomotion (no `Locomotion.Open.Runtime` reference). Cap open/close uses `SendMessage("OnPenCapOpen")`. Studio bakes cap topology via `OpenCloseTopologyCompiler` (editor only).

Wet IK goal: hydro film centroid → `PaintPileLiquidDriver.pileCenter` → drink stream tip / `loopPourActive` nozzle. Dry: towel transform or canvas; blot via `PaintSmudgeCollider`.

IK catalog ids: `pen_dip`, `ink_stroke`, `cap_open`, `cap_close`, `blot_dry`.

Drawing target: text via `FontFamilyGlyphMesher` + `GlyphSdfMaxComposer`. Unknown code points → box `U+25A1` / replacement `U+FFFD`. Image OCR is feature-flagged (same scaffold as sheet-music OCR) and emits a box glyph until a real hop exists. **`understandingConfirmed` is required** before IK train.

## Break and splatter

Break analysis **always** runs on contact (settings are thresholds only). `InkNibBreakAnalyzer` samples the nib with `IntegralConvexTreeSolver` leaves (SdfMax — not Destructible). Stress = `QuillNibDefinition.Stress01(bend, force, breakForce)`. Requested page bend is clamped to 10° for pose; over-limit still feeds stress so a jammed nib can snap.

If stress ≥ 1: pick a break leaf, spawn debris `Rigidbody` + mesh chunk, replace the remaining nib, expand nozzle aperture (`PenInkInstrument.ExpandAperture`) so the reservoir spills.

Regardless of break: `InkSphContactSplatter` seeds `PaintCanvasHydroSolver.SeedFromStamp` and `PaintTransferDecal.TryApply` at contact, using the current drying layer shader when present.

## Hydro ridge force → nib

Off by default so paint scenes stay one-way (stamp seeds SPH; SPH does not push the instrument). Enable **Hydro ridge force → nib** in Pen and Ink Studio (or `PaintCanvasHydroSolver.feedRidgeForceToNib` + `nibFeedbackTarget`).

When on, each hydro step samples density and ∇ρ at the tip: film **pressure** along the canvas normal plus a **ridge** term from the density gradient. That force is `AddForce` on a tip/instrument `Rigidbody` (if present) and `ContactCanvas(..., splatter: false)` so nib-break stress can rise without re-seeding particles every frame. Stamp still splatters as before.
