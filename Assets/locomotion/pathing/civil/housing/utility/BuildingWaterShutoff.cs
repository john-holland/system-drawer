using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Building Water Shutoff")]
public sealed class BuildingWaterShutoff : MonoBehaviour
{
    public bool open = true;
    public UtilityBioRhythm utilityBio;

    public void SetOpen(bool value)
    {
        open = value;
        if (utilityBio != null)
            utilityBio.water01 = open ? 1f : 0f;
    }
}
