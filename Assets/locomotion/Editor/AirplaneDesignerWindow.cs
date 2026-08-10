#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Airplane Designer — Overview SaveAll, Construction, Aero, Power, PixelLight, Checklist, ATC Dialogue.</summary>
public sealed class AirplaneDesignerWindow : EditorWindow
{
    enum Tab
    {
        Overview = 0,
        Construction = 1,
        Aero = 2,
        Power = 3,
        PixelLight = 4,
        Checklist = 5,
        AtcDialogue = 6
    }

    Tab _tab;
    AirplaneConfigurationAsset _asset;
    AirplaneVehicleRagdoll _plane;
    Vector2 _scroll;
    int _mountIndex;
    int _jetIndex;
    int _batteryIndex;

    [MenuItem("Locomotion/Airplane Designer")]
    public static void Open()
    {
        var w = GetWindow<AirplaneDesignerWindow>();
        w.titleContent = new GUIContent("Airplane Designer");
        w.Show();
    }

    void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, System.Enum.GetNames(typeof(Tab)));
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case Tab.Overview: DrawOverview(); break;
            case Tab.Construction: DrawConstruction(); break;
            case Tab.Aero: DrawAero(); break;
            case Tab.Power: DrawPower(); break;
            case Tab.PixelLight: DrawPixelLight(); break;
            case Tab.Checklist: DrawChecklist(); break;
            case Tab.AtcDialogue: DrawAtcDialogue(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawOverview()
    {
        _asset = (AirplaneConfigurationAsset)EditorGUILayout.ObjectField(
            "Config asset", _asset, typeof(AirplaneConfigurationAsset), false);
        _plane = (AirplaneVehicleRagdoll)EditorGUILayout.ObjectField(
            "Airplane ragdoll", _plane, typeof(AirplaneVehicleRagdoll), true);

        EditorGUILayout.Space();
        if (_asset != null)
        {
            _asset.planeName = EditorGUILayout.TextField("Plane name", _asset.planeName);
            _asset.callsign = EditorGUILayout.TextField("Callsign", _asset.callsign);
            _asset.prefabId = EditorGUILayout.TextField("Prefab id", _asset.prefabId);
            _asset.fuelTankCapacity = EditorGUILayout.FloatField("Fuel capacity", _asset.fuelTankCapacity);
            _asset.fuelStart = EditorGUILayout.FloatField("Fuel start", _asset.fuelStart);
            _asset.parentKitchenCompanyId = EditorGUILayout.TextField("Parent kitchen company", _asset.parentKitchenCompanyId);
            _asset.defaultDestinationAtcServiceId = EditorGUILayout.TextField(
                "Default destination ATC", _asset.defaultDestinationAtcServiceId);
            _asset.insertLandingQueue = EditorGUILayout.Toggle("Insert landing queue", _asset.insertLandingQueue);
            _asset.insertRefuelBeforePark = EditorGUILayout.Toggle("Insert refuel before park", _asset.insertRefuelBeforePark);
            _asset.refuelFuelThreshold01 = EditorGUILayout.Slider("Refuel fuel threshold", _asset.refuelFuelThreshold01, 0f, 1f);
        }
        else if (_plane != null)
        {
            _plane.planeName = EditorGUILayout.TextField("Plane name", _plane.planeName);
            _plane.callsign = EditorGUILayout.TextField("Callsign", _plane.callsign);
            _plane.fuel01 = EditorGUILayout.Slider("Fuel 01", _plane.fuel01, 0f, 1f);
            _plane.defaultDestinationAtcServiceId = EditorGUILayout.TextField(
                "Default destination ATC", _plane.defaultDestinationAtcServiceId);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save all"))
                SaveAll();
            if (GUILayout.Button("Pull from plane") && _asset != null && _plane != null)
            {
                _asset.CaptureFrom(_plane);
                EditorUtility.SetDirty(_asset);
            }
            if (GUILayout.Button("Create config asset"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Airplane Configuration", "AirplaneConfiguration", "asset", "Create airplane config");
                if (!string.IsNullOrEmpty(path))
                {
                    var a = CreateInstance<AirplaneConfigurationAsset>();
                    a.EnsureDefaults();
                    AssetDatabase.CreateAsset(a, path);
                    _asset = a;
                }
            }
        }
    }

    void DrawConstruction()
    {
        var left = ActiveLeftWing();
        var right = ActiveRightWing();
        var hTail = ActiveHTail();
        var vTail = ActiveVTail();
        EditorGUILayout.LabelField("Wings / tail", EditorStyles.boldLabel);
        DrawWing("Left wing", left);
        DrawWing("Right wing", right);
        DrawWing("Horizontal tail", hTail);
        DrawWing("Vertical tail", vTail);
        if (GUILayout.Button("Recompute tip end caches"))
        {
            left?.RecomputeTipEndCache(_plane != null ? _plane.transform : null);
            right?.RecomputeTipEndCache(_plane != null ? _plane.transform : null);
            hTail?.RecomputeTipEndCache(_plane != null ? _plane.transform : null);
            vTail?.RecomputeTipEndCache(_plane != null ? _plane.transform : null);
            MarkDirty();
        }

        string nose = _asset != null ? _asset.noseOpenCloseTopologyId : (_plane != null ? _plane.noseOpenCloseTopologyId : "");
        nose = EditorGUILayout.TextField("Nose open/close topology", nose);
        if (_asset != null) _asset.noseOpenCloseTopologyId = nose;
        else if (_plane != null) _plane.noseOpenCloseTopologyId = nose;

        string gear = _asset != null ? _asset.landingGearOpenCloseTopologyId : (_plane != null ? _plane.landingGearOpenCloseTopologyId : "");
        gear = EditorGUILayout.TextField("Landing gear topology", gear);
        if (_asset != null) _asset.landingGearOpenCloseTopologyId = gear;
        else if (_plane != null) _plane.landingGearOpenCloseTopologyId = gear;
    }

    void DrawWing(string label, AirplaneWingSurfaceParams wing)
    {
        if (wing == null) return;
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        wing.surfaceId = EditorGUILayout.TextField("Id", wing.surfaceId);
        wing.aspectRatio = EditorGUILayout.FloatField("Aspect ratio", wing.aspectRatio);
        wing.spanLength = EditorGUILayout.FloatField("Span", wing.spanLength);
        wing.leadingEdgeAoADeg = EditorGUILayout.FloatField("LE AoA deg", wing.leadingEdgeAoADeg);
        wing.trailingEdgeSweepDeg = EditorGUILayout.FloatField("TE sweep deg", wing.trailingEdgeSweepDeg);
        wing.tipTwistDeg = EditorGUILayout.FloatField("Tip twist", wing.tipTwistDeg);
        wing.centerlineLocalPos = EditorGUILayout.Vector3Field("Centerline local", wing.centerlineLocalPos);
        wing.centerlineAngleDeg = EditorGUILayout.FloatField("Centerline angle", wing.centerlineAngleDeg);
        EditorGUILayout.Vector3Field("Tip cache (read)", wing.tipEndPositionCache);
    }

    void DrawAero()
    {
        var egg = ActiveEllipsoid();
        if (egg == null) return;
        EditorGUILayout.LabelField("Fuselage ellipsoid", EditorStyles.boldLabel);
        egg.centerLocal = EditorGUILayout.Vector3Field("Center local", egg.centerLocal);
        egg.radii = EditorGUILayout.Vector3Field("Radii", egg.radii);
        egg.rotationEuler = EditorGUILayout.Vector3Field("Rotation euler", egg.rotationEuler);
        egg.conicalNozzleLength = EditorGUILayout.FloatField("Nozzle length", egg.conicalNozzleLength);
        egg.conicalNozzleHalfAngleDeg = EditorGUILayout.FloatField("Nozzle half-angle", egg.conicalNozzleHalfAngleDeg);
        egg.affineLiftDelta = EditorGUILayout.Vector3Field("Affine lift delta", egg.affineLiftDelta);
        egg.affineDragDelta = EditorGUILayout.Vector3Field("Affine drag delta", egg.affineDragDelta);
        egg.thrustSlotMultiplier = EditorGUILayout.FloatField("Thrust slot mult", egg.thrustSlotMultiplier);

        var jets = ActiveJets();
        if (jets == null) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Jets", EditorStyles.boldLabel);
        if (jets.Count == 0) jets.Add(new AirplaneJetEngineParams());
        _jetIndex = Mathf.Clamp(_jetIndex, 0, jets.Count - 1);
        _jetIndex = EditorGUILayout.IntSlider("Jet index", _jetIndex, 0, jets.Count - 1);
        var jet = jets[_jetIndex];
        jet.engineId = EditorGUILayout.TextField("Engine id", jet.engineId);
        jet.localPosition = EditorGUILayout.Vector3Field("Local pos", jet.localPosition);
        jet.localEuler = EditorGUILayout.Vector3Field("Local euler", jet.localEuler);
        jet.thrustN = EditorGUILayout.FloatField("Thrust N", jet.thrustN);
        jet.gooseContentsId = EditorGUILayout.TextField("Goose contents", jet.gooseContentsId);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add jet")) jets.Add(new AirplaneJetEngineParams { engineId = "jet_" + jets.Count });
            if (GUILayout.Button("Remove jet") && jets.Count > 1) jets.RemoveAt(_jetIndex);
        }
        if (_plane != null && GUILayout.Button("Apply affine to weather bridge"))
        {
            _plane.EnsureSystems();
            _plane.weatherAeroBridge?.ApplyAffineFromEllipsoid();
        }
        MarkDirty();
    }

    void DrawPower()
    {
        if (_plane == null && _asset == null)
        {
            EditorGUILayout.HelpBox("Assign plane or config asset.", MessageType.Info);
            return;
        }

        var packs = _asset != null ? _asset.batteries : _plane.batteries;
        var systems = _asset != null ? _asset.powerSystems : _plane.powerSystems;
        if (packs == null) return;
        if (packs.Count == 0) packs.Add(new AirplaneBatteryPack());
        _batteryIndex = Mathf.Clamp(_batteryIndex, 0, packs.Count - 1);
        var pack = packs[_batteryIndex];
        EditorGUILayout.LabelField("Battery", EditorStyles.boldLabel);
        pack.packId = EditorGUILayout.TextField("Pack id", pack.packId);
        pack.capacityKwh = EditorGUILayout.FloatField("Capacity kWh", pack.capacityKwh);
        pack.chargeKwh = EditorGUILayout.FloatField("Charge kWh", pack.chargeKwh);
        pack.maxDrawKw = EditorGUILayout.FloatField("Max draw kW", pack.maxDrawKw);
        pack.criticalCharge01 = EditorGUILayout.Slider("Critical charge", pack.criticalCharge01, 0f, 1f);

        if (_asset != null)
            _asset.chargeKwWhenEnginesOn = EditorGUILayout.FloatField("Charge kW engines on", _asset.chargeKwWhenEnginesOn);
        else
            _plane.chargeKwWhenEnginesOn = EditorGUILayout.FloatField("Charge kW engines on", _plane.chargeKwWhenEnginesOn);

        int outlets = _asset != null ? _asset.seatPowerOutletCount : _plane.seatPowerOutletCount;
        outlets = EditorGUILayout.IntField("Seat outlets", outlets);
        if (_asset != null) _asset.seatPowerOutletCount = outlets;
        else _plane.seatPowerOutletCount = outlets;

        int seatbacks = _asset != null ? _asset.seatbackWebtopCount : _plane.seatbackWebtopCount;
        seatbacks = EditorGUILayout.IntField("Seatback webtops", seatbacks);
        if (_asset != null) _asset.seatbackWebtopCount = seatbacks;
        else _plane.seatbackWebtopCount = seatbacks;

        var music = _asset != null ? _asset.defaultMusicSource : _plane.defaultMusicSource;
        music = (AirplaneCabinMusicSource)EditorGUILayout.EnumPopup("Cabin music source", music);
        if (_asset != null) _asset.defaultMusicSource = music;
        else
        {
            _plane.defaultMusicSource = music;
            _plane.EnsureSystems();
            if (GUILayout.Button("Apply music source now"))
                _plane.cabinMusicSystem?.SetMusicSource(music);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Power systems", EditorStyles.boldLabel);
        if (systems != null)
        {
            for (int i = 0; i < systems.Count; i++)
            {
                var s = systems[i];
                if (s == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    s.enabled = EditorGUILayout.Toggle(s.enabled, GUILayout.Width(18));
                    s.systemId = EditorGUILayout.TextField(s.systemId, GUILayout.Width(140));
                    s.drawKwWhenOn = EditorGUILayout.FloatField(s.drawKwWhenOn, GUILayout.Width(60));
                    s.shedPriority = EditorGUILayout.IntField(s.shedPriority, GUILayout.Width(40));
                    EditorGUILayout.LabelField(s.label);
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Ensure default systems") && systems != null)
            {
                AirplanePowerBus.FillDefaultPowerSystems(systems);
                MarkDirty();
            }
            if (GUILayout.Button("Reset default priorities") && systems != null)
            {
                var tmp = new List<AirplanePowerSystemDraw>();
                AirplanePowerBus.FillDefaultPowerSystems(tmp);
                for (int i = 0; i < systems.Count; i++)
                {
                    if (systems[i] == null) continue;
                    var match = tmp.Find(t => t.systemId == systems[i].systemId);
                    if (match != null) systems[i].shedPriority = match.shedPriority;
                }
                MarkDirty();
            }
        }

        if (_plane?.airplaneBio?.powerBus != null)
        {
            var bus = _plane.airplaneBio.powerBus;
            EditorGUILayout.HelpBox(
                $"Live total draw {bus.totalDrawKw:0.00} kW · charge {bus.charge01:0%} · shedding={bus.shedding}",
                MessageType.None);
        }
    }

    void DrawPixelLight()
    {
        if (_plane == null)
        {
            EditorGUILayout.HelpBox("Assign airplane ragdoll for mounts.", MessageType.Info);
            return;
        }
        if (_plane.lightMounts == null)
            _plane.lightMounts = new List<PixelLightGridMountGameObject>();
        EditorGUILayout.LabelField("PixelLight mounts (6-view closest-first via Airport PixelLight Designer)", EditorStyles.wordWrappedLabel);
        if (_plane.lightMounts.Count == 0)
            EditorGUILayout.HelpBox("No mounts — Ensure on a mount or open Airport PixelLight Designer.", MessageType.Warning);
        else
        {
            _mountIndex = Mathf.Clamp(_mountIndex, 0, _plane.lightMounts.Count - 1);
            _mountIndex = EditorGUILayout.IntSlider("Mount", _mountIndex, 0, _plane.lightMounts.Count - 1);
            var mount = _plane.lightMounts[_mountIndex];
            EditorGUILayout.ObjectField("Mount", mount, typeof(PixelLightGridMountGameObject), true);
            if (mount != null && GUILayout.Button("Ensure rig"))
            {
                mount.EnsureRig();
                EditorUtility.SetDirty(mount);
            }
        }
        if (GUILayout.Button("Add mount on plane"))
        {
            var go = new GameObject("PixelLightMount");
            go.transform.SetParent(_plane.transform, false);
            var m = go.AddComponent<PixelLightGridMountGameObject>();
            _plane.lightMounts.Add(m);
            EditorUtility.SetDirty(_plane);
        }
        if (GUILayout.Button("Open Airport PixelLight Designer"))
            EditorApplication.ExecuteMenuItem("Locomotion/Airport Pixel Light Designer");
    }

    void DrawChecklist()
    {
        TSAChecklistCard card = _asset != null ? _asset.checklistTemplate : _plane?.checklistTemplate;
        if (card == null)
        {
            if (GUILayout.Button("Generate checklist template"))
            {
                var gen = TSAChecklistCard.Generate(null);
                if (_asset != null) _asset.checklistTemplate = gen;
                else if (_plane != null) _plane.checklistTemplate = gen;
            }
            return;
        }
        if (GUILayout.Button("Regenerate defaults"))
            card.FillDefaults();
        if (card.items != null)
        {
            for (int i = 0; i < card.items.Count; i++)
            {
                var it = card.items[i];
                if (it == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    it.required = EditorGUILayout.Toggle(it.required, GUILayout.Width(18));
                    it.id = EditorGUILayout.TextField(it.id, GUILayout.Width(120));
                    it.label = EditorGUILayout.TextField(it.label);
                }
            }
        }
        MarkDirty();
    }

    void DrawAtcDialogue()
    {
        var catalog = _asset != null ? _asset.dialogueCatalog : _plane?.dialogueCatalog;
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("No dialogue catalog.", MessageType.Info);
            return;
        }
        catalog.EnsureDefaults();
        for (int i = 0; i < catalog.entries.Count; i++)
        {
            var e = catalog.entries[i];
            if (e == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                e.dispatchKind = EditorGUILayout.TextField(e.dispatchKind, GUILayout.Width(140));
                e.dialogueSetId = EditorGUILayout.TextField(e.dialogueSetId);
                e.goalName = EditorGUILayout.TextField(e.goalName, GUILayout.Width(80));
            }
        }
        MarkDirty();
    }

    AirplaneWingSurfaceParams ActiveLeftWing() =>
        _asset != null ? _asset.leftWing : _plane?.leftWing;
    AirplaneWingSurfaceParams ActiveRightWing() =>
        _asset != null ? _asset.rightWing : _plane?.rightWing;
    AirplaneWingSurfaceParams ActiveHTail() =>
        _asset != null ? _asset.horizontalTail : _plane?.horizontalTail;
    AirplaneWingSurfaceParams ActiveVTail() =>
        _asset != null ? _asset.verticalTail : _plane?.verticalTail;
    AirplaneEllipsoidAeroParams ActiveEllipsoid() =>
        _asset != null ? _asset.fuselageEllipsoid : _plane?.fuselageEllipsoid;
    List<AirplaneJetEngineParams> ActiveJets() =>
        _asset != null ? _asset.jets : _plane?.jets;

    void SaveAll()
    {
        if (_asset != null)
        {
            _asset.EnsureDefaults();
            if (_plane != null)
            {
                _asset.CaptureFrom(_plane);
                _asset.ApplyTo(_plane);
            }
            EditorUtility.SetDirty(_asset);
        }
        else if (_plane != null)
        {
            _plane.EnsureSystems();
            EditorUtility.SetDirty(_plane);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Airplane Designer", "Saved.", "OK");
    }

    void MarkDirty()
    {
        if (_asset != null) EditorUtility.SetDirty(_asset);
        if (_plane != null) EditorUtility.SetDirty(_plane);
    }
}
#endif
