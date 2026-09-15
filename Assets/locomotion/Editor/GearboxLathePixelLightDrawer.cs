#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public enum PixelLightGridBrushKind
{
    PaintOn = 0,
    PaintOff = 1,
    FillSolid = 2,
    ChasePreset = 3,
    ClearFrame = 4,
    GridSlot = 5
}

/// <summary>Shared Frame/Shell × cube-face PixelLight UI for gearbox and lathe designers.</summary>
public static class GearboxLathePixelLightDrawer
{
    public static PixelLightMultiSlotCatalog DrawCatalogField(PixelLightMultiSlotCatalog catalog, string defaultName)
    {
        catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "PixelLight catalog", catalog, typeof(PixelLightMultiSlotCatalog), false);
        if (catalog == null && GUILayout.Button("Create PixelLight catalog"))
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save PixelLight Catalog", defaultName, "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
                SeedDefaultBags(catalog);
                AssetDatabase.SaveAssets();
            }
        }
        return catalog;
    }

    public static void SeedDefaultBags(PixelLightMultiSlotCatalog catalog)
    {
        if (catalog == null) return;
        foreach (PixelLightDesignerView view in System.Enum.GetValues(typeof(PixelLightDesignerView)))
        {
            EnsurePattern(catalog, catalog.GetOrCreate(view, PixelLightDesignerScope.Frame, 0), 8, 8);
            EnsurePattern(catalog, catalog.GetOrCreate(view, PixelLightDesignerScope.Shell, 0), 8, 8);
        }
        EnsurePattern(catalog, catalog.GetOrCreate(PixelLightDesignerView.Front, PixelLightDesignerScope.Shell, 1), 5, 5);
        EditorUtility.SetDirty(catalog);
    }

    public static void DrawFrameShell(
        PixelLightMultiSlotCatalog catalog,
        ref PixelLightDesignerView view,
        ref Bounds4SdfInclusionKind inclusion,
        ref Bounds4SdfInclusionPixelLightMount mount)
    {
        EditorGUILayout.LabelField("PixelLight — Frame / Shell", EditorStyles.boldLabel);
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("Assign or create a PixelLight catalog to edit Frame/Shell bags.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        view = (PixelLightDesignerView)EditorGUILayout.EnumPopup("Cube face", view);
        inclusion = (Bounds4SdfInclusionKind)EditorGUILayout.EnumPopup("Inclusion", inclusion);
        if (EditorGUI.EndChangeCheck() && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.rotation = PixelLightIsometricViews.RotationFor(view);

        EditorGUILayout.LabelField("Isometric", PixelLightIsometricViews.RotationFor(view).eulerAngles.ToString("0.0"));
        var scope = inclusion == Bounds4SdfInclusionKind.Frame
            ? PixelLightDesignerScope.Frame
            : PixelLightDesignerScope.Shell;
        var bag = catalog.GetOrCreate(view, scope, 0);
        EditorGUILayout.HelpBox(
            $"Editing {view} × {scope}. Other cube faces keep their own bags.",
            MessageType.None);

        bag.gridWidth = EditorGUILayout.IntSlider("Grid width", Mathf.Max(1, bag.gridWidth), 1, 32);
        bag.gridHeight = EditorGUILayout.IntSlider("Grid height", Mathf.Max(1, bag.gridHeight), 1, 32);
        bag.cellSize = EditorGUILayout.FloatField("Cell size", bag.cellSize);
        DrawBrushAndMountPickers(catalog, bag, ref mount, true);
        EnsurePattern(catalog, bag, bag.gridWidth, bag.gridHeight);
        DrawPatternGrid(bag, catalog);
        PixelLightRadialBrushDrawer.Draw(mount, bag);

        if (mount != null)
        {
            mount.inclusionKind = inclusion;
            mount.shellCurve = (CustomRadialSideAsset)EditorGUILayout.ObjectField(
                "Outer curve", mount.shellCurve, typeof(CustomRadialSideAsset), false);
            mount.frameThickness = EditorGUILayout.FloatField("Frame thickness", mount.frameThickness);
            mount.composition = (SdfMax.SdfMaxCompositionAsset)EditorGUILayout.ObjectField(
                "SDF composition", mount.composition, typeof(SdfMax.SdfMaxCompositionAsset), false);
            mount.convertFromMesh = (Mesh)EditorGUILayout.ObjectField(
                "Convert from mesh", mount.convertFromMesh, typeof(Mesh), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull mount → bag"))
            {
                bag.CopyFromMount(mount, mount.rig ?? mount.GetComponentInChildren<PixelLightRig>());
                EditorUtility.SetDirty(catalog);
            }
            if (GUILayout.Button("Apply bag → mount"))
            {
                bag.ApplyToMount(mount);
                mount.EnsureRig();
                EditorUtility.SetDirty(mount);
                EditorUtility.SetDirty(catalog);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Convert from mesh") && mount.convertFromMesh != null)
                mount.ConvertFromMeshNow();
            if (GUILayout.Button("Bake Frame/Shell SDF"))
                mount.BakeHardwareSubtract(mount.composition);
        }
        else
            EditorGUILayout.HelpBox("Assign a Frame/Shell mount to stamp cells and bake SDF.", MessageType.None);
    }

    public static void DrawMillKerf(
        PixelLightMultiSlotCatalog catalog,
        ref PixelLightGridMountGameObject mount,
        ref int requestedCuts)
    {
        EditorGUILayout.LabelField("PixelLight — mill kerfs", EditorStyles.boldLabel);
        requestedCuts = EditorGUILayout.IntField("Cut count", requestedCuts);
        int odd = MillKerfCuts.OddCutCount(requestedCuts);
        int center = MillKerfCuts.CenterLineIndex(odd);
        EditorGUILayout.LabelField("Odd cuts / center line", odd + " / " + center);
        MillKerfCuts.DualCrescentOffsets(0.5f, out var left, out var right);
        EditorGUILayout.LabelField("Dual-crescent", left.ToString("0.00") + " · " + right.ToString("0.00"));

        if (catalog != null)
        {
            var bag = catalog.GetOrCreate(PixelLightDesignerView.Front, PixelLightDesignerScope.Shell, 1);
            if (GUILayout.Button("Apply centroid + odd kerf grid to bag"))
            {
                bag.gridWidth = odd;
                bag.gridHeight = odd;
                var c = MillKerfCuts.CentroidCell(odd, odd);
                bag.centroidCellX = Mathf.RoundToInt(c.x);
                bag.centroidCellY = Mathf.RoundToInt(c.y);
                if (mount != null)
                    bag.ApplyToMount(mount);
                EditorUtility.SetDirty(catalog);
                if (mount != null)
                    EditorUtility.SetDirty(mount);
            }
            DrawBrushAndMountPickers(catalog, bag, ref mount, false);
            EnsurePattern(catalog, bag, odd, odd);
            DrawPatternGrid(bag, catalog);
            PixelLightRadialBrushDrawer.Draw(mount, bag);
        }
        else if (mount != null)
            PixelLightRadialBrushDrawer.DrawOnMount(mount);
        else
            EditorGUILayout.HelpBox("Assign a catalog or mill-kerf PixelLight mount.", MessageType.Info);
    }

    static void EnsurePattern(PixelLightMultiSlotCatalog catalog, PixelLightViewScopeSettings bag, int w, int h)
    {
        if (catalog == null || bag == null) return;
        w = Mathf.Max(1, w);
        h = Mathf.Max(1, h);
        if (bag.pattern == null)
        {
            var pattern = PixelLightPatternAsset.CreateSolid(' ', w, h);
            pattern.name = catalog.name + "_" + bag.Key.Replace('|', '_');
            pattern.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(pattern, catalog);
            bag.pattern = pattern;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
        bag.pattern.gridWidth = w;
        bag.pattern.gridHeight = h;
        if (bag.pattern.layers == null || bag.pattern.layers.Count == 0)
            bag.pattern.layers.Add(new PixelLightLayer());
        var layer = bag.pattern.layers[0];
        if (layer.frames == null || layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        EnsureFrameSize(layer.frames[0], w, h);
    }

    static void DrawPatternGrid(PixelLightViewScopeSettings bag, PixelLightMultiSlotCatalog catalog)
    {
        var pattern = bag?.pattern;
        if (pattern == null) return;
        if (pattern.layers == null || pattern.layers.Count == 0)
            pattern.layers.Add(new PixelLightLayer());
        bag.paintLayer = Mathf.Clamp(bag.paintLayer, 0, pattern.layers.Count - 1);
        var layer = pattern.layers[bag.paintLayer];
        if (layer.frames == null || layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        bag.paintFrame = Mathf.Clamp(bag.paintFrame, 0, layer.frames.Count - 1);
        var frame = layer.frames[bag.paintFrame];
        int w = Mathf.Max(1, pattern.gridWidth);
        int h = Mathf.Max(1, pattern.gridHeight);
        EnsureFrameSize(frame, w, h);

        EditorGUILayout.LabelField("PixelLight grid", EditorStyles.boldLabel);
        var brush = (PixelLightGridBrushKind)bag.brushKind;
        float cell = 18f;
        var rect = GUILayoutUtility.GetRect(w * cell, h * cell + 4f, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, w * cell, h * cell), new Color(0.12f, 0.12f, 0.14f));
        for (int y = 0; y < h; y++)
        {
            string row = y < frame.rows.Count ? frame.rows[y] : "";
            for (int x = 0; x < w; x++)
            {
                var r = new Rect(rect.x + x * cell, rect.y + y * cell, cell - 1f, cell - 1f);
                bool on = x < row.Length && row[x] != ' ' && row[x] != '.' && row[x] != '_';
                bool centroid = x == bag.centroidCellX && y == bag.centroidCellY;
                Color fill = centroid
                    ? new Color(0.95f, 0.75f, 0.2f)
                    : (on ? new Color(1f, 0.28f, 0.2f) : new Color(0.38f, 0.38f, 0.42f));
                EditorGUI.DrawRect(r, fill);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    ApplyBrushClick(bag, frame, x, y, brush);
                    EditorUtility.SetDirty(pattern);
                    if (catalog != null)
                        EditorUtility.SetDirty(catalog);
                    Event.current.Use();
                    if (EditorWindow.focusedWindow != null)
                        EditorWindow.focusedWindow.Repaint();
                }
            }
        }
        EditorGUILayout.LabelField($"{w}×{h}  brush {brush}  ·  gold = centroid");
    }

    static void DrawBrushAndMountPickers<T>(
        PixelLightMultiSlotCatalog catalog,
        PixelLightViewScopeSettings bag,
        ref T mount,
        bool bounds4)
        where T : PixelLightGridMountGameObject
    {
        if (bag != null)
        {
            EditorGUI.BeginChangeCheck();
            var brush = (PixelLightGridBrushKind)EditorGUILayout.EnumPopup(
                "Brush", (PixelLightGridBrushKind)bag.brushKind);
            if (EditorGUI.EndChangeCheck())
            {
                bag.brushKind = (int)brush;
                ApplyBrushCommand(bag, brush);
                if (catalog != null)
                    EditorUtility.SetDirty(catalog);
            }
        }

        if (catalog == null) return;
        if (catalog.gridSlots == null)
            catalog.gridSlots = new System.Collections.Generic.List<PixelLightGridSlotEntry>();

        var labels = new string[catalog.gridSlots.Count + 1];
        labels[0] = "(none)";
        int selected = 0;
        for (int i = 0; i < catalog.gridSlots.Count; i++)
        {
            var e = catalog.gridSlots[i];
            string lab = e != null && !string.IsNullOrEmpty(e.label) ? e.label : "Mount " + (i + 1);
            labels[i + 1] = lab;
            if (e != null && e.mount == mount && mount != null)
                selected = i + 1;
        }
        int next = EditorGUILayout.Popup("Grid mount", selected, labels);
        if (next != selected)
        {
            if (next <= 0)
                mount = null;
            else
            {
                var picked = catalog.gridSlots[next - 1]?.mount as T;
                if (picked != null)
                    mount = picked;
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add numbered mount"))
        {
            mount = AddNumberedMount<T>(catalog, "Mount", bounds4);
            EditorUtility.SetDirty(catalog);
        }
        EditorGUILayout.EndHorizontal();
    }

    static T AddNumberedMount<T>(PixelLightMultiSlotCatalog catalog, string prefix, bool bounds4)
        where T : PixelLightGridMountGameObject
    {
        string label = catalog.NextIncrementingLabel(prefix);
        var entry = catalog.AddSlot(label);
        var go = new GameObject(label);
        Undo.RegisterCreatedObjectUndo(go, "Add PixelLight mount");
        PixelLightGridMountGameObject created = bounds4
            ? go.AddComponent<Bounds4SdfInclusionPixelLightMount>()
            : go.AddComponent<PixelLightGridMountGameObject>();
        entry.mount = created;
        entry.label = label;
        return created as T;
    }

    static void ApplyBrushCommand(PixelLightViewScopeSettings bag, PixelLightGridBrushKind brush)
    {
        var pattern = bag?.pattern;
        if (pattern == null) return;
        if (pattern.layers == null || pattern.layers.Count == 0)
            pattern.layers.Add(new PixelLightLayer());
        var layer = pattern.layers[Mathf.Clamp(bag.paintLayer, 0, pattern.layers.Count - 1)];
        if (layer.frames == null || layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        var frame = layer.frames[Mathf.Clamp(bag.paintFrame, 0, layer.frames.Count - 1)];
        int w = Mathf.Max(1, pattern.gridWidth);
        int h = Mathf.Max(1, pattern.gridHeight);
        EnsureFrameSize(frame, w, h);
        if (brush == PixelLightGridBrushKind.FillSolid)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    SetCell(frame, x, y, true);
        }
        else if (brush == PixelLightGridBrushKind.ClearFrame)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    SetCell(frame, x, y, false);
        }
        else if (brush == PixelLightGridBrushKind.ChasePreset)
        {
            var chase = PixelLightPatternAsset.CreateChasePreset();
            if (chase.layers != null && chase.layers.Count > 0
                && chase.layers[0].frames != null && chase.layers[0].frames.Count > 0)
            {
                var src = chase.layers[0].frames[0];
                EnsureFrameSize(frame, w, h);
                for (int y = 0; y < h && y < src.rows.Count; y++)
                    frame.rows[y] = src.rows[y].Length >= w
                        ? src.rows[y].Substring(0, w)
                        : src.rows[y].PadRight(w, ' ');
            }
            UnityEngine.Object.DestroyImmediate(chase);
        }
        EditorUtility.SetDirty(pattern);
    }

    static void ApplyBrushClick(
        PixelLightViewScopeSettings bag, PixelLightFrame frame, int x, int y, PixelLightGridBrushKind brush)
    {
        if (brush == PixelLightGridBrushKind.PaintOff)
            SetCell(frame, x, y, false);
        else if (brush == PixelLightGridBrushKind.GridSlot)
        {
            bag.centroidCellX = x;
            bag.centroidCellY = y;
        }
        else if (brush == PixelLightGridBrushKind.PaintOn)
            SetCell(frame, x, y, true);
        else
            SetCell(frame, x, y, !IsOn(frame, x, y));
    }

    static bool IsOn(PixelLightFrame frame, int x, int y)
    {
        if (frame?.rows == null || y < 0 || y >= frame.rows.Count) return false;
        string row = frame.rows[y] ?? "";
        return x < row.Length && row[x] != ' ' && row[x] != '.' && row[x] != '_';
    }

    static void EnsureFrameSize(PixelLightFrame frame, int w, int h)
    {
        if (frame.rows == null)
            frame.rows = new System.Collections.Generic.List<string>();
        while (frame.rows.Count < h)
            frame.rows.Add(new string(' ', w));
        while (frame.rows.Count > h)
            frame.rows.RemoveAt(frame.rows.Count - 1);
        for (int y = 0; y < h; y++)
        {
            var row = frame.rows[y] ?? "";
            if (row.Length < w) row = row.PadRight(w, ' ');
            if (row.Length > w) row = row.Substring(0, w);
            frame.rows[y] = row;
        }
    }

    static void SetCell(PixelLightFrame frame, int x, int y, bool on)
    {
        var chars = frame.rows[y].ToCharArray();
        chars[x] = on ? '#' : ' ';
        frame.rows[y] = new string(chars);
    }
}
#endif
