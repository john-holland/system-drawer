#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Shared PixelLight radial brush fields for heli / airport / accordion.</summary>
public static class PixelLightRadialBrushDrawer
{
    public static readonly string[] SideLabels =
    {
        "Center", "Upper Left", "Up", "Upper Right", "Right",
        "Lower Right", "Bottom", "Left Bottom", "Left"
    };

    public static void Draw(PixelLightGridMountGameObject mount, PixelLightViewScopeSettings vs)
    {
        if (mount == null && vs == null)
            return;

        EditorGUILayout.LabelField("Radial / minigrid brush", EditorStyles.boldLabel);
        if (vs != null)
        {
            vs.minigridW = EditorGUILayout.IntSlider("Minigrid W", Mathf.Max(1, vs.minigridW), 1, 16);
            vs.minigridH = EditorGUILayout.IntSlider("Minigrid H", Mathf.Max(1, vs.minigridH), 1, 16);
            vs.centroidCellX = EditorGUILayout.IntField("Centroid cell X", vs.centroidCellX);
            vs.centroidCellY = EditorGUILayout.IntField("Centroid cell Y", vs.centroidCellY);
            vs.radialSide = (RadialSide)EditorGUILayout.Popup("Side", (int)vs.radialSide, SideLabels);
            vs.customRadialSide = (CustomRadialSideAsset)EditorGUILayout.ObjectField(
                "Custom radial side", vs.customRadialSide, typeof(CustomRadialSideAsset), false);
            vs.radialHost = (RadialBuildHost)EditorGUILayout.ObjectField(
                "Radial host", vs.radialHost, typeof(RadialBuildHost), true);
            vs.recursiveBlock = EditorGUILayout.Toggle("Recursive block", vs.recursiveBlock);
            if (vs.recursiveBlock)
            {
                vs.nestedMinigridW = EditorGUILayout.IntSlider("Nested W", Mathf.Max(1, vs.nestedMinigridW), 1, 8);
                vs.nestedMinigridH = EditorGUILayout.IntSlider("Nested H", Mathf.Max(1, vs.nestedMinigridH), 1, 8);
            }
            if (vs.radialBuild == null)
                vs.radialBuild = new RadialBuildSpec();
            vs.radialBuild.count = EditorGUILayout.IntSlider("Piece count", Mathf.Max(1, vs.radialBuild.count), 1, 24);
            vs.radialBuild.joinKind = (RadialJoinKind)EditorGUILayout.EnumPopup("Join kind", vs.radialBuild.joinKind);
            vs.radialBuild.joinOffset = EditorGUILayout.FloatField("Join offset", vs.radialBuild.joinOffset);
            vs.radialBuild.jointId = EditorGUILayout.TextField("Joint id", vs.radialBuild.jointId ?? "");
        }

        if (mount != null)
        {
            DrawHostButtons(mount);
            DrawPreview(mount);
        }
    }

    public static void DrawOnMount(PixelLightGridMountGameObject mount)
    {
        if (mount == null)
            return;
        EditorGUILayout.LabelField("Radial / minigrid brush", EditorStyles.boldLabel);
        mount.minigridW = EditorGUILayout.IntSlider("Minigrid W", Mathf.Max(1, mount.minigridW), 1, 16);
        mount.minigridH = EditorGUILayout.IntSlider("Minigrid H", Mathf.Max(1, mount.minigridH), 1, 16);
        mount.centroidCellX = EditorGUILayout.IntField("Centroid cell X", mount.centroidCellX);
        mount.centroidCellY = EditorGUILayout.IntField("Centroid cell Y", mount.centroidCellY);
        mount.radialSide = (RadialSide)EditorGUILayout.Popup("Side", (int)mount.radialSide, SideLabels);
        mount.customRadialSide = (CustomRadialSideAsset)EditorGUILayout.ObjectField(
            "Custom radial side", mount.customRadialSide, typeof(CustomRadialSideAsset), false);
        mount.radialHost = (RadialBuildHost)EditorGUILayout.ObjectField(
            "Radial host", mount.radialHost, typeof(RadialBuildHost), true);
        mount.recursiveBlock = EditorGUILayout.Toggle("Recursive block", mount.recursiveBlock);
        if (mount.recursiveBlock)
        {
            mount.nestedMinigridW = EditorGUILayout.IntSlider("Nested W", Mathf.Max(1, mount.nestedMinigridW), 1, 8);
            mount.nestedMinigridH = EditorGUILayout.IntSlider("Nested H", Mathf.Max(1, mount.nestedMinigridH), 1, 8);
        }
        if (mount.radialBuild == null)
            mount.radialBuild = new RadialBuildSpec();
        mount.radialBuild.count = EditorGUILayout.IntSlider("Piece count", Mathf.Max(1, mount.radialBuild.count), 1, 24);
        mount.radialBuild.joinKind = (RadialJoinKind)EditorGUILayout.EnumPopup("Join kind", mount.radialBuild.joinKind);
        mount.radialBuild.joinOffset = EditorGUILayout.FloatField("Join offset", mount.radialBuild.joinOffset);
        mount.radialBuild.jointId = EditorGUILayout.TextField("Joint id", mount.radialBuild.jointId ?? "");
        DrawHostButtons(mount);
        DrawPreview(mount);
    }

    static void DrawHostButtons(PixelLightGridMountGameObject mount)
    {
        var host = mount.ResolvedRadialHost();
        if (host == null)
        {
            if (GUILayout.Button("Add RadialBuildHost"))
            {
                Undo.AddComponent<RadialBuildHost>(mount.gameObject);
                mount.radialHost = mount.GetComponent<RadialBuildHost>();
            }
            return;
        }
        host.centerPost = (GameObject)EditorGUILayout.ObjectField(
            "CenterPost", host.centerPost, typeof(GameObject), true);
        host.customAngle = EditorGUILayout.FloatField("customAngle", host.customAngle);
        host.customAngleObject = (GameObject)EditorGUILayout.ObjectField(
            "customAngleObject", host.customAngleObject, typeof(GameObject), true);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Anchor Objects"))
        {
            Undo.RecordObject(host, "Create radial anchors");
            host.CreateAnchorObjects();
            EditorUtility.SetDirty(host);
        }
        if (GUILayout.Button("Refresh solved joints"))
        {
            host.RefreshSolved();
            EditorUtility.SetDirty(host);
        }
        EditorGUILayout.EndHorizontal();
    }

    static void DrawPreview(PixelLightGridMountGameObject mount)
    {
        var host = mount.ResolvedRadialHost();
        if (host == null)
            return;
        var labels = host.PreviewLabels();
        if (labels.Length == 0)
        {
            EditorGUILayout.HelpBox("Preview configuration: none", MessageType.None);
            return;
        }
        int next = EditorGUILayout.Popup("Preview configuration", host.previewConfigIndex, labels);
        if (next != host.previewConfigIndex)
        {
            Undo.RecordObject(host, "Select radial preview");
            host.previewConfigIndex = next;
            mount.previewConfigIndex = next;
            if (next >= 0 && next < host.solvedConfigs.Count && host.spec != null)
                host.spec.ApplySolved(host.solvedConfigs[next]);
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(mount);
        }
    }
}
#endif
