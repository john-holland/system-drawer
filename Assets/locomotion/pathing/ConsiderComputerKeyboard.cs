using System.Collections.Generic;
using UnityEngine;
using Locomotion.Musculature;

/// <summary>Consider cards for computer keyboard: approach → finger press → release; volume rotate; jump-press.</summary>
[AddComponentMenu("Locomotion/Periphery/Consider Computer Keyboard")]
public sealed class ConsiderComputerKeyboard : MonoBehaviour
{
    public ComputerKeyboardRuntime keyboard;
    public KeyboardMessagePump pump;
    public FingerPositionCache fingerCache;
    public PeripheralJumpPress jumpPress;
    public ComputerPeripheryStation periphery;
    public float pressRadius = 0.04f;

    public bool requestPlaceBuildFallback;

    readonly List<GoodSection> _cards = new List<GoodSection>();

    public List<GoodSection> GenerateCardsForNextStroke(RagdollState state)
    {
        _cards.Clear();
        requestPlaceBuildFallback = false;
        if (periphery != null && !periphery.toolUseGate.AllowsToolUse())
            return _cards;
        if (pump == null || !pump.TryDequeue(out KeyStroke stroke))
            return _cards;

        ComputerKey key = stroke.key;
        if (key == null && keyboard != null)
            keyboard.TryGetKey(stroke.id, out key);
        if (key == null)
            return _cards;

        keyboard?.RecalculateCentroid();
        var side = KeyboardHandPicker.PickHand(key.WorldPressPoint, keyboard != null ? keyboard.worldCentroid : key.WorldPressPoint);
        var finger = KeyboardHandPicker.PreferFinger(key.id);

        if (fingerCache != null && fingerCache.IsOverKey(key, side, finger))
        {
            _cards.Add(BuildPressCard(key, "cached_press", 0.05f));
            fingerCache.Remember(side, finger, key.WorldPressPoint, key.id);
            return _cards;
        }

        _cards.Add(BuildApproachCard(key));
        _cards.Add(BuildPressCard(key, "finger_press", 0.12f));
        _cards.Add(BuildReleaseCard(key));
        return _cards;
    }

    public bool TryJumpPressStroke(ComputerKey key, float impulse01, RagdollSystem ragdoll)
    {
        if (jumpPress == null)
            jumpPress = GetComponent<PeripheralJumpPress>();
        if (jumpPress == null || key == null)
            return false;
        var side = KeyboardHandPicker.PickHand(key.WorldPressPoint, keyboard != null ? keyboard.worldCentroid : key.WorldPressPoint);
        var finger = KeyboardHandPicker.PreferFinger(key.id);
        bool ok = jumpPress.TryJumpPress(key, impulse01, side, finger, out bool needBuild);
        requestPlaceBuildFallback = needBuild;
        if (ok && fingerCache != null)
            fingerCache.Remember(side, finger, key.WorldPressPoint, key.id);
        return ok;
    }

    public GoodSection GenerateRotateKnobCard(VolumeKnobRuntime knob)
    {
        if (knob == null) return null;
        return new GoodSection
        {
            sectionName = "RotateVolumeKnob",
            description = "Rotate volume knob with hemispherical grasp",
            isSitGoal = false,
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "Hand", activation = 0.7f, duration = 0.2f },
                new ImpulseAction { muscleGroup = "Arm", activation = 0.5f, duration = 0.25f, torqueDirection = Vector3.up }
            }
        };
    }

    static GoodSection BuildApproachCard(ComputerKey key)
    {
        return new GoodSection
        {
            sectionName = "ApproachKey_" + key.id,
            description = "Approach " + key.legend,
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "Arm", activation = 0.55f, duration = 0.15f, forceDirection = (key.WorldPressPoint).normalized }
            }
        };
    }

    static GoodSection BuildPressCard(ComputerKey key, string name, float duration)
    {
        return new GoodSection
        {
            sectionName = name + "_" + key.id,
            description = "Press " + key.legend,
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction
                {
                    muscleGroup = "Hand",
                    activation = Mathf.Max(0.4f, key.minPressImpulse),
                    duration = duration,
                    forceDirection = key.WorldTravelAxis
                }
            }
        };
    }

    static GoodSection BuildReleaseCard(ComputerKey key)
    {
        return new GoodSection
        {
            sectionName = "ReleaseKey_" + key.id,
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "Hand", activation = 0.2f, duration = 0.08f, forceDirection = -key.WorldTravelAxis }
            }
        };
    }
}
