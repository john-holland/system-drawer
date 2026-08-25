using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoadLaneBrushLayerOp
{
    public CityPixelLayerKind layerKind = CityPixelLayerKind.Roads;
    public CityPixelBrushKind brushKind = CityPixelBrushKind.RoadLanes;
    public GameObject prefab;
    public bool diggable;
    public bool raiseLayerOnOverlap = true;
}

[Serializable]
public sealed class RoadDebrisDef
{
    public GameObject prefab;
    public Vector3 minSpace = Vector3.one;
    public Vector3 optimalSpace = Vector3.one * 2f;
    public Vector3 maxSpace = Vector3.one * 4f;
}

[CreateAssetMenu(fileName = "RoadLaneConfig", menuName = "Locomotion/Civil/Road Lane Config")]
public sealed class RoadLaneConfigAsset : ScriptableObject
{
    public RoadLaneLayout layout = new RoadLaneLayout();
    public RoadLaneGridSettings grid = new RoadLaneGridSettings();
    public List<RoadLaneBrushLayerOp> recipe = new List<RoadLaneBrushLayerOp>();
    public List<RoadDebrisDef> debris = new List<RoadDebrisDef>();
    public List<Vector3> controlPoints = new List<Vector3>();

    [Header("Shoulder composition")]
    public float sidewalkWidthM = 1.8f;
    public float sidewalkPaddingM = 0.2f;
    [Range(0f, 1f)] public float mattingWidth01;
    public float curbHeightM = 0.15f;
    public float curbWidthM = 0.2f;
    [Range(0f, 1f)] public float dappleBevel01;
    public float grassStripWidthM = 0.8f;

    [Header("Prefabs")]
    public GameObject supportPrefab;
    public GameObject wallPrefab;
    public GameObject diggableVolumePrefab;
    public GameObject jerseyBarrierPrefab;
    public GameObject guardRailPrefab;
    public GameObject streetLightPrefab;
    public GameObject trafficSignalPrefab;
    public GameObject phonePolePrefab;
    public GameObject pedCallButtonPrefab;
    public GameObject signPrefab;
    public GameObject crosswalkMaterialOwner;

    [Header("Lights / signs")]
    public StreetLightKind streetLightKind = StreetLightKind.Luminaire;
    public int shoulderSign = 1;
    public float spacingAlongM = 28f;
    public string approachId = "main";
    public PixelLightPatternAsset pixelLightPattern;
    public PixelLightColorPackage pixelLightColors;
    [Range(0f, 2f)] public float stopPotential01 = 1f;

    public static RoadLaneConfigAsset CreateBridgeRecipe()
    {
        var a = CreateInstance<RoadLaneConfigAsset>();
        a.recipe.Add(new RoadLaneBrushLayerOp { layerKind = CityPixelLayerKind.Support, brushKind = CityPixelBrushKind.Bridge, diggable = true });
        a.recipe.Add(new RoadLaneBrushLayerOp { layerKind = CityPixelLayerKind.Overpass, brushKind = CityPixelBrushKind.Overpass, raiseLayerOnOverlap = true });
        return a;
    }

    public static RoadLaneConfigAsset CreateBridgeAndUnderpassRecipe()
    {
        var a = CreateInstance<RoadLaneConfigAsset>();
        a.recipe.Add(new RoadLaneBrushLayerOp { layerKind = CityPixelLayerKind.Underpass, brushKind = CityPixelBrushKind.BridgeAndUnderpass, diggable = true });
        a.recipe.Add(new RoadLaneBrushLayerOp { layerKind = CityPixelLayerKind.Overpass, brushKind = CityPixelBrushKind.Overpass, raiseLayerOnOverlap = true });
        return a;
    }

    public static RoadLaneConfigAsset CreateOverpassRecipe()
    {
        var a = CreateInstance<RoadLaneConfigAsset>();
        a.recipe.Add(new RoadLaneBrushLayerOp { layerKind = CityPixelLayerKind.Overpass, brushKind = CityPixelBrushKind.Overpass });
        return a;
    }

    public string ToExportJson()
    {
        return JsonUtility.ToJson(this, true);
    }
}

public enum StreetLightKind
{
    Luminaire = 0,
    Signal = 1
}
