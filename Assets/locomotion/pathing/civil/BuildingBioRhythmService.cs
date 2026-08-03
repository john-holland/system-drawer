using UnityEngine;

/// <summary>BuildingRagdoll bio channels — integrity, occupancy, commodity hunger, exterior pressure.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Building Bio Rhythm")]
public sealed class BuildingBioRhythmService : MonoBehaviour
{
    public BuildingHealthState health = new BuildingHealthState();
    public bool isOpen;
    [Range(0f, 1f)] public float occupancySetpoint01 = 0.35f;
    [Range(0f, 1f)] public float commoditySetpoint01 = 0.4f;
    public float occupancyPullPerSec = 0.05f;

    public void NotifyOpen()
    {
        isOpen = true;
    }

    public void NotifyClosed()
    {
        isOpen = false;
    }

    public void ApplyPersonaSeed(float amplitude01)
    {
        occupancySetpoint01 = Mathf.Clamp01(0.2f + amplitude01 * 0.5f);
    }

    public void Tick(float dt)
    {
        if (health == null) health = new BuildingHealthState();
        float targetOcc = isOpen ? occupancySetpoint01 : 0f;
        health.occupancyLoad01 = Mathf.MoveTowards(health.occupancyLoad01, targetOcc, occupancyPullPerSec * dt);
        float targetHunger = isOpen ? commoditySetpoint01 : 0.1f;
        health.commodityHunger01 = Mathf.MoveTowards(health.commodityHunger01, targetHunger, 0.03f * dt);
        health.TickDecay(dt);
    }
}
