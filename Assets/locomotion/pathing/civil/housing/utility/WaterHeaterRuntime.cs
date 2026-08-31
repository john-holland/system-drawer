using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Water Heater")]
public sealed class WaterHeaterRuntime : MonoBehaviour
{
    public BuildingPlumbingGroup plumbing;
    public UtilityBioRhythm utilityBio;
    public BuildingWaterShutoff shutoff;
    public bool running = true;
    [Range(0f, 1f)] public float tankTemp01 = 0.7f;
    [Range(0f, 1f)] public float leak01;

    public void Tick(float dt)
    {
        if (plumbing == null)
            plumbing = GetComponentInParent<BuildingPlumbingGroup>();
        float feed = shutoff != null && !shutoff.open ? 0f : 1f;
        if (plumbing != null)
            plumbing.heaterHot01 = running ? tankTemp01 * feed : 0f;
        if (utilityBio != null && leak01 > 0.01f)
            utilityBio.standingLiters += leak01 * dt * 4f;
    }

    public void SetRunning(bool on) => running = on;
}
