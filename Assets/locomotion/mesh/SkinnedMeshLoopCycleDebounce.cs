using UnityEngine;

/// <summary>
/// Freezes hover raycast / zone rebuild after Shift+wheel until timeout or cursor moves past a pixel threshold.
/// </summary>
public sealed class SkinnedMeshLoopCycleDebounce
{
    public const float PixelThreshold = 12f;
    public const float TimeoutSeconds = 2f;

    public Vector2 origin;
    public double lastWheelTime;
    public bool active;

    public void Begin(Vector2 mouse, double time)
    {
        origin = mouse;
        lastWheelTime = time;
        active = true;
    }

    public void NoteWheel(double time)
    {
        lastWheelTime = time;
        active = true;
    }

    public bool ShouldFreezeHover(Vector2 mouse, double time)
    {
        if (!active)
            return false;
        if (time - lastWheelTime >= TimeoutSeconds)
        {
            active = false;
            return false;
        }
        if ((mouse - origin).sqrMagnitude >= PixelThreshold * PixelThreshold)
        {
            active = false;
            return false;
        }
        return true;
    }
}
