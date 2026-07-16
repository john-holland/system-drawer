using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Jump-to-press: strong enough muscle impulse + finger cache over button completes Consider press.
/// After maxJumpPressAttempts (default 5) failures, requests place/build grabbable fallback.
/// </summary>
[AddComponentMenu("Locomotion/Periphery/Peripheral Jump Press")]
public sealed class PeripheralJumpPress : MonoBehaviour
{
    public int maxJumpPressAttempts = 5;
    public FingerPositionCache fingerCache;
    public ComputerKeyboardRuntime keyboard;
    public float defaultMinImpulse = 0.55f;

    public int FailedAttempts { get; private set; }
    public bool ShouldFallbackToPlaceBuild => FailedAttempts >= maxJumpPressAttempts;

    public void ResetAttempts() => FailedAttempts = 0;

    /// <summary>
    /// Try jump press. Returns true if press completed. impulse01 is muscle strength 0-1.
    /// </summary>
    public bool TryJumpPress(ComputerKey key, float impulse01, KeyboardHandPicker.HandSide side, FingerKind kind, out bool needPlaceBuild)
    {
        needPlaceBuild = false;
        if (key == null)
        {
            FailedAttempts++;
            needPlaceBuild = ShouldFallbackToPlaceBuild;
            return false;
        }

        float need = key.minPressImpulse > 0f ? key.minPressImpulse : defaultMinImpulse;
        bool over = fingerCache != null && fingerCache.IsOverKey(key, side, kind);
        if (!over && fingerCache == null)
            over = true; // allow strength-only when no cache yet, first contact establishes cache

        if (impulse01 >= need && over)
        {
            key.ApplyPressDepth(1f);
            fingerCache?.Remember(side, kind, key.WorldPressPoint, key.id);
            FailedAttempts = 0;
            return true;
        }

        FailedAttempts++;
        needPlaceBuild = ShouldFallbackToPlaceBuild;
        return false;
    }
}
