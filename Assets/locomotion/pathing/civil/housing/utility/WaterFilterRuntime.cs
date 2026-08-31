using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Water Filter")]
public sealed class WaterFilterRuntime : MonoBehaviour
{
    public UtilityBioRhythm utilityBio;
    [Range(0f, 1f)] public float clog01;
    public float clogRate = 0.005f;

    public void Tick(float dt)
    {
        clog01 = Mathf.Clamp01(clog01 + clogRate * dt);
        if (utilityBio != null)
            utilityBio.filterClog01 = clog01;
    }

    public void ChangeFilter()
    {
        clog01 = 0f;
        if (utilityBio != null)
            utilityBio.filterClog01 = 0f;
    }
}
