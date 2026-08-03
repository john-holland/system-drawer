using UnityEngine;

/// <summary>Stretch: gym-specific IK training modes catalog (extends PhysicsIK catalogs).</summary>
[CreateAssetMenu(fileName = "GymIkTrainingCatalog", menuName = "Locomotion/Civil/Gym IK Training Catalog")]
public sealed class GymIkTrainingCatalog : ScriptableObject
{
    public string[] modeIds = { "treadmill", "bench_press", "squat_rack", "cable_row", "check_in" };
    public string frontDeskModeId = "check_in";
}
