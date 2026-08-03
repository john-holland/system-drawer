using UnityEngine;

/// <summary>Dance IK training modes for bars/clubs (compose with LoveMaking DanceClose).</summary>
[CreateAssetMenu(fileName = "DanceIkTrainingCatalog", menuName = "Locomotion/Civil/Dance IK Training Catalog")]
public sealed class DanceIkTrainingCatalog : ScriptableObject
{
    public string[] modeIds = { "bar_sway", "club_groove", "dance_close", "slow_sway" };
    public string defaultBarModeId = "bar_sway";
    public string defaultClubModeId = "club_groove";
}
