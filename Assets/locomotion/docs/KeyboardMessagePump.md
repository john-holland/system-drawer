# Keyboard Message Pump

`KeyboardMessagePump`: `EnqueueText`, `EnqueueKeys`, `EnqueueFromGameObject`, `TryDequeue`.

`ConsiderComputerKeyboard` consumes strokes → approach/press/release cards when `PeripheryToolUseGate` is open.

**FingerPositionCache** short-circuits repeat presses. **PeripheralJumpPress**: impulse ≥ threshold + cache over key → press; after **5** failed attempts → `requestPlaceBuildFallback` for grabbable place/build.
