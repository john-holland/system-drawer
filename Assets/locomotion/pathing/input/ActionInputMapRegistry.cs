using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ActionInputBinding
{
    public string actionId;
    public List<ControlBinding> controls = new List<ControlBinding>();
}

/// <summary>
/// Shared action → control map painted by {P:action|maps-to=...} lemmas.
/// Consumers poll IsPressed / WasPressedThisFrame / WasReleasedThisFrame / GetAxis.
/// </summary>
[AddComponentMenu("Locomotion/Input/Action Input Map Registry")]
public sealed class ActionInputMapRegistry : MonoBehaviour
{
    public List<ActionInputBinding> bindings = new List<ActionInputBinding>();

    static readonly List<ActionInputMapRegistry> s_live = new List<ActionInputMapRegistry>(4);
    static readonly HashSet<string> s_loggedMissing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void OnEnable()
    {
        if (!s_live.Contains(this))
            s_live.Add(this);
    }

    void OnDisable() => s_live.Remove(this);

    public static ActionInputMapRegistry FindActive()
    {
        for (int i = s_live.Count - 1; i >= 0; i--)
        {
            if (s_live[i] != null && s_live[i].isActiveAndEnabled)
                return s_live[i];
            s_live.RemoveAt(i);
        }
        return UnityEngine.Object.FindAnyObjectByType<ActionInputMapRegistry>();
    }

    public static ActionInputMapRegistry FindOrCreate()
    {
        var existing = FindActive();
        if (existing != null) return existing;
        var go = new GameObject("ActionInputMapRegistry");
        return go.AddComponent<ActionInputMapRegistry>();
    }

    public bool TryGet(string actionId, out ActionInputBinding binding)
    {
        binding = null;
        if (string.IsNullOrEmpty(actionId) || bindings == null) return false;
        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b == null || string.IsNullOrEmpty(b.actionId)) continue;
            if (string.Equals(b.actionId, actionId, StringComparison.OrdinalIgnoreCase))
            {
                binding = b;
                return true;
            }
        }
        return false;
    }

    public ActionInputBinding GetOrCreate(string actionId)
    {
        if (TryGet(actionId, out var existing))
            return existing;
        var b = new ActionInputBinding
        {
            actionId = actionId,
            controls = new List<ControlBinding>()
        };
        if (bindings == null) bindings = new List<ActionInputBinding>();
        bindings.Add(b);
        return b;
    }

    public void ApplyLemma(ActionInputLemmaProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.actionId))
            return;

        var entry = GetOrCreate(props.actionId.Trim());
        if (props.clear && entry.controls != null)
            entry.controls.Clear();
        if (entry.controls == null)
            entry.controls = new List<ControlBinding>();

        TryAddToken(entry, props.mapsTo, props.subscribe);
        TryAddToken(entry, props.andMapsTo, props.subscribe);
    }

    void TryAddToken(ActionInputBinding entry, string token, ActionInputSubscribeMode subscribe)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        if (!ControlTokenResolver.TryParse(token, subscribe, out var control) || control == null || !control.IsValid)
            return;
        // Avoid exact duplicates
        for (int i = 0; i < entry.controls.Count; i++)
        {
            var c = entry.controls[i];
            if (c == null) continue;
            if (c.kind == control.kind &&
                c.keyCode == control.keyCode &&
                c.mouseButton == control.mouseButton &&
                string.Equals(c.axisName, control.axisName, StringComparison.OrdinalIgnoreCase) &&
                c.subscribe == control.subscribe)
                return;
        }
        entry.controls.Add(control);
    }

    public bool IsPressed(string actionId)
    {
        if (!TryGet(actionId, out var b) || b.controls == null) return false;
        for (int i = 0; i < b.controls.Count; i++)
        {
            var c = b.controls[i];
            if (c == null || !c.IsValid) continue;
            if (c.kind == ControlBindingKind.Axis)
            {
                if (Mathf.Abs(Input.GetAxisRaw(c.axisName)) > 0.1f) return true;
                continue;
            }
            if (EvalHeld(c)) return true;
        }
        return false;
    }

    public bool WasPressedThisFrame(string actionId)
    {
        if (!TryGet(actionId, out var b) || b.controls == null) return false;
        for (int i = 0; i < b.controls.Count; i++)
        {
            var c = b.controls[i];
            if (c == null || !c.IsValid || c.kind == ControlBindingKind.Axis) continue;
            ActionInputSubscribeMode mode = c.subscribe;
            if (mode == ActionInputSubscribeMode.KeyUp) continue;
            if (mode == ActionInputSubscribeMode.Held)
            {
                if (EvalHeld(c)) return true;
                continue;
            }
            // KeyDown default
            if (EvalDown(c)) return true;
        }
        return false;
    }

    public bool WasReleasedThisFrame(string actionId)
    {
        if (!TryGet(actionId, out var b) || b.controls == null) return false;
        for (int i = 0; i < b.controls.Count; i++)
        {
            var c = b.controls[i];
            if (c == null || !c.IsValid || c.kind == ControlBindingKind.Axis) continue;
            // Prefer KeyUp-subscribed controls; still allow release poll on any digital bind
            if (c.subscribe == ActionInputSubscribeMode.KeyUp ||
                c.subscribe == ActionInputSubscribeMode.KeyDown ||
                c.subscribe == ActionInputSubscribeMode.Held)
            {
                if (EvalUp(c)) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True when the action fires this frame according to each control's subscribe mode
    /// (KEY_DOWN → down edge, KEY_UP → up edge, KEY_HELD → held, AXIS → |axis|&gt;deadzone).
    /// </summary>
    public bool FiredThisFrame(string actionId)
    {
        if (!TryGet(actionId, out var b) || b.controls == null)
        {
            if (s_loggedMissing.Add(actionId ?? ""))
                Debug.LogWarning($"[ActionInputMapRegistry] No bindings for action '{actionId}'.");
            return false;
        }
        for (int i = 0; i < b.controls.Count; i++)
        {
            var c = b.controls[i];
            if (c == null || !c.IsValid) continue;
            switch (c.subscribe)
            {
                case ActionInputSubscribeMode.KeyUp:
                    if (EvalUp(c)) return true;
                    break;
                case ActionInputSubscribeMode.Held:
                    if (EvalHeld(c)) return true;
                    break;
                case ActionInputSubscribeMode.Axis:
                    if (c.kind == ControlBindingKind.Axis &&
                        Mathf.Abs(Input.GetAxisRaw(c.axisName)) > 0.1f)
                        return true;
                    break;
                default:
                    if (EvalDown(c)) return true;
                    break;
            }
        }
        return false;
    }

    public float GetAxis(string actionId)
    {
        if (!TryGet(actionId, out var b) || b.controls == null) return 0f;
        float sum = 0f;
        int n = 0;
        for (int i = 0; i < b.controls.Count; i++)
        {
            var c = b.controls[i];
            if (c == null || c.kind != ControlBindingKind.Axis || string.IsNullOrEmpty(c.axisName))
                continue;
            sum += Input.GetAxisRaw(c.axisName);
            n++;
        }
        return n > 0 ? sum / n : 0f;
    }

    static bool EvalHeld(ControlBinding c)
    {
        if (c.kind == ControlBindingKind.Key)
            return Input.GetKey(c.keyCode);
        if (c.kind == ControlBindingKind.MouseButton)
            return Input.GetMouseButton(c.mouseButton);
        return false;
    }

    static bool EvalDown(ControlBinding c)
    {
        if (c.kind == ControlBindingKind.Key)
            return Input.GetKeyDown(c.keyCode);
        if (c.kind == ControlBindingKind.MouseButton)
            return Input.GetMouseButtonDown(c.mouseButton);
        return false;
    }

    static bool EvalUp(ControlBinding c)
    {
        if (c.kind == ControlBindingKind.Key)
            return Input.GetKeyUp(c.keyCode);
        if (c.kind == ControlBindingKind.MouseButton)
            return Input.GetMouseButtonUp(c.mouseButton);
        return false;
    }
}
