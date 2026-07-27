using System;
using System.Collections.Generic;
using UnityEngine;

public enum ControlBindingKind
{
    Key,
    MouseButton,
    Axis
}

[Serializable]
public sealed class ControlBinding
{
    public ControlBindingKind kind;
    public KeyCode keyCode = KeyCode.None;
    public int mouseButton = -1;
    public string axisName = "";
    public ActionInputSubscribeMode subscribe = ActionInputSubscribeMode.KeyDown;
    public string sourceToken = "";

    public bool IsValid =>
        kind == ControlBindingKind.Key ? keyCode != KeyCode.None :
        kind == ControlBindingKind.MouseButton ? mouseButton >= 0 :
        !string.IsNullOrEmpty(axisName);
}

/// <summary>Parses symbolic control tokens (KEY_UP, MOUSE_0, X_AXIS, KeyCode names) into ControlBinding.</summary>
public static class ControlTokenResolver
{
    static readonly HashSet<string> s_loggedUnknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static bool TryParse(string token, ActionInputSubscribeMode subscribe, out ControlBinding binding)
    {
        binding = null;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        string raw = token.Trim().Trim('\'', '"');
        string n = ActionInputLemmaProperties.NormalizeToken(raw);

        // Mouse buttons
        if (n.StartsWith("MOUSE_", StringComparison.Ordinal) ||
            n.StartsWith("MOUSEBUTTON", StringComparison.Ordinal))
        {
            string num = n.StartsWith("MOUSE_", StringComparison.Ordinal)
                ? n.Substring("MOUSE_".Length)
                : n.Substring("MOUSEBUTTON".Length);
            if (int.TryParse(num, out int btn) && btn >= 0 && btn <= 6)
            {
                binding = new ControlBinding
                {
                    kind = ControlBindingKind.MouseButton,
                    mouseButton = btn,
                    subscribe = subscribe == ActionInputSubscribeMode.Axis
                        ? ActionInputSubscribeMode.KeyDown
                        : subscribe,
                    sourceToken = raw
                };
                return true;
            }
        }

        // Axes
        if (TryResolveAxis(n, raw, out string axisName))
        {
            binding = new ControlBinding
            {
                kind = ControlBindingKind.Axis,
                axisName = axisName,
                subscribe = ActionInputSubscribeMode.Axis,
                sourceToken = raw
            };
            return true;
        }

        // Arrow / special KEY_* aliases (as key targets, not subscribe modes)
        if (TryResolveKeyAlias(n, out KeyCode aliasKey))
        {
            binding = new ControlBinding
            {
                kind = ControlBindingKind.Key,
                keyCode = aliasKey,
                subscribe = subscribe == ActionInputSubscribeMode.Axis
                    ? ActionInputSubscribeMode.KeyDown
                    : subscribe,
                sourceToken = raw
            };
            return true;
        }

        // Joystick buttons JOY_0 …
        if (n.StartsWith("JOY_", StringComparison.Ordinal) ||
            n.StartsWith("JOYSTICKBUTTON", StringComparison.Ordinal))
        {
            string num = n.StartsWith("JOY_", StringComparison.Ordinal)
                ? n.Substring("JOY_".Length)
                : n.Substring("JOYSTICKBUTTON".Length);
            if (int.TryParse(num, out int j) && j >= 0 && j <= 19)
            {
                binding = new ControlBinding
                {
                    kind = ControlBindingKind.Key,
                    keyCode = KeyCode.JoystickButton0 + j,
                    subscribe = subscribe == ActionInputSubscribeMode.Axis
                        ? ActionInputSubscribeMode.KeyDown
                        : subscribe,
                    sourceToken = raw
                };
                return true;
            }
        }

        // Single letter or KeyCode enum name
        if (TryParseKeyCode(raw, n, out KeyCode kc))
        {
            binding = new ControlBinding
            {
                kind = ControlBindingKind.Key,
                keyCode = kc,
                subscribe = subscribe == ActionInputSubscribeMode.Axis
                    ? ActionInputSubscribeMode.KeyDown
                    : subscribe,
                sourceToken = raw
            };
            return true;
        }

        if (s_loggedUnknown.Add(raw))
            Debug.LogWarning($"[ControlTokenResolver] Unknown control token '{raw}' — binding skipped.");
        return false;
    }

    static bool TryResolveAxis(string n, string raw, out string axisName)
    {
        axisName = null;
        switch (n)
        {
            case "X_AXIS":
            case "XAXIS":
                axisName = "Horizontal";
                return true;
            case "Y_AXIS":
            case "YAXIS":
                axisName = "Vertical";
                return true;
            case "HORIZONTAL":
                axisName = "Horizontal";
                return true;
            case "VERTICAL":
                axisName = "Vertical";
                return true;
            case "MOUSE_X":
            case "MOUSEX":
                axisName = "Mouse X";
                return true;
            case "MOUSE_Y":
            case "MOUSEY":
                axisName = "Mouse Y";
                return true;
            case "MOUSE_SCROLL_WHEEL":
            case "MOUSESCROLLWHEEL":
            case "SCROLL":
                axisName = "Mouse ScrollWheel";
                return true;
        }

        // Allow raw Unity axis names that contain spaces (preserved from original)
        if (raw.IndexOf(' ') >= 0 ||
            string.Equals(raw, "Horizontal", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Vertical", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Jump", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Fire1", StringComparison.OrdinalIgnoreCase))
        {
            axisName = raw.Trim();
            return true;
        }

        return false;
    }

    static bool TryResolveKeyAlias(string n, out KeyCode key)
    {
        key = KeyCode.None;
        switch (n)
        {
            case "KEY_UP":
            case "ARROW_UP":
            case "UPARROW":
                key = KeyCode.UpArrow;
                return true;
            case "KEY_DOWN":
            case "ARROW_DOWN":
            case "DOWNARROW":
                // Ambiguous with subscribe KEY_DOWN when used as maps-to — prefer arrow
                key = KeyCode.DownArrow;
                return true;
            case "KEY_LEFT":
            case "ARROW_LEFT":
            case "LEFTARROW":
                key = KeyCode.LeftArrow;
                return true;
            case "KEY_RIGHT":
            case "ARROW_RIGHT":
            case "RIGHTARROW":
                key = KeyCode.RightArrow;
                return true;
            case "RETURN":
            case "ENTER":
                key = KeyCode.Return;
                return true;
            case "ESC":
            case "ESCAPE":
                key = KeyCode.Escape;
                return true;
            default:
                return false;
        }
    }

    static bool TryParseKeyCode(string raw, string normalized, out KeyCode key)
    {
        key = KeyCode.None;
        // Single letter a–z
        if (raw.Length == 1)
        {
            char c = char.ToLowerInvariant(raw[0]);
            if (c >= 'a' && c <= 'z')
            {
                key = (KeyCode)((int)KeyCode.A + (c - 'a'));
                return true;
            }
            if (c >= '0' && c <= '9')
            {
                key = (KeyCode)((int)KeyCode.Alpha0 + (c - '0'));
                return true;
            }
        }

        // KeyCode enum by name (Space, LeftShift, Alpha1, …)
        if (Enum.TryParse(raw, true, out KeyCode parsed) && parsed != KeyCode.None)
        {
            key = parsed;
            return true;
        }

        // Normalized form without underscores for names like LEFT_SHIFT
        string pascal = normalized.Replace("_", "");
        if (Enum.TryParse(pascal, true, out parsed) && parsed != KeyCode.None)
        {
            key = parsed;
            return true;
        }

        return false;
    }
}
