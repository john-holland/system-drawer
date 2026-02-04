using UnityEngine;

/// <summary>
/// Marker for GameObjects that are "loose" (not placed by the current 3D/4D generator).
/// SkinController finds these and applies the loose-object policy (disable, move to limbo, scale to zero) when switching skin.
/// </summary>
public class SpatialSkinLooseObject : MonoBehaviour
{
}
