#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Magneto / helicopter designer — Property Config vs Requirements, placement, GPS portal.</summary>
public sealed class MagnetoHelicopterDesignerWindow : EditorWindow
{
    enum Tab
    {
        Overview = 0,
        PropertyConfig = 1,
        Requirements = 2,
        Turning = 3,
        Flapping = 4,
        Cabin = 5,
        Instruments = 6,
        PixelLight = 7,
        Placement = 8,
        CockpitGps = 9
    }

    Tab _tab;
    MagnetoHelicopterConfigurationAsset _asset;
    HelicopterVehicleRagdoll _heli;
    int _magnetoIndex;
    Vector2 _scroll;
    HelicoptorGridSlotGameObject _slot;
    static readonly string[] ViewNames = { "Top", "Front", "Back", "Left", "Right", "Bottom" };
    static readonly string[] PixelScopeNames = { "Airframe", "Magneto" };
    int _viewIndex;
    int _pixelScope;
    int _mountIndex;
    int _boundView = -1;
    int _boundScope = -1;
    int _boundMagneto = -1;
    PixelLightGridMountGameObject _selectedMount;
    PixelLightViewScopeSettings _activeViewScope;
    PixelLightMultiSlotCatalog _catalog;
    Vector2 _placementSlotsScroll;
    bool _frameScrubLivePreview = true;
    bool _frameScrubPausedRig;
    enum PixelBrushKind
    {
        PaintOn = 0,
        PaintOff = 1,
        FillSolid = 2,
        ChasePreset = 3,
        ClearFrame = 4,
        GridSlot = 5
    }

    [MenuItem("Locomotion/Magneto / Helicopter Designer")]
    public static void Open()
    {
        var w = GetWindow<MagnetoHelicopterDesignerWindow>();
        w.titleContent = new GUIContent("Magneto / Heli");
        w.Show();
    }

    void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, System.Enum.GetNames(typeof(Tab)));
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case Tab.Overview: DrawOverview(); break;
            case Tab.PropertyConfig: DrawPropertyConfig(); break;
            case Tab.Requirements: DrawRequirements(); break;
            case Tab.Turning: DrawTurning(); break;
            case Tab.Flapping: DrawFlapping(); break;
            case Tab.Cabin: DrawCabin(); break;
            case Tab.Instruments: DrawInstruments(); break;
            case Tab.PixelLight: DrawPixelLight(); break;
            case Tab.Placement: DrawPlacement(); break;
            case Tab.CockpitGps: DrawCockpitGps(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    List<MagnetoLiftParams> MagnetoList()
    {
        EnsureLists();
        if (_asset != null)
        {
            // Keep heli mirror in sync so Placement / PixelLight Magneto scope see the same count.
            if (_heli != null)
                _heli.magnetos = _asset.magnetos;
            return _asset.magnetos;
        }
        return _heli != null ? _heli.magnetos : null;
    }

    MagnetoLiftParams SelectedMagneto()
    {
        var list = MagnetoList();
        if (list == null || list.Count == 0) return null;
        _magnetoIndex = Mathf.Clamp(_magnetoIndex, 0, list.Count - 1);
        return list[_magnetoIndex];
    }

    void EnsureLists()
    {
        if (_asset != null) _asset.EnsureDefaults();
        if (_heli != null && (_heli.magnetos == null || _heli.magnetos.Count == 0))
            _heli.magnetos = new List<MagnetoLiftParams> { new MagnetoLiftParams { magnetoId = "main" } };
    }

    void MarkMagnetoHostsDirty()
    {
        if (_asset != null) EditorUtility.SetDirty(_asset);
        if (_heli != null) EditorUtility.SetDirty(_heli);
    }

    string NextMagnetoId(List<MagnetoLiftParams> list)
    {
        int n = list != null ? list.Count + 1 : 1;
        string id = "magneto_" + n;
        while (list != null && list.Exists(m => m != null && m.magnetoId == id))
        {
            n++;
            id = "magneto_" + n;
        }
        return id;
    }

    void DrawMagnetoListToolbar(List<MagnetoLiftParams> list)
    {
        if (list == null) return;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add magneto", GUILayout.Height(22)))
        {
            var src = SelectedMagneto();
            var added = src != null
                ? new MagnetoLiftParams
                {
                    magnetoId = NextMagnetoId(list),
                    aspectRatio = src.aspectRatio,
                    spanLength = src.spanLength,
                    bladeCount = src.bladeCount,
                    collectiveMinDeg = src.collectiveMinDeg,
                    collectiveMaxDeg = src.collectiveMaxDeg,
                    cyclicMaxDeg = src.cyclicMaxDeg,
                    rpmIdle = src.rpmIdle,
                    rpmMax = src.rpmMax,
                    tipTwistDeg = src.tipTwistDeg
                }
                : new MagnetoLiftParams { magnetoId = NextMagnetoId(list) };
            list.Add(added);
            _magnetoIndex = list.Count - 1;
            if (_heli != null && _asset == null)
                _heli.magnetos = list;
            else if (_asset != null && _heli != null)
                _heli.magnetos = _asset.magnetos;
            MarkMagnetoHostsDirty();
        }
        EditorGUI.BeginDisabledGroup(list.Count <= 1);
        if (GUILayout.Button("Remove magneto", GUILayout.Height(22)))
        {
            int idx = Mathf.Clamp(_magnetoIndex, 0, list.Count - 1);
            string label = list[idx] != null ? list[idx].magnetoId : ("#" + idx);
            if (EditorUtility.DisplayDialog(
                    "Remove magneto",
                    "Remove magneto '" + label + "' from the list?",
                    "Yes",
                    "No"))
            {
                list.RemoveAt(idx);
                _magnetoIndex = Mathf.Clamp(_magnetoIndex, 0, list.Count - 1);
                if (_asset != null && _heli != null)
                    _heli.magnetos = _asset.magnetos;
                MarkMagnetoHostsDirty();
            }
        }
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Duplicate", GUILayout.Height(22)))
        {
            var src = SelectedMagneto();
            if (src != null)
            {
                list.Insert(_magnetoIndex + 1, new MagnetoLiftParams
                {
                    magnetoId = NextMagnetoId(list),
                    aspectRatio = src.aspectRatio,
                    spanLength = src.spanLength,
                    bladeCount = src.bladeCount,
                    collectiveMinDeg = src.collectiveMinDeg,
                    collectiveMaxDeg = src.collectiveMaxDeg,
                    cyclicMaxDeg = src.cyclicMaxDeg,
                    rpmIdle = src.rpmIdle,
                    rpmMax = src.rpmMax,
                    tipTwistDeg = src.tipTwistDeg
                });
                _magnetoIndex++;
                if (_asset != null && _heli != null)
                    _heli.magnetos = _asset.magnetos;
                MarkMagnetoHostsDirty();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Magnetos: " + list.Count, EditorStyles.miniLabel);
    }

    void DrawOverview()
    {
        EditorGUILayout.LabelField("Overview / Save all", EditorStyles.boldLabel);
        _asset = (MagnetoHelicopterConfigurationAsset)EditorGUILayout.ObjectField(
            "Config asset", _asset, typeof(MagnetoHelicopterConfigurationAsset), false);
        _heli = (HelicopterVehicleRagdoll)EditorGUILayout.ObjectField(
            "Helicopter ragdoll", _heli, typeof(HelicopterVehicleRagdoll), true);
        if (_asset == null && GUILayout.Button("Create config asset"))
        {
            _asset = CreateInstance<MagnetoHelicopterConfigurationAsset>();
            _asset.EnsureDefaults();
            string path = EditorUtility.SaveFilePanelInProject("Magneto Heli Config", "MagnetoHelicopterConfiguration", "asset", "");
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.CreateAsset(_asset, path);
        }
        if (_asset != null)
        {
            _asset.craftName = EditorGUILayout.TextField("Craft name", _asset.craftName);
            _asset.callsign = EditorGUILayout.TextField("Callsign", _asset.callsign);
            _asset.prefabId = EditorGUILayout.TextField("Prefab id", _asset.prefabId);
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save all", GUILayout.Height(28)))
            SaveAll();
        if (GUILayout.Button("Save prefab", GUILayout.Height(28)))
            SavePrefabEnTotale();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Save prefab writes the current helicopter (config + magnetos + PixelLight catalog + mounts/slots) as a prefab asset.",
            MessageType.None);
    }

    void DrawPropertyConfig()
    {
        EditorGUILayout.LabelField("Property Config (always authoritative)", EditorStyles.boldLabel);
        var list = MagnetoList();
        if (list == null)
        {
            EditorGUILayout.HelpBox("Assign a config asset or helicopter on Overview.", MessageType.Info);
            return;
        }
        DrawMagnetoListToolbar(list);
        DrawMagnetoIndexPicker(list);
        var m = SelectedMagneto();
        if (m == null) return;
        EditorGUI.BeginChangeCheck();
        m.magnetoId = EditorGUILayout.TextField("Id", m.magnetoId);
        m.aspectRatio = EditorGUILayout.FloatField("Aspect ratio", m.aspectRatio);
        m.spanLength = EditorGUILayout.FloatField("Span / diameter", m.spanLength);
        m.bladeCount = EditorGUILayout.IntField("Blade count", m.bladeCount);
        m.collectiveMinDeg = EditorGUILayout.FloatField("Collective min", m.collectiveMinDeg);
        m.collectiveMaxDeg = EditorGUILayout.FloatField("Collective max", m.collectiveMaxDeg);
        m.cyclicMaxDeg = EditorGUILayout.FloatField("Cyclic max", m.cyclicMaxDeg);
        m.rpmIdle = EditorGUILayout.FloatField("RPM idle", m.rpmIdle);
        m.rpmMax = EditorGUILayout.FloatField("RPM max", m.rpmMax);
        m.tipTwistDeg = EditorGUILayout.FloatField("Tip twist", m.tipTwistDeg);
        EditorGUILayout.LabelField("Tip cache", m.tipEndPositionCache.ToString("F2"));
        if (GUILayout.Button("Recompute tip end cache"))
            m.RecomputeTipEndCache(_heli != null ? _heli.transform : null);
        m.RefreshEfficacyFromLastApplied();
        EditorGUILayout.LabelField("Efficacy 01", m.efficacy01.ToString("F2"));
        if (m.IsEfficacyLowered())
            EditorGUILayout.HelpBox("Efficacy lowered vs last Applied requirements.", MessageType.Warning);
        if (EditorGUI.EndChangeCheck())
            MarkMagnetoHostsDirty();
    }

    void DrawMagnetoIndexPicker(List<MagnetoLiftParams> list)
    {
        if (list == null || list.Count == 0) return;
        var labels = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            labels[i] = (m != null && !string.IsNullOrEmpty(m.magnetoId))
                ? i + ": " + m.magnetoId
                : i + ": (unnamed)";
        }
        _magnetoIndex = Mathf.Clamp(_magnetoIndex, 0, list.Count - 1);
        _magnetoIndex = EditorGUILayout.Popup("Magneto", _magnetoIndex, labels);
        if (list.Count > 1)
            _magnetoIndex = EditorGUILayout.IntSlider("Magneto index", _magnetoIndex, 0, list.Count - 1);
        else
            EditorGUILayout.LabelField("Magneto index", "0 (only magneto — use Add magneto for more)");
    }

    void DrawRequirements()
    {
        EditorGUILayout.LabelField("Requirements (does not mutate props until Apply)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit requirements below Property Config. Apply writes minimums into Property Config only.", MessageType.Info);
        var req = _asset != null ? _asset.requirements : _heli?.requirements;
        if (req == null) return;
        req.minLiftN = EditorGUILayout.FloatField("Min lift N", req.minLiftN);
        req.minClimbMs = EditorGUILayout.FloatField("Min climb m/s", req.minClimbMs);
        req.minYawRateDegPerSec = EditorGUILayout.FloatField("Min yaw deg/s", req.minYawRateDegPerSec);
        req.minDiskLoading = EditorGUILayout.FloatField("Min disk loading", req.minDiskLoading);
        req.designMassKg = EditorGUILayout.FloatField("Design mass kg", req.designMassKg);
        var m = SelectedMagneto();
        if (m != null)
        {
            m.RefreshEfficacyFromLastApplied();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current vs requirements", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Lift N", m.EstimateLiftN().ToString("F0") + " / " + req.minLiftN.ToString("F0"));
            EditorGUILayout.LabelField("Climb", m.EstimateClimbMs().ToString("F2") + " / " + req.minClimbMs.ToString("F2"));
            EditorGUILayout.LabelField("Efficacy", m.efficacy01.ToString("F2"));
            if (m.IsEfficacyLowered())
                EditorGUILayout.HelpBox("Property config is below last Applied requirement mins (efficacy lowered).", MessageType.Warning);
            if (GUILayout.Button("Apply minimums to Property Config"))
            {
                req.ApplyMinimumsTo(m);
                if (_asset != null) EditorUtility.SetDirty(_asset);
                if (_heli != null) EditorUtility.SetDirty(_heli);
            }
        }
    }

    void DrawTurning()
    {
        var m = SelectedMagneto();
        if (m == null) return;
        m.yawAuthority01 = EditorGUILayout.Slider("Yaw authority", m.yawAuthority01, 0f, 1f);
        m.tailRotorGain = EditorGUILayout.FloatField("Tail rotor gain", m.tailRotorGain);
        m.antiTorqueBias = EditorGUILayout.FloatField("Anti-torque bias", m.antiTorqueBias);
        EditorGUILayout.LabelField("Lemmas", HelicopterLemmaPropertyKeys.TailRudder + ", " + HelicopterLemmaPropertyKeys.MagnetoCyclic);
    }

    void DrawFlapping()
    {
        var m = SelectedMagneto();
        if (m == null) return;
        m.flappingEnabled = EditorGUILayout.Toggle("Flapping enabled", m.flappingEnabled);
        m.flapOpenCloseTopologyId = EditorGUILayout.TextField("Flap topology", m.flapOpenCloseTopologyId);
        m.wingletTurnDegMin = EditorGUILayout.FloatField("Winglet min", m.wingletTurnDegMin);
        m.wingletTurnDegMax = EditorGUILayout.FloatField("Winglet max", m.wingletTurnDegMax);
        m.wingletOpenCloseTopologyId = EditorGUILayout.TextField("Winglet topology", m.wingletOpenCloseTopologyId);
    }

    void DrawCabin()
    {
        if (_heli == null)
        {
            EditorGUILayout.HelpBox("Assign helicopter ragdoll.", MessageType.Info);
            return;
        }
        _heli.hasBathroom = EditorGUILayout.Toggle("Bathroom", _heli.hasBathroom);
        _heli.hasKitchen = EditorGUILayout.Toggle("Kitchen", _heli.hasKitchen);
        _heli.doorOpenCloseTopologyId = EditorGUILayout.TextField("Door topology", _heli.doorOpenCloseTopologyId);
        _heli.landingGearOpenCloseTopologyId = EditorGUILayout.TextField("Gear topology", _heli.landingGearOpenCloseTopologyId);
        _heli.standupSupportBars = EditorGUILayout.Toggle("Standup support bars", _heli.standupSupportBars);
        _heli.grabBarShape = (GrabBarShape)EditorGUILayout.EnumPopup("Grab bar shape", _heli.grabBarShape);
    }

    void DrawInstruments()
    {
        if (_heli == null) return;
        _heli.magnetoCollectiveSurfaceId = EditorGUILayout.TextField("Collective surface", _heli.magnetoCollectiveSurfaceId);
        _heli.magnetoCyclicSurfaceId = EditorGUILayout.TextField("Cyclic surface", _heli.magnetoCyclicSurfaceId);
        _heli.tailRudderSurfaceId = EditorGUILayout.TextField("Tail rudder surface", _heli.tailRudderSurfaceId);
        _heli.accelerationSurfaceId = EditorGUILayout.TextField("Acceleration surface", _heli.accelerationSurfaceId);
        _heli.airBrakeSurfaceId = EditorGUILayout.TextField("Air brake surface", _heli.airBrakeSurfaceId);
    }

    PixelLightMultiSlotCatalog EnsureCatalog()
    {
        if (_catalog == null && _heli != null)
            _catalog = _heli.pixelLightCatalog;
        if (_catalog == null && _asset != null)
            _catalog = _asset.pixelLightCatalog;
        if (_catalog == null)
        {
            EditorGUILayout.HelpBox("No PixelLightMultiSlotCatalog — create one to persist per view/scope settings.", MessageType.Warning);
            if (GUILayout.Button("Create PixelLight multi-slot catalog asset"))
            {
                var c = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
                string path = EditorUtility.SaveFilePanelInProject(
                    "PixelLight Multi Slot Catalog", "PixelLightMultiSlotCatalog", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(c, path);
                    _catalog = c;
                    if (_heli != null) _heli.pixelLightCatalog = c;
                    if (_asset != null) _asset.pixelLightCatalog = c;
                }
            }
        }
        return _catalog;
    }

    PixelLightViewScopeSettings BindViewScopeSettings()
    {
        var catalog = EnsureCatalog();
        if (catalog == null) return null;
        var view = (PixelLightDesignerView)_viewIndex;
        var scope = (PixelLightDesignerScope)_pixelScope;
        int mag = _pixelScope == 1 ? _magnetoIndex : 0;
        bool switched = _boundView != _viewIndex || _boundScope != _pixelScope || _boundMagneto != mag;
        if (switched)
        {
            // Previous bag was already mutated by the UI — do NOT CopyFromMount
            // (that would clobber per-view edits with whatever is currently on the mount).
            if (_activeViewScope != null)
                EditorUtility.SetDirty(catalog);
            _boundView = _viewIndex;
            _boundScope = _pixelScope;
            _boundMagneto = mag;
            _activeViewScope = catalog.GetOrCreate(view, scope, mag);
            if (_selectedMount != null)
                _activeViewScope.ApplyToMount(_selectedMount);
        }
        else
        {
            _activeViewScope = catalog.GetOrCreate(view, scope, mag);
        }
        return _activeViewScope;
    }

    void DrawPixelLight()
    {
        EditorGUILayout.LabelField("PixelLight — per view × scope settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _viewIndex = EditorGUILayout.Popup("View", _viewIndex, ViewNames);
        _pixelScope = EditorGUILayout.Popup("Scope", _pixelScope, PixelScopeNames);
        if (_pixelScope == 1)
        {
            var magList = MagnetoList();
            if (magList != null && magList.Count > 0)
            {
                DrawMagnetoIndexPicker(magList);
            }
            else
            {
                _magnetoIndex = EditorGUILayout.IntField("Magneto index", _magnetoIndex);
                EditorGUILayout.HelpBox("No magnetos yet — add them on Property Config.", MessageType.Info);
            }
        }
        EditorGUI.EndChangeCheck();

        _catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "Multi-slot catalog", _catalog ?? _heli?.pixelLightCatalog ?? _asset?.pixelLightCatalog,
            typeof(PixelLightMultiSlotCatalog), false);
        if (_heli != null) _heli.pixelLightCatalog = _catalog;
        if (_asset != null) _asset.pixelLightCatalog = _catalog;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Pixel Light Timed Designer", GUILayout.Height(24)))
            PixelLightTimedDesignerWindow.Open();
        if (GUILayout.Button("Open Airport Pixel Light Designer", GUILayout.Height(24)))
            AirportPixelLightDesignerWindow.Open();
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Open City Pixel Grid Designer"))
            EditorApplication.ExecuteMenuItem("Locomotion/City Pixel Grid Designer");

        if (_heli == null)
        {
            EditorGUILayout.HelpBox("Assign a HelicopterVehicleRagdoll on Overview to edit mounts.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mounts", EditorStyles.boldLabel);
        if (GUILayout.Button("Collect light mounts on heli"))
        {
            _heli.lightMounts = new List<PixelLightGridMountGameObject>(
                _heli.GetComponentsInChildren<PixelLightGridMountGameObject>());
            EnsureCatalog()?.SyncSlotsFromMounts(_heli.lightMounts);
        }
        if (_heli.lightMounts == null || _heli.lightMounts.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No mounts yet. Use Placement → Place PixelLight, or add PixelLightGridMountGameObject.",
                MessageType.Warning);
        }
        else
        {
            _mountIndex = Mathf.Clamp(_mountIndex, 0, _heli.lightMounts.Count - 1);
            _mountIndex = EditorGUILayout.IntSlider("Mount index", _mountIndex, 0, _heli.lightMounts.Count - 1);
            _selectedMount = _heli.lightMounts[_mountIndex];
        }

        _selectedMount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
            "Selected mount", _selectedMount, typeof(PixelLightGridMountGameObject), true);

        var vs = BindViewScopeSettings();
        if (vs == null) return;

        EditorGUILayout.HelpBox(
            $"Editing settings for View={ViewNames[_viewIndex]}, Scope={PixelScopeNames[_pixelScope]}"
            + (_pixelScope == 1 ? $", Magneto[{_magnetoIndex}]" : "")
            + " — changes save into this bag only.",
            MessageType.Info);

        if (_selectedMount == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Square / grid mount config (this view+scope)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        vs.gridWidth = EditorGUILayout.IntSlider("Mount grid width", vs.gridWidth, 1, 32);
        vs.gridHeight = EditorGUILayout.IntSlider("Mount grid height", vs.gridHeight, 1, 32);
        vs.cellSize = EditorGUILayout.FloatField("Cell size", vs.cellSize);
        vs.mountCellX = EditorGUILayout.IntField("Mount cell X", vs.mountCellX);
        vs.mountCellY = EditorGUILayout.IntField("Mount cell Y", vs.mountCellY);
        vs.fineOffset = EditorGUILayout.Vector3Field("Fine offset", vs.fineOffset);
        vs.snapToBake = EditorGUILayout.Toggle("Snap to bake", vs.snapToBake);
        vs.onlyActivateLightSource = EditorGUILayout.Toggle("Only activate light source", vs.onlyActivateLightSource);

        if (GUILayout.Button("Pull mount → this view+scope bag"))
        {
            vs.CopyFromMount(_selectedMount,
                _selectedMount.rig ?? _selectedMount.GetComponentInChildren<PixelLightRig>());
            EditorUtility.SetDirty(_catalog);
        }
        if (GUILayout.Button("Apply this view+scope to selected mount + EnsureRig"))
        {
            vs.ApplyToMount(_selectedMount);
            _selectedMount.EnsureRig();
            EditorUtility.SetDirty(_selectedMount);
            if (_catalog != null) EditorUtility.SetDirty(_catalog);
        }

        var rig = _selectedMount.rig ?? _selectedMount.GetComponentInChildren<PixelLightRig>();
        if (rig != null)
        {
            EditorGUILayout.ObjectField("Rig", rig, typeof(PixelLightRig), true);
            vs.rigGridWidth = EditorGUILayout.IntSlider("Rig grid width", vs.rigGridWidth, 2, 32);
            vs.rigGridHeight = EditorGUILayout.IntSlider("Rig grid height", vs.rigGridHeight, 1, 16);
            vs.stepMs = EditorGUILayout.FloatField("Step ms", vs.stepMs);
            vs.brightness01 = EditorGUILayout.Slider("Brightness", vs.brightness01, 0f, 1f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern / colors (this view+scope)", EditorStyles.boldLabel);
        vs.pattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField(
            "Pattern", vs.pattern, typeof(PixelLightPatternAsset), false);
        vs.colors = (PixelLightColorPackage)EditorGUILayout.ObjectField(
            "Colors", vs.colors, typeof(PixelLightColorPackage), false);
        if (EditorGUI.EndChangeCheck() && _catalog != null)
            EditorUtility.SetDirty(_catalog);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New chase pattern"))
        {
            var p = PixelLightPatternAsset.CreateChasePreset();
            string path = EditorUtility.SaveFilePanelInProject("Save Pattern", "HeliPixelLightChase", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(p, path);
                vs.pattern = p;
            }
        }
        if (GUILayout.Button("Apply pattern/colors to mount+rig") && vs.pattern != null)
        {
            vs.ApplyToMount(_selectedMount);
            EditorUtility.SetDirty(_selectedMount);
            if (_catalog != null) EditorUtility.SetDirty(_catalog);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brushes", EditorStyles.boldLabel);
        DrawBrushQuickButtons(vs);
        var brush = (PixelBrushKind)vs.brushKind;
        switch (brush)
        {
            case PixelBrushKind.PaintOn:
                EditorGUILayout.HelpBox("Click cells on the square grid below to paint ON.", MessageType.None);
                break;
            case PixelBrushKind.PaintOff:
                EditorGUILayout.HelpBox(
                    "Delete brush: click cells to paint OFF. Clicking a Grid Slot (G) asks to delete that slot.",
                    MessageType.None);
                break;
            case PixelBrushKind.GridSlot:
                EditorGUILayout.HelpBox(
                    "Click a cell to add a HelicoptorGridSlot at that X/Y. Cells with slots show G.",
                    MessageType.None);
                break;
            case PixelBrushKind.FillSolid:
                if (GUILayout.Button("Fill current frame solid ON") && vs.pattern != null)
                    FillCurrentFrame(vs.pattern, true, vs);
                break;
            case PixelBrushKind.ChasePreset:
                if (GUILayout.Button("Replace working pattern with chase preset"))
                    vs.pattern = PixelLightPatternAsset.CreateChasePreset();
                break;
            case PixelBrushKind.ClearFrame:
                if (GUILayout.Button("Clear current frame") && vs.pattern != null)
                    FillCurrentFrame(vs.pattern, false, vs);
                break;
        }

        EditorGUILayout.LabelField("Available city/airport brushes", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(
            string.Join(", ", System.Enum.GetNames(typeof(CityPixelBrushKind)))
            + "\nUse City Pixel / Airport Pixel Light designers for stamp brushes; this tab paints PixelLight pattern squares.",
            MessageType.Info);

        if (vs.pattern == null)
        {
            EditorGUILayout.HelpBox("Assign or create a PixelLightPatternAsset for this view+scope.", MessageType.Info);
            return;
        }

        vs.pattern.gridWidth = EditorGUILayout.IntSlider("Pattern width", vs.pattern.gridWidth, 2, 32);
        vs.pattern.gridHeight = EditorGUILayout.IntSlider("Pattern height", vs.pattern.gridHeight, 1, 16);
        vs.pattern.stepMs = EditorGUILayout.FloatField("Pattern step ms", vs.pattern.stepMs);
        if (vs.pattern.layers == null || vs.pattern.layers.Count == 0)
            vs.pattern.layers.Add(new PixelLightLayer());
        vs.paintLayer = Mathf.Clamp(vs.paintLayer, 0, vs.pattern.layers.Count - 1);
        var layer = vs.pattern.layers[vs.paintLayer];
        if (layer.frames == null || layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        vs.paintFrame = Mathf.Clamp(vs.paintFrame, 0, layer.frames.Count - 1);
        DrawFrameScrubber(vs, layer);

        EnsurePixelFrameSize(layer.frames[vs.paintFrame], vs.pattern.gridWidth, vs.pattern.gridHeight);
        DrawPixelSquareGrid(layer.frames[vs.paintFrame], vs.pattern.gridWidth, vs.pattern.gridHeight, brush);

        if (GUILayout.Button("Mark pattern + catalog dirty"))
        {
            EditorUtility.SetDirty(vs.pattern);
            if (_catalog != null) EditorUtility.SetDirty(_catalog);
        }
    }

    void DrawFrameScrubber(PixelLightViewScopeSettings vs, PixelLightLayer layer)
    {
        if (vs?.pattern == null || layer?.frames == null || layer.frames.Count == 0) return;

        int maxFrame = layer.frames.Count - 1;
        float stepMs = Mathf.Max(1f, vs.pattern.stepMs);
        float loopMs = Mathf.Max(stepMs, layer.frames.Count * stepMs);

        EditorGUILayout.LabelField("Frame scrubber", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◀", GUILayout.Width(28)))
            vs.paintFrame = Mathf.Max(0, vs.paintFrame - 1);
        if (GUILayout.Button("▶", GUILayout.Width(28)))
        {
            if (vs.paintFrame >= maxFrame)
                layer.frames.Add(new PixelLightFrame());
            maxFrame = layer.frames.Count - 1;
            vs.paintFrame = Mathf.Min(vs.paintFrame + 1, maxFrame);
        }
        if (GUILayout.Button("+ Frame", GUILayout.Width(60)))
        {
            layer.frames.Add(new PixelLightFrame());
            vs.paintFrame = layer.frames.Count - 1;
            maxFrame = vs.paintFrame;
            if (_catalog != null) EditorUtility.SetDirty(_catalog);
            if (vs.pattern != null) EditorUtility.SetDirty(vs.pattern);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        int frameFromSlider = EditorGUILayout.IntSlider(
            new GUIContent("Frame", "Scrub pattern frames by hand"),
            vs.paintFrame, 0, Mathf.Max(0, maxFrame));
        bool frameChanged = EditorGUI.EndChangeCheck();

        float timeAtFrame = vs.paintFrame * stepMs;
        EditorGUI.BeginChangeCheck();
        float timeFromSlider = EditorGUILayout.Slider(
            new GUIContent("Time (ms)", "Scrub design timing; snaps to frame steps"),
            timeAtFrame, 0f, Mathf.Max(0f, maxFrame * stepMs));
        bool timeChanged = EditorGUI.EndChangeCheck();

        if (timeChanged)
            vs.paintFrame = Mathf.Clamp(Mathf.RoundToInt(timeFromSlider / stepMs), 0, maxFrame);
        else if (frameChanged)
            vs.paintFrame = frameFromSlider;

        vs.paintFrame = Mathf.Clamp(vs.paintFrame, 0, Mathf.Max(0, layer.frames.Count - 1));
        float curMs = vs.paintFrame * stepMs;
        EditorGUILayout.LabelField(
            $"Timing  frame {vs.paintFrame}/{Mathf.Max(0, layer.frames.Count - 1)}  ·  {curMs:0} ms  ·  step {stepMs:0} ms  ·  loop {loopMs:0} ms",
            EditorStyles.miniLabel);

        // Mini range bar: draw tick marks so scrub position is obvious.
        var barRect = GUILayoutUtility.GetRect(18f, 14f);
        EditorGUI.DrawRect(barRect, new Color(0.18f, 0.18f, 0.2f));
        int ticks = layer.frames.Count;
        for (int i = 0; i < ticks; i++)
        {
            float u = ticks <= 1 ? 0f : i / (float)(ticks - 1);
            float x = barRect.x + u * barRect.width;
            var tick = new Rect(x - 1f, barRect.y, 2f, barRect.height);
            EditorGUI.DrawRect(tick, i == vs.paintFrame
                ? new Color(0.35f, 0.85f, 1f)
                : new Color(0.45f, 0.45f, 0.5f));
        }

        _frameScrubLivePreview = EditorGUILayout.ToggleLeft(
            "Live preview on selected mount (pause rig playback while scrubbing)",
            _frameScrubLivePreview);
        // Only push on scrub change — PushFrame every OnGUI event recreates textures and breaks IMGUI.
        if (_frameScrubLivePreview && (frameChanged || timeChanged))
            ApplyFrameScrubToMount(vs);
        if (_frameScrubPausedRig && GUILayout.Button("Resume mount rig playback"))
        {
            var rig = _selectedMount != null
                ? (_selectedMount.rig ?? _selectedMount.GetComponentInChildren<PixelLightRig>())
                : null;
            if (rig != null)
            {
                rig.playing = true;
                EditorUtility.SetDirty(rig);
            }
            _frameScrubPausedRig = false;
        }

        if (_catalog != null && (frameChanged || timeChanged))
            EditorUtility.SetDirty(_catalog);
    }

    void ApplyFrameScrubToMount(PixelLightViewScopeSettings vs)
    {
        if (!_frameScrubLivePreview || _selectedMount == null || vs?.pattern == null) return;
        var rig = _selectedMount.rig != null
            ? _selectedMount.rig
            : _selectedMount.GetComponentInChildren<PixelLightRig>();
        if (rig == null) return;
        try
        {
            if (rig.playing)
            {
                rig.playing = false;
                _frameScrubPausedRig = true;
            }
            // Always re-sync size from the edited pattern (same asset ref, mutated dimensions).
            rig.pattern = vs.pattern;
            rig.gridWidth = Mathf.Max(1, vs.pattern.gridWidth);
            rig.gridHeight = Mathf.Max(1, vs.pattern.gridHeight);
            rig.stepMs = vs.pattern.stepMs;
            rig.frameIndex = Mathf.Max(0, vs.paintFrame);
            rig.PushFrame();
            EditorUtility.SetDirty(rig);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Magneto/Heli] Live PixelLight scrub preview failed: " + ex.Message);
        }
    }

    void DrawBrushQuickButtons(PixelLightViewScopeSettings vs)
    {
        EditorGUILayout.BeginHorizontal();
        BrushToggle(vs, PixelBrushKind.PaintOn, "On");
        BrushToggle(vs, PixelBrushKind.PaintOff, "Delete");
        BrushToggle(vs, PixelBrushKind.GridSlot, "Grid Slot");
        BrushToggle(vs, PixelBrushKind.FillSolid, "Fill");
        BrushToggle(vs, PixelBrushKind.ChasePreset, "Chase");
        BrushToggle(vs, PixelBrushKind.ClearFrame, "Clear");
        EditorGUILayout.EndHorizontal();
        vs.brushKind = (int)(PixelBrushKind)EditorGUILayout.EnumPopup("Brush", (PixelBrushKind)vs.brushKind);
    }

    void BrushToggle(PixelLightViewScopeSettings vs, PixelBrushKind kind, string label)
    {
        bool on = (PixelBrushKind)vs.brushKind == kind;
        if (on) GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
        if (GUILayout.Button(label, EditorStyles.miniButton))
            vs.brushKind = (int)kind;
        GUI.backgroundColor = Color.white;
    }

    void DrawPixelSquareGrid(PixelLightFrame frame, int width, int height, PixelBrushKind brush)
    {
        float cell = 18f;
        var rect = GUILayoutUtility.GetRect(width * cell, height * cell);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f));
        var gStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        gStyle.normal.textColor = new Color(0.2f, 0.95f, 0.35f);
        for (int y = 0; y < height; y++)
        {
            string row = y < frame.rows.Count ? frame.rows[y] : "";
            for (int x = 0; x < width; x++)
            {
                var r = new Rect(rect.x + x * cell, rect.y + y * cell, cell - 1f, cell - 1f);
                bool hasSlot = HasGridSlotAt(x, y);
                bool on = x < row.Length && row[x] != ' ' && row[x] != '.' && row[x] != '_';
                Color fill = hasSlot
                    ? new Color(0.15f, 0.45f, 0.22f)
                    : (on ? new Color(1f, 0.25f, 0.2f) : new Color(0.35f, 0.35f, 0.38f));
                EditorGUI.DrawRect(r, fill);
                if (hasSlot)
                    GUI.Label(r, "G", gStyle);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    HandlePixelBrushClick(frame, x, y, brush, hasSlot);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
        GUI.Label(new Rect(rect.x, rect.yMax + 2f, rect.width, 18f), "PixelLight square (" + width + "×" + height + ")");
    }

    void HandlePixelBrushClick(PixelLightFrame frame, int x, int y, PixelBrushKind brush, bool hasSlot)
    {
        if (brush == PixelBrushKind.GridSlot)
        {
            PlaceGridSlotAt(x, y);
            return;
        }

        if (brush == PixelBrushKind.PaintOff)
        {
            if (hasSlot)
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Delete Grid Slot",
                    "Do you want to delete this grid slot?",
                    "Yes",
                    "No");
                if (ok)
                    DeleteGridSlotAt(x, y);
                return;
            }
            SetPixelCell(frame, x, y, false);
            MarkPixelArtDirty();
            return;
        }

        if (brush == PixelBrushKind.PaintOn || brush == PixelBrushKind.FillSolid
            || brush == PixelBrushKind.ChasePreset || brush == PixelBrushKind.ClearFrame)
        {
            // Click paint only for On; Fill/Chase/Clear use their toolbar actions.
            if (brush != PixelBrushKind.PaintOn) return;
            SetPixelCell(frame, x, y, true);
            MarkPixelArtDirty();
        }
    }

    void MarkPixelArtDirty()
    {
        if (_activeViewScope?.pattern != null)
            EditorUtility.SetDirty(_activeViewScope.pattern);
        if (_catalog != null)
            EditorUtility.SetDirty(_catalog);
    }

    bool HasGridSlotAt(int x, int y)
    {
        if (_heli != null)
        {
            // Prefer live children so destroyed/orphan list refs do not keep drawing G.
            var children = _heli.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
            for (int i = 0; i < children.Length; i++)
            {
                var s = children[i];
                if (s != null && s.cellX == x && s.cellY == y)
                    return true;
            }
            if (_heli.gridSlots != null)
            {
                for (int i = _heli.gridSlots.Count - 1; i >= 0; i--)
                {
                    var s = _heli.gridSlots[i];
                    if (s == null)
                    {
                        _heli.gridSlots.RemoveAt(i);
                        continue;
                    }
                    if (s.cellX == x && s.cellY == y)
                        return true;
                }
            }
        }
        if (_catalog != null && _catalog.gridSlots != null)
        {
            for (int i = _catalog.gridSlots.Count - 1; i >= 0; i--)
            {
                var e = _catalog.gridSlots[i];
                if (e == null)
                {
                    _catalog.gridSlots.RemoveAt(i);
                    continue;
                }
                // Orphan catalog rows with no scene slot: cell coords alone must not show G.
                if (e.heliSlot == null && e.mount == null)
                    continue;
                if (e.heliSlot != null && e.heliSlot.cellX == x && e.heliSlot.cellY == y)
                    return true;
                if (e.mount != null && e.mount.mountCellX == x && e.mount.mountCellY == y)
                    return true;
            }
        }
        return false;
    }

    void DeleteGridSlotAt(int x, int y)
    {
        var catalog = EnsureCatalog();
        bool removed = false;
        if (catalog != null)
            removed = catalog.RemoveSlotAtCell(x, y, _heli, null, destroySceneObjects: true);
        else if (_heli != null)
        {
            var children = _heli.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
            for (int i = 0; i < children.Length; i++)
            {
                var s = children[i];
                if (s == null || s.cellX != x || s.cellY != y) continue;
                if (_heli.gridSlots != null)
                    _heli.gridSlots.Remove(s);
                PixelLightMultiSlotCatalog.DestroyComponentOrEmptyHost(s, new[] { _heli.gameObject });
                removed = true;
            }
        }

        // Clear editor selection if it pointed at the destroyed cell (or Unity fake-null).
        if (_slot == null || (_slot.cellX == x && _slot.cellY == y))
            _slot = null;

        if (catalog != null) EditorUtility.SetDirty(catalog);
        if (_heli != null) EditorUtility.SetDirty(_heli);
        if (!removed)
            Debug.LogWarning($"[PixelLight] No grid slot found to delete at ({x},{y}).");
        Repaint();
    }

    void PlaceGridSlotAt(int x, int y)
    {
        if (_heli == null)
        {
            EditorUtility.DisplayDialog("PixelLight", "Assign a HelicopterVehicleRagdoll on Overview first.", "OK");
            return;
        }

        _heli.gridSlots ??= new List<HelicoptorGridSlotGameObject>();
        for (int i = 0; i < _heli.gridSlots.Count; i++)
        {
            var existing = _heli.gridSlots[i];
            if (existing != null && existing.cellX == x && existing.cellY == y)
            {
                _slot = existing;
                Selection.activeGameObject = existing.gameObject;
                return;
            }
        }

        int gw = _activeViewScope != null ? _activeViewScope.gridWidth : 8;
        int gh = _activeViewScope != null ? _activeViewScope.gridHeight : 8;
        float cellSize = _activeViewScope != null ? _activeViewScope.cellSize : 0.5f;

        var go = new GameObject("GridSlot_" + x + "_" + y);
        Undo.RegisterCreatedObjectUndo(go, "Add Grid Slot");
        go.transform.SetParent(_heli.transform, false);
        var slot = go.AddComponent<HelicoptorGridSlotGameObject>();
        slot.helicopter = _heli;
        slot.cellX = x;
        slot.cellY = y;
        slot.gridWidth = gw;
        slot.gridHeight = gh;
        slot.cellSize = cellSize;
        slot.contents = HelicoptorGridSlotGameObject.SlotContents.Empty;
        go.transform.localPosition = slot.CellLocalPosition(x, y);
        _heli.gridSlots.Add(slot);
        _slot = slot;

        var catalog = EnsureCatalog();
        if (catalog != null)
        {
            var entry = catalog.AddSlot("Grid " + x + "," + y);
            entry.cellX = x;
            entry.cellY = y;
            entry.heliSlot = slot;
            entry.contents = HelicoptorGridSlotGameObject.SlotContents.Empty;
            EditorUtility.SetDirty(catalog);
        }
        EditorUtility.SetDirty(_heli);
        Selection.activeGameObject = go;
    }

    static void EnsurePixelFrameSize(PixelLightFrame frame, int w, int h)
    {
        if (frame.rows == null)
            frame.rows = new List<string>();
        while (frame.rows.Count < h)
            frame.rows.Add(new string(' ', w));
        for (int y = 0; y < h; y++)
        {
            string row = frame.rows[y] ?? "";
            if (row.Length < w) row = row.PadRight(w, ' ');
            if (row.Length > w) row = row.Substring(0, w);
            frame.rows[y] = row;
        }
        while (frame.rows.Count > h)
            frame.rows.RemoveAt(frame.rows.Count - 1);
    }

    static void SetPixelCell(PixelLightFrame frame, int x, int y, bool on)
    {
        var chars = frame.rows[y].ToCharArray();
        chars[x] = on ? '#' : ' ';
        frame.rows[y] = new string(chars);
    }

    void FillCurrentFrame(PixelLightPatternAsset pattern, bool on, PixelLightViewScopeSettings vs)
    {
        if (pattern?.layers == null || pattern.layers.Count == 0 || vs == null) return;
        vs.paintLayer = Mathf.Clamp(vs.paintLayer, 0, pattern.layers.Count - 1);
        var layer = pattern.layers[vs.paintLayer];
        if (layer.frames == null || layer.frames.Count == 0) return;
        vs.paintFrame = Mathf.Clamp(vs.paintFrame, 0, layer.frames.Count - 1);
        var frame = layer.frames[vs.paintFrame];
        EnsurePixelFrameSize(frame, pattern.gridWidth, pattern.gridHeight);
        char fill = on ? '#' : ' ';
        for (int y = 0; y < pattern.gridHeight; y++)
            frame.rows[y] = new string(fill, pattern.gridWidth);
        EditorUtility.SetDirty(pattern);
        if (_catalog != null) EditorUtility.SetDirty(_catalog);
    }

    void DrawPlacement()
    {
        EditorGUILayout.LabelField("Placement — multi grid slots", EditorStyles.boldLabel);
        _catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "Multi-slot catalog", _catalog ?? _heli?.pixelLightCatalog ?? _asset?.pixelLightCatalog,
            typeof(PixelLightMultiSlotCatalog), false);
        if (_heli != null) _heli.pixelLightCatalog = _catalog;
        if (_asset != null) _asset.pixelLightCatalog = _catalog;
        EnsureCatalog();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid slots (scroll + accordion)", EditorStyles.boldLabel);
        PixelLightGridSlotAccordionDrawer.Draw(
            _catalog,
            ref _placementSlotsScroll,
            _heli,
            entry =>
            {
                if (entry?.heliSlot != null)
                    _slot = entry.heliSlot;
                if (entry?.mount != null)
                    _selectedMount = entry.mount;
            });

        if (_slot == null)
            _slot = null; // Unity fake-null after DestroyImmediate
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Active slot tools", EditorStyles.boldLabel);
        _slot = (HelicoptorGridSlotGameObject)EditorGUILayout.ObjectField(
            "Grid slot", _slot, typeof(HelicoptorGridSlotGameObject), true);
        if (_heli != null && _slot == null)
            _slot = _heli.GetComponentInChildren<HelicoptorGridSlotGameObject>();
        if (_slot == null && _heli != null && GUILayout.Button("Add HelicoptorGridSlot to heli"))
        {
            _slot = _heli.gameObject.AddComponent<HelicoptorGridSlotGameObject>();
            _slot.helicopter = _heli;
            EnsureCatalog()?.SyncSlotsFromHeli(_heli);
        }
        if (_slot == null) return;
        _slot.helicopter = _heli;
        _slot.cellX = EditorGUILayout.IntField("Cell X", _slot.cellX);
        _slot.cellY = EditorGUILayout.IntField("Cell Y", _slot.cellY);
        if (GUILayout.Button("Place magneto"))
        {
            _slot.PlaceMagneto(SelectedMagneto());
            EnsureCatalog()?.SyncSlotsFromHeli(_heli);
        }
        if (GUILayout.Button("Place PixelLight"))
        {
            _slot.PlacePixelLight();
            EnsureCatalog()?.SyncSlotsFromHeli(_heli);
            EnsureCatalog()?.SyncSlotsFromMounts(_heli.lightMounts);
        }
        if (GUILayout.Button("Place telecom + GPS webtop"))
        {
            _slot.PlaceTelecomGpsWebtop();
            EnsureCatalog()?.SyncSlotsFromHeli(_heli);
        }
        EditorGUILayout.LabelField("Contents", _slot.contents.ToString());
        if (_catalog != null && GUILayout.Button("Mark catalog dirty"))
            EditorUtility.SetDirty(_catalog);
    }

    void DrawCockpitGps()
    {
        if (_heli == null)
        {
            EditorGUILayout.HelpBox("Assign helicopter.", MessageType.Info);
            return;
        }
        _heli.EnsureSystems();
        _heli.gpsPortalId = EditorGUILayout.TextField("Portal id", _heli.gpsPortalId);
        _heli.defaultHudMode = (PilotGpsHudMode)EditorGUILayout.EnumPopup("HUD mode", _heli.defaultHudMode);
        _heli.webtopUrl = EditorGUILayout.TextField("Webtop URL", _heli.webtopUrl);
        if (_heli.gpsHud != null)
        {
            _heli.gpsHud.mode = _heli.defaultHudMode;
            _heli.gpsHud.travelAgent = (TravelAgent)EditorGUILayout.ObjectField(
                "TravelAgent", _heli.gpsHud.travelAgent, typeof(TravelAgent), true);
            _heli.gpsHud.bakeCache = (PilotGpsRouteBakeCache)EditorGUILayout.ObjectField(
                "Bake cache", _heli.gpsHud.bakeCache, typeof(PilotGpsRouteBakeCache), false);
        }
        if (_heli.renderPortal != null)
        {
            _heli.renderPortal.portalId = _heli.gpsPortalId;
            EditorGUILayout.LabelField("Portal has bounds", _heli.renderPortal.hasBounds.ToString());
        }
    }

    void SaveAll()
    {
        SyncConfigToHelicopter();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Magneto / Heli", "Saved.", "OK");
    }

    void SyncConfigToHelicopter()
    {
        if (_asset != null)
        {
            _asset.EnsureDefaults();
            if (_heli != null)
            {
                _asset.CaptureFrom(_heli);
                if (_catalog != null)
                    _asset.pixelLightCatalog = _catalog;
                _asset.ApplyTo(_heli);
            }
            EditorUtility.SetDirty(_asset);
        }
        else if (_heli != null)
        {
            _heli.EnsureSystems();
            if (_catalog != null)
                _heli.pixelLightCatalog = _catalog;
            EditorUtility.SetDirty(_heli);
        }
        if (_catalog != null)
            EditorUtility.SetDirty(_catalog);
        if (_heli != null)
            EditorUtility.SetDirty(_heli);
    }

    /// <summary>Flush designer state onto the craft and save the full GameObject hierarchy as a prefab.</summary>
    void SavePrefabEnTotale()
    {
        bool createdTemp = false;
        if (_heli == null)
        {
            if (_asset == null)
            {
                EditorUtility.DisplayDialog(
                    "Save prefab",
                    "Assign a HelicopterVehicleRagdoll in the scene, or a config asset to bake into a new craft.",
                    "OK");
                return;
            }
            var go = new GameObject(string.IsNullOrEmpty(_asset.craftName) ? "Helicopter" : _asset.craftName);
            Undo.RegisterCreatedObjectUndo(go, "Create Helicopter for Prefab");
            _heli = go.AddComponent<HelicopterVehicleRagdoll>();
            createdTemp = true;
        }

        SyncConfigToHelicopter();
        if (_asset != null)
            _asset.ApplyTo(_heli);
        else
            _heli.EnsureSystems();

        if (_catalog != null)
        {
            _heli.pixelLightCatalog = _catalog;
            _catalog.SyncSlotsFromHeli(_heli);
            if (_heli.lightMounts != null)
                _catalog.SyncSlotsFromMounts(_heli.lightMounts);
            EditorUtility.SetDirty(_catalog);
        }

        string defaultName = !string.IsNullOrEmpty(_asset?.prefabId)
            ? _asset.prefabId
            : (!string.IsNullOrEmpty(_heli.craftName) ? _heli.craftName : _heli.gameObject.name);
        defaultName = SanitizeFileName(defaultName);
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Helicopter Prefab",
            defaultName,
            "prefab",
            "Save the current helicopter configuration as a prefab (en totale).");
        if (string.IsNullOrEmpty(path))
        {
            if (createdTemp && _heli != null)
            {
                Undo.DestroyObjectImmediate(_heli.gameObject);
                _heli = null;
            }
            return;
        }

        var root = _heli.gameObject;
        // Always write the chosen path and keep the scene object connected to that prefab.
        if (PrefabUtility.IsPartOfPrefabInstance(root))
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
        GameObject saved = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);

        if (_asset != null)
        {
            string id = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(_asset.prefabId))
                _asset.prefabId = id;
            EditorUtility.SetDirty(_asset);
        }
        if (_heli != null)
        {
            _heli.configurationAsset = _asset;
            if (string.IsNullOrEmpty(_heli.prefabId))
                _heli.prefabId = System.IO.Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(_heli);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (saved != null)
        {
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            EditorUtility.DisplayDialog("Magneto / Heli", "Prefab saved:\n" + path, "OK");
        }
        else
            EditorUtility.DisplayDialog("Magneto / Heli", "Failed to save prefab at:\n" + path, "OK");
    }

    static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Helicopter";
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
#endif
