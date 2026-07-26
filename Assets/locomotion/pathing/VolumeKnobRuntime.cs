using System.Reflection;
using UnityEngine;

/// <summary>Volume knob with discrete click travel.</summary>
/// <remarks>
/// OpenableJointDriver lives in Locomotion.Open (which references this assembly), so the
/// joint is a MonoBehaviour soft-ref and SetOpen01 is invoked by name to avoid a cycle.
/// </remarks>
[AddComponentMenu("Locomotion/Periphery/Volume Knob")]
public sealed class VolumeKnobRuntime : MonoBehaviour
{
    public float height = 0.018f;
    public float radius = 0.01f;
    public float topBevel = 0.002f;
    public float clearance = 0.002f;
    public float travel = 0.004f;
    public int clickCount = 12;
    public Light knobLight;
    [Tooltip("Optional OpenableJointDriver (Locomotion.Open) — assigned in inspector.")]
    public MonoBehaviour jointDriver;

    public int CurrentClick { get; private set; }

    public void SetClick(int click)
    {
        CurrentClick = Mathf.Clamp(click, 0, Mathf.Max(0, clickCount - 1));
        float t = clickCount <= 1 ? 0f : CurrentClick / (float)(clickCount - 1);
        TrySetOpen01(jointDriver, t);
        transform.localRotation = Quaternion.Euler(0f, t * 270f, 0f);
        if (knobLight != null)
            knobLight.intensity = 0.2f + t * 0.8f;
    }

    public void Nudge(int delta) => SetClick(CurrentClick + delta);

    static void TrySetOpen01(MonoBehaviour driver, float open01)
    {
        if (driver == null) return;
        MethodInfo mi = driver.GetType().GetMethod(
            "SetOpen01",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(float) },
            null);
        if (mi != null)
            mi.Invoke(driver, new object[] { open01 });
    }
}
