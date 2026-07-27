# Action input / keymap lemmas

Continuuuum spans that paint a shared [`ActionInputMapRegistry`](../pathing/input/ActionInputMapRegistry.cs) with symbolic control tokens. Consumers poll the registry instead of hard-coded `KeyCode`s.

## Grammar

Canonical form (`{P:...}`):

```text
{P:action|id=jump|maps-to=x}
{P:action|id=strafe|maps-to=X_AXIS}
{P:action|id=fire|subscribe=KEY_UP|maps-to=MOUSE_0}
{P:action|id=fire|subscribe=KEY_UP|maps-to=MOUSE_0|and-maps-to=MOUSE_1}
{P:keymap|action=jump|to=Space}
```

Author-facing prose (“action maps to 'x'”, “action subscribes 'KEY_UP' and maps to 'MOUSE_0'”) documents intent; runtime paint uses the spans above.

Placeholders: `action`, `keymap`, `maps`.

| Key | Aliases | Role |
|-----|---------|------|
| `id` | `action` | Action id string |
| `maps-to` | `to`, `map` | Control token |
| `subscribe` | `edge`, `on` | `KEY_DOWN` (default) / `KEY_UP` / `KEY_HELD` / `AXIS` |
| `and-maps-to` | `also` | Extra OR-bound token |
| `clear` | — | Clear existing binds for this action before apply |

## Control tokens

| Family | Examples | Resolves to |
|--------|----------|-------------|
| Letter / KeyCode | `x`, `Space`, `LeftShift` | `KeyCode` |
| Arrows | `KEY_UP`, `KEY_DOWN`, `KEY_LEFT`, `KEY_RIGHT` | Arrow keys |
| Mouse | `MOUSE_0`, `MOUSE_1`, `MOUSE_2` | Mouse button index |
| Axis | `X_AXIS`, `Y_AXIS`, `Horizontal`, `Mouse X` | `Input.GetAxis` name |
| Joystick | `JOY_0` … | `JoystickButton0` … |

Unknown tokens log once and skip.

## Subscribe semantics

| Mode | Poll |
|------|------|
| `KEY_DOWN` | `GetKeyDown` / `GetMouseButtonDown` via `FiredThisFrame` / `WasPressedThisFrame` |
| `KEY_UP` | `GetKeyUp` / `GetMouseButtonUp` via `FiredThisFrame` / `WasReleasedThisFrame` |
| `KEY_HELD` | `GetKey` / `GetMouseButton` |
| `AXIS` | `GetAxisRaw` (forced for axis tokens) |

## Runtime

- [`ControlTokenResolver`](../pathing/input/ControlTokenResolver.cs) — token parse
- [`ActionInputMapRegistry`](../pathing/input/ActionInputMapRegistry.cs) — bind table + poll API
- [`ActionInputLemmaResolver.ApplyFromPrompt`](../pathing/input/ActionInputLemmaResolver.cs) — walk prompt spans
- [`ActionInputLemmaApplier`](../pathing/input/ActionInputLemmaResolver.cs) — MB with serialized `lemmaPrompt` applied on enable

Example consumer:

```csharp
var map = ActionInputMapRegistry.FindActive();
if (map != null && map.FiredThisFrame("fire"))
    /* handle fire */;
float strafe = map != null ? map.GetAxis("strafe") : 0f;
```

## Out of scope (follow-up)

- Unity Input System `.inputed` migration
- Rewiring `WaypointPlannerInput` / player movement to this map
- Mutating `WrestlingMoveInputBindings` SO from lemmas
