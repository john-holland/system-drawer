using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D spatial generator node with UI/menu-friendly placement defaults.
/// Inherit for procedural 2D layout (main menu planks, HUD panels, etc.).
/// </summary>
[AddComponentMenu("Bedoga Generator/SG Behavior Tree Node 2D")]
public class SGBehaviorTreeNode2D : SGBehaviorTreeNode
{
    [Tooltip("When true, this node stacks siblings along +X inside its parent bounds.")]
    public bool stackSiblingsHorizontally = true;

    protected virtual void Reset()
    {
        ApplyTwoDimensionalDefaults();
    }

    protected virtual void OnValidate()
    {
        if (minSpace == Vector3.zero && maxSpace == Vector3.zero)
            ApplyTwoDimensionalDefaults();
    }

    public void ApplyTwoDimensionalDefaults(bool asSiblingStack = false)
    {
        minSpace = new Vector3(2f, 0.5f, 0f);
        maxSpace = new Vector3(2.5f, 0.6f, 0f);
        optimalSpace = new Vector3(2.2f, 0.55f, 0f);
        fitX = FitX.Left;
        fitY = FitY.Center;
        fitZ = FitZ.Center;
        stackDirection = AxisDirection.PosX;
        wrapDirection = AxisDirection.PosY;
        placeSearchMode = PlaceSearchMode.FromFit;
        placementMode = asSiblingStack ? PlacementMode.Forward : PlacementMode.In;
        placementLimit = 1;
    }

    /// <summary>Rebuild childNodes from transform children that are SGBehaviorTreeNode2D.</summary>
    public virtual void RefreshChildNodesFromHierarchy()
    {
        childNodes.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).GetComponent<SGBehaviorTreeNode2D>();
            if (child != null)
                childNodes.Add(child);
        }
    }
}
