using UnityEngine;
using Locomotion.Musculature;

/// <summary>Binary-tree hand selection about keyboard centroid + finger preference.</summary>
public static class KeyboardHandPicker
{
    public enum HandSide { Left, Right }

    public static HandSide PickHand(Vector3 keyWorld, Vector3 keyboardCentroid, HandSide? sticky = null)
    {
        // Project onto keyboard lateral axis (world X relative to centroid).
        float dx = keyWorld.x - keyboardCentroid.x;
        if (Mathf.Abs(dx) < 0.01f && sticky.HasValue)
            return sticky.Value;
        return dx < 0f ? HandSide.Left : HandSide.Right;
    }

    public static FingerKind PreferFinger(ComputerKeyId id)
    {
        switch (id)
        {
            case ComputerKeyId.Space:
            case ComputerKeyId.LeftAlt:
            case ComputerKeyId.RightAlt:
                return FingerKind.Thumb;
            case ComputerKeyId.LeftShift:
            case ComputerKeyId.RightShift:
            case ComputerKeyId.LeftControl:
            case ComputerKeyId.RightControl:
            case ComputerKeyId.CapsLock:
            case ComputerKeyId.Tab:
                return FingerKind.Pinky;
            default:
                return FingerKind.Index;
        }
    }

    public static bool TryResolveFinger(RagdollSystem ragdoll, HandSide side, FingerKind kind, out Transform tip)
    {
        tip = null;
        if (ragdoll == null) return false;
        string handName = side == HandSide.Left ? "LeftHand" : "RightHand";
        Transform handTf = ragdoll.GetBoneTransform(handName);
        if (handTf == null) return false;
        var hand = handTf.GetComponentInChildren<RagdollHand>();
        if (hand == null || hand.fingers == null)
        {
            tip = handTf;
            return true;
        }
        for (int i = 0; i < hand.fingers.Count; i++)
        {
            var f = hand.fingers[i];
            if (f == null || f.kind != kind) continue;
            if (f.digits != null && f.digits.Count > 0 && f.digits[f.digits.Count - 1] != null)
            {
                tip = f.digits[f.digits.Count - 1].transform;
                return tip != null;
            }
            tip = f.transform;
            return tip != null;
        }
        tip = handTf;
        return true;
    }
}
