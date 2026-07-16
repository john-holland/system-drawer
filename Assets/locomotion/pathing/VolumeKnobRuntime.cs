using UnityEngine;
using Locomotion.Open;

/// <summary>Volume knob with discrete click travel.</summary>
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
    public OpenableJointDriver jointDriver;

    public int CurrentClick { get; private set; }

    public void SetClick(int click)
    {
        CurrentClick = Mathf.Clamp(click, 0, Mathf.Max(0, clickCount - 1));
        float t = clickCount <= 1 ? 0f : CurrentClick / (float)(clickCount - 1);
        if (jointDriver != null)
            jointDriver.SetOpen01(t);
        transform.localRotation = Quaternion.Euler(0f, t * 270f, 0f);
        if (knobLight != null)
            knobLight.intensity = 0.2f + t * 0.8f;
    }

    public void Nudge(int delta) => SetClick(CurrentClick + delta);
}
