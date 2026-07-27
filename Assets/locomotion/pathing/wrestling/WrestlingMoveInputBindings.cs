using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WrestlingMoveBinding
{
    public WrestlingMoveKind kind;
    public string inputActionName;
    public KeyCode fallbackKey = KeyCode.None;
}

/// <summary>Button → WrestlingMoveKind map per mode (unscaled-time hotkeys).</summary>
[CreateAssetMenu(fileName = "WrestlingMoveInputBindings", menuName = "Locomotion/Wrestling/Move Input Bindings")]
public sealed class WrestlingMoveInputBindings : ScriptableObject
{
    public List<WrestlingMoveBinding> playDefaults = new List<WrestlingMoveBinding>
    {
        new WrestlingMoveBinding { kind = WrestlingMoveKind.LockGrapple, fallbackKey = KeyCode.JoystickButton2, inputActionName = "Wrestling/Lock" },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Lift, fallbackKey = KeyCode.JoystickButton3, inputActionName = "Wrestling/Lift" },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Counter, fallbackKey = KeyCode.JoystickButton0, inputActionName = "Wrestling/Counter" },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Throw, fallbackKey = KeyCode.JoystickButton1, inputActionName = "Wrestling/Throw" },
    };

    public List<WrestlingMoveBinding> subdueDefaults = new List<WrestlingMoveBinding>
    {
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Pull, fallbackKey = KeyCode.JoystickButton2 },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Push, fallbackKey = KeyCode.JoystickButton1 },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.LockGrapple, fallbackKey = KeyCode.JoystickButton3 },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Block, fallbackKey = KeyCode.JoystickButton0 },
    };

    public List<WrestlingMoveBinding> pinDefaults = new List<WrestlingMoveBinding>
    {
        new WrestlingMoveBinding { kind = WrestlingMoveKind.LockGrapple, fallbackKey = KeyCode.JoystickButton2 },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.DropOn, fallbackKey = KeyCode.JoystickButton1 },
        new WrestlingMoveBinding { kind = WrestlingMoveKind.Push, fallbackKey = KeyCode.JoystickButton0 },
    };

    public IList<WrestlingMoveBinding> ForMode(WrestlingMode mode)
    {
        switch (mode)
        {
            case WrestlingMode.Subdue: return subdueDefaults;
            case WrestlingMode.Pin: return pinDefaults;
            default: return playDefaults;
        }
    }

    public bool TryPollPressed(WrestlingMode mode, out WrestlingMoveKind kind)
    {
        kind = default;
        var list = ForMode(mode);
        if (list == null) return false;
        for (int i = 0; i < list.Count; i++)
        {
            var b = list[i];
            if (b == null || b.fallbackKey == KeyCode.None) continue;
            if (Input.GetKeyDown(b.fallbackKey))
            {
                kind = b.kind;
                return true;
            }
        }
        return false;
    }
}
