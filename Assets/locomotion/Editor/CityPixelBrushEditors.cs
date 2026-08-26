using System;
using UnityEditor;
using UnityEngine;

/// <summary>Shared brush option drawers and Open Available Editors mapping.</summary>
public static class CityPixelBrushEditors
{
    public static void DrawBrushOptions(CityPixelBrushKind kind, ref CityPixelBrushStamp stamp)
    {
        if (kind == CityPixelBrushKind.Select)
        {
            EditorGUILayout.HelpBox(
                "Click a cell or drag a rectangle. Shift adds. Ctrl/Cmd toggles. Esc clears. Paint/Erase Selected applies the current tool to the selection.",
                MessageType.None);
            return;
        }

        if (stamp == null) stamp = new CityPixelBrushStamp { kind = kind };
        stamp.kind = kind;
        stamp.yawDegrees = EditorGUILayout.FloatField("Yaw Degrees", stamp.yawDegrees);

        switch (kind)
        {
            case CityPixelBrushKind.PoliceDetail:
                stamp.ladderAsset = (TrafficDetailLadderAsset)EditorGUILayout.ObjectField(
                    "Ladder", stamp.ladderAsset, typeof(TrafficDetailLadderAsset), false);
                if (GUILayout.Button("Open LadderLogic Editor"))
                    LadderLogicDesignerWindow.OpenWith(stamp.ladderAsset);
                break;

            case CityPixelBrushKind.OneWay:
                stamp.yawDegrees = EditorGUILayout.Slider("Direction Yaw", stamp.yawDegrees, 0f, 360f);
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Sign Prefab", stamp.signPrefab, typeof(GameObject), false);
                DrawCardinalButtons(ref stamp.yawDegrees);
                break;

            case CityPixelBrushKind.Detour:
                stamp.placementPrompt = EditorGUILayout.TextField("Placement Prompt", stamp.placementPrompt ?? "");
                stamp.detourGoalCellX = EditorGUILayout.IntField("Detour Goal Cell X", stamp.detourGoalCellX);
                stamp.detourGoalCellY = EditorGUILayout.IntField("Detour Goal Cell Y", stamp.detourGoalCellY);
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Primary Sign", stamp.signPrefab, typeof(GameObject), false);
                EditorGUILayout.LabelField("Signage Prefabs (array on stamp asset via inspector)");
                break;

            case CityPixelBrushKind.StopSign:
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Stop Sign Prefab", stamp.signPrefab, typeof(GameObject), false);
                stamp.yawDegrees = EditorGUILayout.FloatField("Approach Heading", stamp.yawDegrees);
                stamp.stopPotential01 = EditorGUILayout.Slider("Stop Potential", stamp.stopPotential01, 0f, 2f);
                break;

            case CityPixelBrushKind.Intersection:
                stamp.pixelLightPattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField(
                    "PixelLight Pattern", stamp.pixelLightPattern, typeof(PixelLightPatternAsset), false);
                stamp.pixelLightColors = (PixelLightColorPackage)EditorGUILayout.ObjectField(
                    "PixelLight Colors", stamp.pixelLightColors, typeof(PixelLightColorPackage), false);
                stamp.ladderAsset = (TrafficDetailLadderAsset)EditorGUILayout.ObjectField(
                    "Detail Ladder", stamp.ladderAsset, typeof(TrafficDetailLadderAsset), false);
                DrawPlaceableStampFields(ref stamp);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Pixel Light Designer"))
                    PixelLightTimedDesignerWindow.Open();
                if (GUILayout.Button("Open LadderLogic Editor"))
                    LadderLogicDesignerWindow.OpenWith(stamp.ladderAsset);
                EditorGUILayout.EndHorizontal();
                break;

            case CityPixelBrushKind.SchoolBusStop:
                stamp.stopRadius = EditorGUILayout.FloatField("Stop Radius", stamp.stopRadius);
                stamp.scheduleCron = EditorGUILayout.TextField("Schedule Cron", stamp.scheduleCron ?? "");
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Bus / Stop Prefab", stamp.signPrefab, typeof(GameObject), false);
                DrawPlaceableStampFields(ref stamp);
                break;

            case CityPixelBrushKind.Building:
                stamp.buildingKind = (CivilSystemKind)EditorGUILayout.EnumPopup("Building Kind", stamp.buildingKind);
                stamp.buildingTypeId = EditorGUILayout.TextField("Building Type Id", stamp.buildingTypeId ?? "");
                stamp.typeKey = EditorGUILayout.TextField("Type Key", stamp.typeKey ?? "");
                stamp.buildingConfig = (BuildingRequirementSpec)EditorGUILayout.ObjectField(
                    "Building Config", stamp.buildingConfig, typeof(BuildingRequirementSpec), false);
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Building Prefab", stamp.signPrefab, typeof(GameObject), false);
                DrawPlaceableStampFields(ref stamp);
                stamp.floorIndex = EditorGUILayout.IntField("Floor Index", stamp.floorIndex);
                stamp.zoneId = EditorGUILayout.TextField("Zone Id", stamp.zoneId ?? "");
                stamp.floorPlanIndexMap = (FloorPlanIndexMap)EditorGUILayout.ObjectField(
                    "Floor Plan Override", stamp.floorPlanIndexMap, typeof(FloorPlanIndexMap), false);
                if (GUILayout.Button("Open Available Editors"))
                    OpenAvailableEditors(stamp.buildingKind);
                break;

            case CityPixelBrushKind.Sign:
                stamp.signKind = (TASignKind)EditorGUILayout.EnumPopup("Sign Kind", stamp.signKind);
                stamp.avoidCostMultiplier = EditorGUILayout.FloatField("Avoid Cost Mult", stamp.avoidCostMultiplier);
                stamp.slowRadius = EditorGUILayout.FloatField("Slow Radius", stamp.slowRadius);
                stamp.stopPotential01 = EditorGUILayout.Slider("Stop Potential", stamp.stopPotential01, 0f, 2f);
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Sign Prefab", stamp.signPrefab, typeof(GameObject), false);
                break;

            case CityPixelBrushKind.BuildingTypeSeparator:
            case CityPixelBrushKind.IntersectionTypeSeparator:
            case CityPixelBrushKind.PlaceableTypeSeparator:
                EditorGUILayout.HelpBox(
                    "Separator stamps cut same-type adjacency (wall cells). Incomplete cuts warn and keep one chunk.",
                    MessageType.Info);
                break;

            case CityPixelBrushKind.Cell:
                stamp.buildingKind = CivilSystemKind.Prison;
                stamp.buildingTypeId = EditorGUILayout.TextField("Building Type Id", stamp.buildingTypeId ?? "prison");
                stamp.diggable = EditorGUILayout.Toggle("Diggable Wall", stamp.diggable);
                stamp.wallDestructible = EditorGUILayout.Toggle("Destructible", stamp.wallDestructible);
                stamp.tunnelStress01 = EditorGUILayout.Slider("Tunnel Stress", stamp.tunnelStress01, 0f, 1f);
                stamp.buildingConfig = (BuildingRequirementSpec)EditorGUILayout.ObjectField(
                    "Building Config", stamp.buildingConfig, typeof(BuildingRequirementSpec), false);
                DrawPlaceableStampFields(ref stamp);
                break;

            case CityPixelBrushKind.DrivewayLot:
            case CityPixelBrushKind.GarageLot:
                stamp.typeKey = EditorGUILayout.TextField("Lot Type Key", stamp.typeKey ?? (kind == CityPixelBrushKind.GarageLot ? "garage" : "driveway"));
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Lot Prefab", stamp.signPrefab, typeof(GameObject), false);
                EditorGUILayout.HelpBox(
                    kind == CityPixelBrushKind.GarageLot
                        ? "Garage lot must 4-touch driveway, street, or sidewalk."
                        : "Driveway lot must 4-touch street or sidewalk.",
                    MessageType.Info);
                DrawPlaceableStampFields(ref stamp);
                break;

            case CityPixelBrushKind.RoadLanes:
            case CityPixelBrushKind.Overpass:
            case CityPixelBrushKind.Bridge:
            case CityPixelBrushKind.BridgeAndUnderpass:
                stamp.laneConfig = (RoadLaneConfigAsset)EditorGUILayout.ObjectField(
                    "Lane Config", stamp.laneConfig, typeof(RoadLaneConfigAsset), false);
                stamp.diggable = EditorGUILayout.Toggle("Diggable", stamp.diggable);
                break;
            case CityPixelBrushKind.StreetLight:
            case CityPixelBrushKind.TrafficSignal:
            case CityPixelBrushKind.PhonePole:
                stamp.streetLightKind = (StreetLightKind)EditorGUILayout.EnumPopup("Kind", stamp.streetLightKind);
                stamp.shoulderSign = EditorGUILayout.IntSlider("Shoulder", stamp.shoulderSign, -1, 1);
                stamp.spacingAlongM = EditorGUILayout.FloatField("Spacing Along", stamp.spacingAlongM);
                stamp.approachId = EditorGUILayout.TextField("Approach Id", stamp.approachId ?? "main");
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", stamp.signPrefab, typeof(GameObject), false);
                stamp.pixelLightPattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField(
                    "PixelLight Pattern", stamp.pixelLightPattern, typeof(PixelLightPatternAsset), false);
                if (GUILayout.Button("Open Pixel Light Designer"))
                    PixelLightTimedDesignerWindow.Open();
                break;
            case CityPixelBrushKind.PedCallButton:
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField("Button Prefab", stamp.signPrefab, typeof(GameObject), false);
                break;
            case CityPixelBrushKind.Crosswalk:
                stamp.barCount = EditorGUILayout.IntField("Bar Count", stamp.barCount);
                stamp.barWidthM = EditorGUILayout.FloatField("Bar Width", stamp.barWidthM);
                stamp.acrossLanes = EditorGUILayout.Toggle("Across Lanes", stamp.acrossLanes);
                break;
            case CityPixelBrushKind.Sidewalk:
            case CityPixelBrushKind.GrassStrip:
                stamp.laneConfig = (RoadLaneConfigAsset)EditorGUILayout.ObjectField(
                    "Lane Config", stamp.laneConfig, typeof(RoadLaneConfigAsset), false);
                stamp.stripWidthM = EditorGUILayout.FloatField("Strip Width", stamp.stripWidthM);
                break;
            case CityPixelBrushKind.JerseyBarrier:
            case CityPixelBrushKind.GuardRail:
                stamp.bendWithRoad = EditorGUILayout.Toggle("Bend With Road", stamp.bendWithRoad);
                stamp.laneDisabled = EditorGUILayout.Toggle("Lane Disabled", stamp.kind == CityPixelBrushKind.JerseyBarrier && stamp.laneDisabled);
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", stamp.signPrefab, typeof(GameObject), false);
                break;
            case CityPixelBrushKind.WireEnd:
                stamp.poleId = EditorGUILayout.TextField("Pole Id", stamp.poleId ?? "");
                stamp.wireId = EditorGUILayout.TextField("Wire Id", stamp.wireId ?? "");
                stamp.wireT01 = EditorGUILayout.Slider("T", stamp.wireT01, 0f, 1f);
                stamp.wireEndKind = (StreetWireEndKind)EditorGUILayout.EnumPopup("End Kind", stamp.wireEndKind);
                break;
            case CityPixelBrushKind.Debris:
                stamp.signPrefab = (GameObject)EditorGUILayout.ObjectField("Debris Prefab", stamp.signPrefab, typeof(GameObject), false);
                stamp.laneConfig = (RoadLaneConfigAsset)EditorGUILayout.ObjectField(
                    "Lane Config", stamp.laneConfig, typeof(RoadLaneConfigAsset), false);
                break;
        }

        DrawCrowdHintFields(ref stamp);
    }

    public static void DrawCrowdHintFields(ref CityPixelBrushStamp stamp)
    {
        if (stamp == null) return;
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Crowd / RTS TravelAgent Hints", EditorStyles.boldLabel);
        stamp.crowdHint = (CityPixelCrowdHint)EditorGUILayout.EnumPopup("Crowd Hint", stamp.crowdHint);
        stamp.flockGroupId = EditorGUILayout.TextField("Flock Group Id", stamp.flockGroupId ?? "");
        stamp.ambulationCacheKey = EditorGUILayout.TextField("Ambulation Cache Key", stamp.ambulationCacheKey ?? "");
        stamp.cacheLikelihood01 = EditorGUILayout.Slider("Cache Likelihood", stamp.cacheLikelihood01, 0f, 1f);
        stamp.cacheToleranceM = EditorGUILayout.FloatField("Cache Tolerance M", stamp.cacheToleranceM);
        if (stamp.travelHintRow == null)
            stamp.travelHintRow = new TravelAuthoringRow { kind = TravelAuthoringRowKind.Hint };
        stamp.travelHintRow.kind = TravelAuthoringRowKind.Hint;
        stamp.travelHintRow.notes = EditorGUILayout.TextField("Hint Notes", stamp.travelHintRow.notes ?? "");
    }

    static void DrawPlaceableStampFields(ref CityPixelBrushStamp stamp)
    {
        stamp.heightCells = Mathf.Max(1, EditorGUILayout.IntField("Height Cells", stamp.heightCells < 1 ? 1 : stamp.heightCells));
        stamp.candidateId = EditorGUILayout.TextField("Forced Candidate Id", stamp.candidateId ?? "");
    }

    static void DrawCardinalButtons(ref float yaw)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("N")) yaw = 0f;
        if (GUILayout.Button("E")) yaw = 90f;
        if (GUILayout.Button("S")) yaw = 180f;
        if (GUILayout.Button("W")) yaw = 270f;
        EditorGUILayout.EndHorizontal();
    }

    public static void OpenAvailableEditors(CivilSystemKind kind)
    {
        // Always useful
        TryMenu("Locomotion/Pixel Light Timed Designer");
        TryMenu("Locomotion/Ladder Logic Designer");
        TryMenu("Locomotion/City Pixel Grid Designer");

        switch (kind)
        {
            case CivilSystemKind.PoliceStation:
            case CivilSystemKind.FireStation:
            case CivilSystemKind.CarRepair:
            case CivilSystemKind.Prison:
                Debug.Log($"[CityPixel] Open Available Editors for {kind}: PixelLight, LadderLogic, CityPixel (+ assign CivilInstitutionStub in scene).");
                break;
            case CivilSystemKind.School:
                TryMenu("Locomotion/Campus Pixel Grid Designer");
                break;
            default:
                Debug.Log($"[CityPixel] Open Available Editors for {kind}: opened shared locomotion editors. Kind-specific windows may be missing.");
                break;
        }
    }

    static void TryMenu(string path)
    {
        if (!EditorApplication.ExecuteMenuItem(path))
            Debug.Log($"[CityPixel] Menu not found: {path}");
    }

    public static CityPixelBrushStamp CloneStampTemplate(CityPixelBrushStamp src, int frame, int x, int y)
    {
        var s = new CityPixelBrushStamp();
        if (src != null)
        {
            s.kind = src.kind;
            s.yawDegrees = src.yawDegrees;
            s.payloadJson = src.payloadJson;
            s.signPrefab = src.signPrefab;
            s.signagePrefabs = src.signagePrefabs;
            s.ladderAsset = src.ladderAsset;
            s.pixelLightPattern = src.pixelLightPattern;
            s.pixelLightColors = src.pixelLightColors;
            s.buildingConfig = src.buildingConfig;
            s.buildingKind = src.buildingKind;
            s.buildingTypeId = src.buildingTypeId;
            s.signKind = src.signKind;
            s.placementPrompt = src.placementPrompt;
            s.detourGoalCellX = src.detourGoalCellX;
            s.detourGoalCellY = src.detourGoalCellY;
            s.stopRadius = src.stopRadius;
            s.scheduleCron = src.scheduleCron;
            s.avoidCostMultiplier = src.avoidCostMultiplier;
            s.slowRadius = src.slowRadius;
            s.heightCells = src.heightCells;
            s.candidateId = src.candidateId;
            s.typeKey = src.typeKey;
            s.floorIndex = src.floorIndex;
            s.zoneId = src.zoneId;
            s.floorPlanIndexMap = src.floorPlanIndexMap;
            s.tunnelStress01 = src.tunnelStress01;
            s.wallDestructible = src.wallDestructible;
            s.diggable = src.diggable;
            s.laneConfig = src.laneConfig;
            s.streetLightKind = src.streetLightKind;
            s.shoulderSign = src.shoulderSign;
            s.spacingAlongM = src.spacingAlongM;
            s.approachId = src.approachId;
            s.stopPotential01 = src.stopPotential01;
            s.bendWithRoad = src.bendWithRoad;
            s.poleId = src.poleId;
            s.wireId = src.wireId;
            s.wireT01 = src.wireT01;
            s.wireEndKind = src.wireEndKind;
            s.barCount = src.barCount;
            s.barWidthM = src.barWidthM;
            s.acrossLanes = src.acrossLanes;
            s.stripWidthM = src.stripWidthM;
            s.laneDisabled = src.laneDisabled;
            s.crowdHint = src.crowdHint;
            s.flockGroupId = src.flockGroupId;
            s.ambulationCacheKey = src.ambulationCacheKey;
            s.cacheLikelihood01 = src.cacheLikelihood01;
            s.cacheToleranceM = src.cacheToleranceM;
            s.travelHintRow = src.travelHintRow;
        }
        s.frameIndex = frame;
        s.cellX = x;
        s.cellY = y;
        return s;
    }
}
