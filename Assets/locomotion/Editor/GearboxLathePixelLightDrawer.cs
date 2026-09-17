#if UNITY_EDITOR
using System.Collections.Generic;
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

/// <summary>Transient PixelLight slot multi-selection (not serialized on the catalog).</summary>
public static class PixelLightSlotSelection
{
    public const string PrefShowStacks = "PixelLight.ShowStacks";
    public static readonly HashSet<string> SelectedIds = new HashSet<string>();
    public static int LastCellX;
    public static int LastCellY;
    public static Vector2 ListScroll;
    public static string FocusedSlotId;

    public static bool ShowStacks
    {
        get => EditorPrefs.GetBool(PrefShowStacks, false);
        set => EditorPrefs.SetBool(PrefShowStacks, value);
    }
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
        EditorGUILayout.LabelField("Inclusion lemma", Bounds4SdfInclusionLemmas.ToInclusionLemma(inclusion));
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
                mount.BakeHardwareSubtract(catalog, mount.composition);
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

    public static void DrawSewingStitchProgram(
        PixelLightMultiSlotCatalog catalog,
        SewingStitchProgram program,
        ref PixelLightGridMountGameObject mount)
    {
        EditorGUILayout.LabelField("PixelLight — stitch program", EditorStyles.boldLabel);
        if (program == null)
        {
            EditorGUILayout.HelpBox("Assign a stitch program.", MessageType.Info);
            return;
        }
        program.stitchClass = (SewingStitchClass)EditorGUILayout.EnumPopup("Stitch class", program.stitchClass);
        program.defaultGauge01 = EditorGUILayout.Slider("Default gauge", program.defaultGauge01, 0f, 1f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Lockstitch defaults"))
            CopyProgram(program, SewingStitchProgram.DefaultLockstitch());
        if (GUILayout.Button("Overlock defaults"))
            CopyProgram(program, SewingStitchProgram.DefaultOverlock());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Left chase (row)"))
            program.PaintRowChase(program.steps != null && program.steps.Count > 0 ? program.steps[0].cellY : 3, 8, SewingFeedDirection.Left);
        if (GUILayout.Button("Right chase (row)"))
            program.PaintRowChase(program.steps != null && program.steps.Count > 0 ? program.steps[0].cellY : 3, 8, SewingFeedDirection.Right);
        EditorGUILayout.EndHorizontal();

        var bag = catalog != null
            ? catalog.GetOrCreate(PixelLightDesignerView.Front, PixelLightDesignerScope.Shell, 1)
            : LooseStitchBag();
        DrawBrushAndMountPickers(catalog, bag, ref mount, false);
        if (catalog != null)
            EnsurePattern(catalog, bag, Mathf.Max(5, bag.gridWidth), Mathf.Max(5, bag.gridHeight));
        else
            EnsureLoosePattern(bag, 8, 8);
        SyncStitchPattern(bag, program);
        DrawPatternGrid(bag, catalog, program);
        DrawStitchStepListing(program);
        PixelLightRadialBrushDrawer.Draw(mount, bag);
    }

    static PixelLightViewScopeSettings _looseStitchBag;

    static PixelLightViewScopeSettings LooseStitchBag()
    {
        if (_looseStitchBag == null)
        {
            _looseStitchBag = new PixelLightViewScopeSettings
            {
                view = PixelLightDesignerView.Front,
                scope = PixelLightDesignerScope.Shell,
                magnetoIndex = 1,
                gridWidth = 8,
                gridHeight = 8
            };
        }
        return _looseStitchBag;
    }

    static void EnsureLoosePattern(PixelLightViewScopeSettings bag, int w, int h)
    {
        if (bag == null) return;
        w = Mathf.Max(1, w);
        h = Mathf.Max(1, h);
        if (bag.pattern == null)
            bag.pattern = PixelLightPatternAsset.CreateSolid(' ', w, h);
        bag.pattern.gridWidth = w;
        bag.pattern.gridHeight = h;
        if (bag.pattern.layers == null || bag.pattern.layers.Count == 0)
            bag.pattern.layers.Add(new PixelLightLayer());
        var layer = bag.pattern.layers[0];
        if (layer.frames == null || layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        EnsureFrameSize(layer.frames[0], w, h);
    }

    /// <summary>Paint grid for inspectors / designers that already have a view-scope bag.</summary>
    public static void DrawInspectorGrid(
        PixelLightViewScopeSettings bag,
        PixelLightMultiSlotCatalog catalog,
        SewingStitchProgram stitchProgram = null)
    {
        if (bag == null) return;
        if (bag.pattern == null)
            EnsureLoosePattern(bag, Mathf.Max(1, bag.gridWidth), Mathf.Max(1, bag.gridHeight));
        DrawPatternGrid(bag, catalog, stitchProgram);
    }

    /// <summary>Paint grid for a scene mount (inspector, accordion, airplane / garage / window).</summary>
    public static void DrawMountPatternGrid(
        PixelLightGridMountGameObject mount,
        PixelLightMultiSlotCatalog catalog = null)
    {
        if (mount == null) return;
        var bag = InspectorBagFor(mount.GetInstanceID());
        bag.CopyFromMount(mount, mount.rig != null ? mount.rig : mount.GetComponentInChildren<PixelLightRig>());
        if (bag.pattern == null)
        {
            EditorGUILayout.HelpBox("Assign or create a PixelLight pattern to paint this mount.", MessageType.Info);
            if (GUILayout.Button("Create PixelLight pattern"))
            {
                var path = EditorUtility.SaveFilePanelInProject(
                    "Save PixelLight Pattern", mount.gameObject.name + "PixelLight", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    int w = Mathf.Max(1, mount.gridWidth);
                    int h = Mathf.Max(1, mount.gridHeight);
                    var created = PixelLightPatternAsset.CreateSolid(' ', w, h);
                    AssetDatabase.CreateAsset(created, path);
                    mount.pattern = created;
                    bag.pattern = created;
                    EditorUtility.SetDirty(mount);
                }
            }
            return;
        }

        EditorGUI.BeginChangeCheck();
        var brush = (PixelLightGridBrushKind)EditorGUILayout.EnumPopup(
            "Grid brush", (PixelLightGridBrushKind)bag.brushKind);
        if (EditorGUI.EndChangeCheck())
            bag.brushKind = (int)brush;

        DrawPatternGrid(bag, catalog);
        if (bag.pattern != null)
            mount.pattern = bag.pattern;
    }

    /// <summary>Paint grid for a standalone pattern (airport / campus / timed-adjacent).</summary>
    public static void DrawPatternAssetGrid(
        PixelLightPatternAsset pattern,
        PixelLightMultiSlotCatalog catalog = null)
    {
        if (pattern == null) return;
        var bag = InspectorBagFor(pattern.GetInstanceID());
        bag.pattern = pattern;
        bag.gridWidth = Mathf.Max(1, pattern.gridWidth);
        bag.gridHeight = Mathf.Max(1, pattern.gridHeight);
        EditorGUI.BeginChangeCheck();
        var brush = (PixelLightGridBrushKind)EditorGUILayout.EnumPopup(
            "Grid brush", (PixelLightGridBrushKind)bag.brushKind);
        if (EditorGUI.EndChangeCheck())
            bag.brushKind = (int)brush;
        DrawPatternGrid(bag, catalog);
    }

    static readonly System.Collections.Generic.Dictionary<int, PixelLightViewScopeSettings> InspectorBags =
        new System.Collections.Generic.Dictionary<int, PixelLightViewScopeSettings>();

    static PixelLightViewScopeSettings InspectorBagFor(int id)
    {
        if (!InspectorBags.TryGetValue(id, out var bag) || bag == null)
        {
            bag = new PixelLightViewScopeSettings();
            InspectorBags[id] = bag;
        }
        return bag;
    }

    static void CopyProgram(SewingStitchProgram dest, SewingStitchProgram src)
    {
        dest.stitchClass = src.stitchClass;
        dest.defaultGauge01 = src.defaultGauge01;
        dest.steps = src.steps;
        dest.Reindex();
    }

    static void SyncStitchPattern(PixelLightViewScopeSettings bag, SewingStitchProgram program)
    {
        if (bag?.pattern == null || program?.steps == null) return;
        if (bag.pattern.layers == null || bag.pattern.layers.Count == 0) return;
        var layer = bag.pattern.layers[0];
        if (layer.frames == null || layer.frames.Count == 0) return;
        var frame = layer.frames[0];
        int w = Mathf.Max(1, bag.pattern.gridWidth);
        int h = Mathf.Max(1, bag.pattern.gridHeight);
        EnsureFrameSize(frame, w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                SetCell(frame, x, y, program.StepAt(x, y) != null);
    }

    static int _stitchStepIndex;

    static void DrawStitchStepListing(SewingStitchProgram program)
    {
        if (program.steps == null) program.steps = new System.Collections.Generic.List<SewingNeedleStep>();
        program.Reindex();
        EditorGUILayout.LabelField("Step index listing", EditorStyles.boldLabel);
        if (program.steps.Count == 0)
        {
            EditorGUILayout.HelpBox("Paint On the stitch grid (Front × Shell × magnetoIndex 1) to add needle steps.", MessageType.None);
            return;
        }
        _stitchStepIndex = Mathf.Clamp(_stitchStepIndex, 0, program.steps.Count - 1);
        _stitchStepIndex = EditorGUILayout.IntSlider("Step", _stitchStepIndex, 0, program.steps.Count - 1);
        var step = program.steps[_stitchStepIndex];
        if (step == null) return;
        EditorGUILayout.LabelField("Index", step.index.ToString());
        EditorGUILayout.LabelField("Cell", step.cellX + ", " + step.cellY);
        step.direction = (SewingFeedDirection)EditorGUILayout.EnumPopup("Feed direction", step.direction);
        step.clothSide = (ClothThreadSide)EditorGUILayout.EnumPopup("Thread cloth side", step.clothSide);
        step.entryAngleDeg = EditorGUILayout.FloatField("Needle entry deg", step.entryAngleDeg);
        step.exitAngleDeg = EditorGUILayout.FloatField("Needle exit deg", step.exitAngleDeg);
        step.gauge01 = EditorGUILayout.Slider("Gauge", step.gauge01, 0f, 1f);
        step.connectingStrand = EditorGUILayout.Toggle("Connecting strand", step.connectingStrand);
        EditorGUILayout.LabelField("Connecting span m", program.ConnectingSpanM().ToString("0.000"));
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

    static void DrawPatternGrid(
        PixelLightViewScopeSettings bag,
        PixelLightMultiSlotCatalog catalog,
        SewingStitchProgram stitchProgram = null)
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
        bool explode = PixelLightSlotSelection.ShowStacks;
        explode = EditorGUILayout.Toggle("Show stacks", explode);
        PixelLightSlotSelection.ShowStacks = explode;
        var brush = (PixelLightGridBrushKind)bag.brushKind;
        float cell = 18f;
        int maxZ = catalog != null ? catalog.MaxZIndex() : 0;
        Vector2 size = PixelLightStackPreview.GridSize(
            w, h, maxZ, cell, PixelLightStackPreview.DefaultPadPx, PixelLightStackPreview.DefaultSkew, explode);
        var rect = GUILayoutUtility.GetRect(size.x, size.y + 4f, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, size.x, size.y), new Color(0.12f, 0.12f, 0.14f));

        for (int y = 0; y < h; y++)
        {
            string row = y < frame.rows.Count ? frame.rows[y] : "";
            for (int x = 0; x < w; x++)
            {
                var r = PixelLightStackPreview.CellRect(x, y, 0, cell,
                    PixelLightStackPreview.DefaultPadPx, PixelLightStackPreview.DefaultSkew, false);
                r.x += rect.x;
                r.y += rect.y;
                bool on = x < row.Length && row[x] != ' ' && row[x] != '.' && row[x] != '_';
                bool centroid = x == bag.centroidCellX && y == bag.centroidCellY;
                Color fill = centroid
                    ? new Color(0.95f, 0.75f, 0.2f)
                    : (on ? new Color(1f, 0.28f, 0.2f) : new Color(0.38f, 0.38f, 0.42f));
                EditorGUI.DrawRect(r, fill);
                if (catalog != null)
                {
                    var at = catalog.SlotsAtCell(x, y);
                    for (int s = 0; s < at.Count; s++)
                    {
                        var slot = at[s];
                        var sr = PixelLightStackPreview.CellRect(
                            x, y, slot.zIndex, cell,
                            PixelLightStackPreview.DefaultPadPx, PixelLightStackPreview.DefaultSkew, explode);
                        sr.x += rect.x;
                        sr.y += rect.y;
                        bool sel = PixelLightSlotSelection.SelectedIds.Contains(slot.slotId);
                        var overlay = sel
                            ? new Color(0.25f, 0.75f, 1f, explode ? 0.85f : 0.45f)
                            : SlotKindColor(slot.kind);
                        overlay.a = explode ? 0.8f : 0.35f;
                        EditorGUI.DrawRect(sr, overlay);
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) && !explode)
                    HandleGridClick(bag, frame, catalog, stitchProgram, brush, pattern, x, y, null);
            }
        }

        if (explode && catalog != null && Event.current.type == EventType.MouseDown)
        {
            PixelLightGridSlotEntry hit = null;
            int hx = 0, hy = 0;
            var slots = catalog.gridSlots;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (slot == null) continue;
                var sr = PixelLightStackPreview.CellRect(
                    slot.cellX, slot.cellY, slot.zIndex, cell,
                    PixelLightStackPreview.DefaultPadPx, PixelLightStackPreview.DefaultSkew, true);
                sr.x += rect.x;
                sr.y += rect.y;
                if (sr.Contains(Event.current.mousePosition))
                {
                    hit = slot;
                    hx = slot.cellX;
                    hy = slot.cellY;
                    break;
                }
            }
            if (hit != null)
                HandleGridClick(bag, frame, catalog, stitchProgram, brush, pattern, hx, hy, hit);
            else
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var r = PixelLightStackPreview.CellRect(x, y, 0, cell,
                            PixelLightStackPreview.DefaultPadPx, PixelLightStackPreview.DefaultSkew, true);
                        r.x += rect.x;
                        r.y += rect.y;
                        if (r.Contains(Event.current.mousePosition))
                        {
                            HandleGridClick(bag, frame, catalog, stitchProgram, brush, pattern, x, y, null);
                            y = h;
                            break;
                        }
                    }
                }
            }
        }

        EditorGUILayout.LabelField($"{w}×{h}  brush {brush}  ·  gold = centroid  ·  cyan = selected slot");
        DrawSlotListbox(catalog);
    }

    static Color SlotKindColor(PixelLightGridSlotKind kind)
    {
        switch (kind)
        {
            case PixelLightGridSlotKind.HollowSubtract:
                return new Color(0.2f, 0.85f, 0.45f, 0.5f);
            case PixelLightGridSlotKind.Door:
                return new Color(0.85f, 0.55f, 0.2f, 0.5f);
            default:
                return new Color(0.4f, 0.55f, 0.95f, 0.4f);
        }
    }

    static void HandleGridClick(
        PixelLightViewScopeSettings bag,
        PixelLightFrame frame,
        PixelLightMultiSlotCatalog catalog,
        SewingStitchProgram stitchProgram,
        PixelLightGridBrushKind brush,
        PixelLightPatternAsset pattern,
        int x,
        int y,
        PixelLightGridSlotEntry explodedHit)
    {
        ApplyBrushClick(bag, frame, x, y, brush);
        if (stitchProgram != null)
        {
            if (brush == PixelLightGridBrushKind.PaintOff)
                stitchProgram.RemoveAtCell(x, y);
            else if (brush == PixelLightGridBrushKind.PaintOn || brush == PixelLightGridBrushKind.GridSlot)
            {
                var step = stitchProgram.AddOrSelectAt(x, y);
                _stitchStepIndex = step.index;
            }
        }
        if (catalog != null && (brush == PixelLightGridBrushKind.GridSlot || explodedHit != null))
            ApplySlotSelection(catalog, x, y, explodedHit);
        EditorUtility.SetDirty(pattern);
        if (catalog != null)
            EditorUtility.SetDirty(catalog);
        Event.current.Use();
        if (EditorWindow.focusedWindow != null)
            EditorWindow.focusedWindow.Repaint();
    }

    static void ApplySlotSelection(
        PixelLightMultiSlotCatalog catalog, int x, int y, PixelLightGridSlotEntry explodedHit)
    {
        PixelLightSlotSelection.LastCellX = x;
        PixelLightSlotSelection.LastCellY = y;
        bool ctrl = Event.current.control || Event.current.command;
        bool shift = Event.current.shift;
        var at = catalog.SlotsAtCell(x, y);
        if (explodedHit != null && !shift)
        {
            if (ctrl)
            {
                if (!PixelLightSlotSelection.SelectedIds.Add(explodedHit.slotId))
                    PixelLightSlotSelection.SelectedIds.Remove(explodedHit.slotId);
            }
            else
            {
                PixelLightSlotSelection.SelectedIds.Clear();
                PixelLightSlotSelection.SelectedIds.Add(explodedHit.slotId);
            }
            PixelLightSlotSelection.FocusedSlotId = explodedHit.slotId;
            explodedHit.accordionExpanded = true;
            return;
        }
        if (!ctrl && !shift)
            PixelLightSlotSelection.SelectedIds.Clear();
        for (int i = 0; i < at.Count; i++)
        {
            if (at[i] == null) continue;
            if (ctrl && PixelLightSlotSelection.SelectedIds.Contains(at[i].slotId))
                PixelLightSlotSelection.SelectedIds.Remove(at[i].slotId);
            else
                PixelLightSlotSelection.SelectedIds.Add(at[i].slotId);
            at[i].accordionExpanded = true;
            PixelLightSlotSelection.FocusedSlotId = at[i].slotId;
        }
    }

    static void DrawSlotListbox(PixelLightMultiSlotCatalog catalog)
    {
        if (catalog == null) return;
        var rows = new List<PixelLightGridSlotEntry>();
        foreach (var id in PixelLightSlotSelection.SelectedIds)
        {
            var e = catalog.FindSlot(id);
            if (e != null) rows.Add(e);
        }
        if (rows.Count == 0)
            rows = catalog.SlotsAtCell(PixelLightSlotSelection.LastCellX, PixelLightSlotSelection.LastCellY);
        rows.Sort((a, b) => a.zIndex.CompareTo(b.zIndex));
        EditorGUILayout.LabelField(
            rows.Count == 0 ? "Slot selection (empty)" : "Slot selection (" + rows.Count + ")",
            EditorStyles.boldLabel);
        PixelLightSlotSelection.ListScroll = EditorGUILayout.BeginScrollView(
            PixelLightSlotSelection.ListScroll, GUILayout.MaxHeight(110f));
        for (int i = 0; i < rows.Count; i++)
        {
            var e = rows[i];
            bool focused = e.slotId == PixelLightSlotSelection.FocusedSlotId;
            var style = focused ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            string line = "z" + e.zIndex + "  " + e.label + "  " + e.kind + "  " + e.slotId
                          + "  (" + e.cellX + "," + e.cellY + ")";
            if (GUILayout.Button(line, style))
            {
                PixelLightSlotSelection.FocusedSlotId = e.slotId;
                e.accordionExpanded = true;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(PixelLightSlotSelection.FocusedSlotId)))
        {
            if (GUILayout.Button("Z Down") && catalog.MoveSlotZ(PixelLightSlotSelection.FocusedSlotId, -1))
                EditorUtility.SetDirty(catalog);
            if (GUILayout.Button("Z Up") && catalog.MoveSlotZ(PixelLightSlotSelection.FocusedSlotId, 1))
                EditorUtility.SetDirty(catalog);
            if (GUILayout.Button("Back"))
            {
                catalog.MoveSlotZToEdge(PixelLightSlotSelection.FocusedSlotId, false);
                EditorUtility.SetDirty(catalog);
            }
            if (GUILayout.Button("Front"))
            {
                catalog.MoveSlotZToEdge(PixelLightSlotSelection.FocusedSlotId, true);
                EditorUtility.SetDirty(catalog);
            }
        }
        EditorGUILayout.EndHorizontal();
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
