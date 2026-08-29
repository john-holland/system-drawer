using UnityEngine;

/// <summary>A split piece that still points at the loop-section picker asset.</summary>
[AddComponentMenu("Locomotion/Mesh/Skinned Mesh Loop Section Piece")]
public sealed class SkinnedMeshLoopSectionPiece : MonoBehaviour
{
    public SkinnedMeshLoopSectionAsset sectionAsset;
    public string[] loopIds;
    public SkinnedMeshLoopSplitMode splitMode;
}
