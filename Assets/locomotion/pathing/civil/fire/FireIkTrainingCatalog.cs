using UnityEngine;

[CreateAssetMenu(fileName = "FireIkTrainingCatalog", menuName = "Locomotion/Civil/Fire IK Training Catalog")]
public sealed class FireIkTrainingCatalog : ScriptableObject
{
    public string[] modeIds = { "pole_slide", "axe_breach", "sledge_breach", "hose_aim", "hydrant_open" };
    public string defaultPoleModeId = "pole_slide";
    public string defaultBreachModeId = "axe_breach";
}
