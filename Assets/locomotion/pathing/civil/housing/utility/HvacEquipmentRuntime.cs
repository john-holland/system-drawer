using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/HVAC Equipment")]
public sealed class HvacEquipmentRuntime : MonoBehaviour
{
    public HouseBioRhythm houseBio;
    public UtilityBioRhythm utilityBio;
    public HousePowerBus powerBus;
    public bool running = true;
    [Range(0f, 1f)] public float comfort01 = 0.8f;

    public void Tick(float dt)
    {
        if (houseBio == null)
            houseBio = GetComponentInParent<HouseBioRhythm>();
        bool power = houseBio == null || houseBio.electricAvailable01 > 0.2f;
        float hvac = running && power ? comfort01 : 0f;
        if (utilityBio != null)
            utilityBio.hvac01 = hvac;
    }

    public void SetRunning(bool on) => running = on;
}
