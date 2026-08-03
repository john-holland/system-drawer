using System;
using UnityEngine;

public enum DestructibleLayerKind
{
    Ceiling = 0,
    Roof = 1,
    Sky = 2
}

[Serializable]
public sealed class DestructibleLayerRef
{
    public DestructibleLayerKind kind = DestructibleLayerKind.Ceiling;
    public GameObject layerRoot;
    public MonoBehaviour destructibleRenderer;
    public bool destroyed;

    public bool IsIntact()
    {
        if (destroyed) return false;
        if (layerRoot == null) return kind == DestructibleLayerKind.Sky;
        if (!layerRoot.activeInHierarchy) return false;
        if (destructibleRenderer != null)
        {
            var prop = destructibleRenderer.GetType().GetProperty("IsActivated");
            if (prop != null && prop.GetValue(destructibleRenderer) is bool activated && activated)
            {
                destroyed = true;
                return false;
            }
        }
        return true;
    }
}
