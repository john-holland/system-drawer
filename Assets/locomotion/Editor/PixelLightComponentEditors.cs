#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector paint grid for PixelLight mounts and rigs so selecting the component
/// shows the same Frame/Shell-style grid as the designers.
/// </summary>
[CustomEditor(typeof(PixelLightGridMountGameObject), true)]
public sealed class PixelLightGridMountEditor : Editor
{
    PixelLightMultiSlotCatalog _catalog;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var mount = (PixelLightGridMountGameObject)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PixelLight grid", EditorStyles.boldLabel);
        _catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "Catalog (optional)", _catalog, typeof(PixelLightMultiSlotCatalog), false);

        var bounds = mount as Bounds4SdfInclusionPixelLightMount;
        if (bounds != null)
            EditorGUILayout.LabelField(
                "Inclusion lemma",
                Bounds4SdfInclusionLemmas.ToInclusionLemma(bounds.inclusionKind));

        PixelLightRadialBrushDrawer.DrawOnMount(mount);
        GearboxLathePixelLightDrawer.DrawMountPatternGrid(mount, _catalog);

        if (GUILayout.Button("Ensure PixelLight rig"))
        {
            Undo.RecordObject(mount, "Ensure PixelLight rig");
            mount.EnsureRig();
            EditorUtility.SetDirty(mount);
        }

        if (GUI.changed)
            EditorUtility.SetDirty(mount);
    }
}

[CustomEditor(typeof(PixelLightRig))]
public sealed class PixelLightRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var rig = (PixelLightRig)target;
        EditorGUILayout.Space();
        GearboxLathePixelLightDrawer.DrawPatternAssetGrid(rig.pattern);
        if (GUI.changed)
            EditorUtility.SetDirty(rig);
    }
}
#endif
