# Computer Keyboard Layout

Procedural PC keyboard under `ComputerKeyboardRuntime` / `ComputerKeyboardBuilder`.

## Spec

Base width/height/depth, bevels, slant, chiclet defaults, volume knob, aux count (default 3), render mode SimpleMesh | SdfMax.

Travel band: `ComputeTravelBand(row/7)` → `[min(base0, travel), clearance]`.

## Rows / sections

See `ComputerKeyboardLayout.BuildDefault`: Esc+F1–12 groups, Print/Scroll/Pause, Aux+Volume; number/nav/numpad; QWERTY; Caps; Shift; Ctrl/Cmd/Alt/Space/Fn/Option; arrows; numpad spans for `+`, `0`, Enter.

Option opens context menu (`ComputerKey.opensContextMenu`).
