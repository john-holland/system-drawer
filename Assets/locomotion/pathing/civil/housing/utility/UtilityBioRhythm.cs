using UnityEngine;

/// <summary>Basement / utility channels that feed <see cref="HouseBioRhythm.utilityComfort01"/>.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Utility Bio Rhythm")]
public sealed class UtilityBioRhythm : MonoBehaviour
{
    public HouseBioRhythm houseBio;
    [Range(0f, 1f)] public float water01 = 1f;
    [Range(0f, 1f)] public float sewerBackup01;
    [Range(0f, 1f)] public float heat01 = 0.8f;
    [Range(0f, 1f)] public float hvac01 = 0.8f;
    [Range(0f, 1f)] public float filterClog01;
    [Range(0f, 1f)] public float gunk01;
    [Range(0f, 1f)] public float panelLoad01;
    [Range(0f, 1f)] public float flood01;
    public bool sumpOn;
    public float standingLiters;
    public CircuitBreakerPanel panel;
    public SumpPumpRuntime sump;
    public HouseBasementFloodCache floodCache;

    void Awake()
    {
        if (houseBio == null)
            houseBio = GetComponent<HouseBioRhythm>();
        if (panel == null)
            panel = GetComponent<CircuitBreakerPanel>();
        if (sump == null)
            sump = GetComponentInChildren<SumpPumpRuntime>(true);
        if (floodCache == null)
            floodCache = GetComponent<HouseBasementFloodCache>();
    }

    public void Tick(float dt)
    {
        if (floodCache != null)
            standingLiters = floodCache.standingLiters;
        flood01 = Mathf.Clamp01(standingLiters / 200f);
        if (sump != null)
            sumpOn = sump.lastOn;
        if (panel != null)
            panelLoad01 = panel.Load01();
        if (houseBio == null) return;
        float utilities = (water01 + (1f - sewerBackup01) + heat01 + hvac01 + (1f - filterClog01)
                           + (1f - gunk01) + (1f - panelLoad01) + (1f - flood01)) / 8f;
        houseBio.utilityComfort01 = Mathf.Clamp01(
            (houseBio.gasAvailable01 + houseBio.oilAvailable01 + houseBio.electricAvailable01 + utilities) / 4f);
    }
}
