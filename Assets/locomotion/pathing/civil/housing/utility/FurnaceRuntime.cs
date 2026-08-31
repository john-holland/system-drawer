using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Furnace")]
public sealed class FurnaceRuntime : MonoBehaviour
{
    public HouseBioRhythm houseBio;
    public UtilityBioRhythm utilityBio;
    public bool running = true;
    [Range(0f, 1f)] public float output01 = 0.8f;

    public void Tick(float dt)
    {
        if (houseBio == null)
            houseBio = GetComponentInParent<HouseBioRhythm>();
        float fuel = houseBio != null
            ? Mathf.Max(houseBio.gasAvailable01, houseBio.oilAvailable01)
            : 1f;
        float heat = running ? output01 * fuel : 0f;
        if (utilityBio != null)
            utilityBio.heat01 = heat;
    }

    public void SetRunning(bool on) => running = on;
}
