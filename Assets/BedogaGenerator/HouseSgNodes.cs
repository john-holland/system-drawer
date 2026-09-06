using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>Shared house SG part: floor index, optional SDF fragment, PixelLight mount.</summary>
public abstract class HousePartNode : SGBehaviorTreeNode
{
    public int floorIndex = 1;
    public SdfMaxCompositionAsset sdfFragment;
    public PixelLightGridMountGameObject pixelLightMount;

    public float FloorY(float storyHeightM, float originY = 0f) =>
        HouseFloorIndex.FloorY(floorIndex, storyHeightM, originY);

    public PixelLightGridMountGameObject BindPixelLight(GameObject host, HouseConstructionFloorParams floor)
    {
        if (host == null) return pixelLightMount;
        var mount = host.GetComponent<PixelLightGridMountGameObject>()
                    ?? host.AddComponent<PixelLightGridMountGameObject>();
        if (floor != null)
        {
            mount.gridWidth = Mathf.Max(1, floor.pixelLightGridW);
            mount.gridHeight = Mathf.Max(1, floor.pixelLightGridH);
            mount.cellSize = Mathf.Max(0.05f, floor.pixelLightCellSize);
        }
        pixelLightMount = mount;
        return mount;
    }
}

[AddComponentMenu("Bedoga/House/House Shell Node")]
public sealed class HouseShellNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Dig Site Node")]
public sealed class DigSiteNode : HousePartNode
{
    public DiggableVolume BindVolume(GameObject host)
    {
        if (host == null) return null;
        var vol = host.GetComponent<DiggableVolume>() ?? host.AddComponent<DiggableVolume>();
        vol.volumeKind = DiggableVolumeKind.Soil;
        vol.diggable = true;
        vol.floorIndex = floorIndex;
        return vol;
    }
}

[AddComponentMenu("Bedoga/House/Foundation Node")]
public sealed class FoundationNode : HousePartNode
{
    public DiggableVolume BindVolume(GameObject host)
    {
        if (host == null) return null;
        var vol = host.GetComponent<DiggableVolume>() ?? host.AddComponent<DiggableVolume>();
        vol.volumeKind = DiggableVolumeKind.Foundation;
        vol.floorIndex = floorIndex;
        return vol;
    }
}

[AddComponentMenu("Bedoga/House/Wall Volume Node")]
public sealed class WallVolumeNode : HousePartNode
{
    [Range(0f, 1f)] public float destructibility01 = 0.5f;
    public bool diggable = true;

    public DiggableVolume BindVolume(GameObject host)
    {
        if (host == null) return null;
        var vol = host.GetComponent<DiggableVolume>() ?? host.AddComponent<DiggableVolume>();
        vol.diggable = diggable;
        vol.destructibility01 = destructibility01;
        vol.volumeKind = DiggableVolumeKind.Wall;
        vol.floorIndex = floorIndex;
        vol.sdf = sdfFragment;
        return vol;
    }
}

[AddComponentMenu("Bedoga/House/Stud Bay Node")]
public sealed class StudBayNode : HousePartNode
{
    public float bayWidthM = 0.406f;
}

[AddComponentMenu("Bedoga/House/Insulation Batt Node")]
public sealed class InsulationBattNode : HousePartNode
{
    public int pleatLayers = 3;
    public bool inactiveUntilFrame = true;
}

[AddComponentMenu("Bedoga/House/Opening Node")]
public class HouseOpeningNode : HousePartNode
{
    public string jointId = "door";
    public PathingApertureMode apertureMode = PathingApertureMode.Walk;
    public string apertureTag = "door";

    public PathingAperture BindAperture(GameObject host)
    {
        if (host == null) return null;
        var ap = host.GetComponent<PathingAperture>() ?? host.AddComponent<PathingAperture>();
        ap.mode = apertureMode;
        ap.apertureId = jointId;
        if (ap.tags == null) ap.tags = new List<string>();
        if (!ap.tags.Contains(apertureTag)) ap.tags.Add(apertureTag);
        return ap;
    }
}

[AddComponentMenu("Bedoga/House/Garage Door Node")]
public sealed class GarageDoorNode : HouseOpeningNode
{
    public DoorAssemblySpec assembly;
    public GarageChainSpec chain;
    public int sectionCount = 4;

    void Reset()
    {
        jointId = "garage_door";
        apertureMode = PathingApertureMode.Vehicle;
        apertureTag = "garage_door";
        floorIndex = 1;
        if (assembly != null)
            sectionCount = assembly.sectionCount;
    }

    public void ConfigureRepeat(int sections)
    {
        int n = Mathf.Max(1, sections);
        sectionCount = n;
        if (assembly != null)
            assembly.sectionCount = n;
        placementLimit = 1;
        var parts = GetComponentsInChildren<HousePartNode>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p == null || p == this)
                continue;
            p.perParentPlacementLimits = true;
            p.placementLimitType = PlacementLimitType.Specific;
            if (p is DoorMullionNode)
                p.placementLimit = Mathf.Max(0, n - 1);
            else if (p is DoorLockStileNode)
                p.placementLimit = 2;
            else if (p is DoorMouldingNode mould)
            {
                mould.perParentPlacementLimits = true;
                mould.sides = assembly != null ? assembly.MouldingSideCount : 4;
                if (mould.radialBuild == null)
                    mould.radialBuild = new RadialBuildSpec();
                mould.radialBuild.count = mould.sides;
                mould.placeSearchMode = PlaceSearchMode.Radial;
                mould.placementMode = PlacementMode.Around;
                p.placementLimit = mould.sides;
            }
            else
                p.placementLimit = 1;
        }
    }
}

public enum DoorLockRailKind
{
    Middle = 0,
    Frieze = 1
}

[AddComponentMenu("Bedoga/House/Door Top Rail Node")]
public sealed class DoorTopRailNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Door Bottom Rail Node")]
public sealed class DoorBottomRailNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Door Lock Stile Node")]
public sealed class DoorLockStileNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Door Lock Rail Node")]
public sealed class DoorLockRailNode : HousePartNode
{
    public DoorLockRailKind railKind = DoorLockRailKind.Middle;
}

[AddComponentMenu("Bedoga/House/Door Mullion Node")]
public sealed class DoorMullionNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Door Moulding Node")]
public sealed class DoorMouldingNode : HousePartNode
{
    public int sides = 4;
}

[AddComponentMenu("Bedoga/House/Doorway Edge Portal Node")]
public sealed class DoorwayEdgePortalNode : HouseOpeningNode { }

[AddComponentMenu("Bedoga/House/Window Opening Node")]
public sealed class WindowOpeningNode : HouseOpeningNode
{
    public WindowAssemblySpec assembly;

    void Reset()
    {
        jointId = "window";
        apertureMode = PathingApertureMode.Walk;
        apertureTag = "window";
        floorIndex = 1;
    }

    public PathingAperture BindWindow(GameObject host)
    {
        var ap = BindAperture(host);
        if (ap == null) return null;
        ap.materialHint = "glass";
        ap.passMode = PathingAperturePassMode.CrashThrough;
        if (assembly != null)
        {
            ap.smellPassThrough01 = assembly.glazing == WindowGlazingKind.DoubleVacuum
                ? Mathf.Min(0.08f, assembly.smellPassThrough01)
                : assembly.smellPassThrough01;
            ap.hearingLeak01 = assembly.glazing == WindowGlazingKind.DoubleVacuum
                ? Mathf.Min(0.12f, assembly.hearingLeak01)
                : assembly.hearingLeak01;
        }
        return ap;
    }
}

[AddComponentMenu("Bedoga/House/Pane Node")]
public sealed class PaneNode : HousePartNode
{
    public WindowGlazingKind glazing = WindowGlazingKind.Single;
}

[AddComponentMenu("Bedoga/House/Muntin Grid Node")]
public sealed class MuntinGridNode : HousePartNode
{
    public float barWidth = 0.03f;
}

[AddComponentMenu("Bedoga/House/Sill Node")]
public sealed class SillNode : HousePartNode
{
    public bool underSillTrim;
}

[AddComponentMenu("Bedoga/House/Trim Run Node")]
public sealed class TrimRunNode : HousePartNode
{
    public int runSegments = 3;

    public int CountElbows() => MuntinGridLayout.ElbowCount;

    public int CountRunPieces(int gridAlong) => MuntinGridLayout.TrimRunLength(gridAlong);
}

[AddComponentMenu("Bedoga/House/Trim Elbow Node")]
public sealed class TrimElbowNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Shutter Node")]
public sealed class ShutterNode : HouseOpeningNode
{
    void Reset()
    {
        jointId = "shutter";
        apertureTag = "shutter";
        floorIndex = 1;
    }
}

[AddComponentMenu("Bedoga/House/Shade Node")]
public sealed class ShadeNode : HousePartNode
{
    public WindowShadeKind kind = WindowShadeKind.Slats;

    public PulleySurfaceRagdoll BindPulley(GameObject host)
    {
        if (host == null) return null;
        var pulley = host.GetComponent<PulleySurfaceRagdoll>() ?? host.AddComponent<PulleySurfaceRagdoll>();
        pulley.kind = kind == WindowShadeKind.Cloth ? PulleySurfaceKind.Cloth
            : kind == WindowShadeKind.Reeds ? PulleySurfaceKind.Reeds
            : PulleySurfaceKind.Slats;
        return pulley;
    }
}

[AddComponentMenu("Bedoga/House/Frame Node")]
public sealed class FrameNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Floor Node")]
public sealed class HouseFloorNode : HousePartNode
{
    public HouseFinishFloorKind finishKind = HouseFinishFloorKind.Wood;
}

[AddComponentMenu("Bedoga/House/Trim Node")]
public sealed class TrimNode : HousePartNode
{
    public string trimKind = "casing";
}

[AddComponentMenu("Bedoga/House/Feature Node")]
public sealed class FeatureNode : HousePartNode
{
    public string featureKind = "knob";
}

[AddComponentMenu("Bedoga/House/Electrical Span Node")]
public sealed class ElectricalSpanNode : HousePartNode
{
    public bool inactivePrebake = true;
}

[AddComponentMenu("Bedoga/House/Vent Duct Node")]
public sealed class VentDuctNode : HousePartNode
{
    public bool fullBoreCollider = true;
}

[AddComponentMenu("Bedoga/House/Pixel Light Fixture Node")]
public sealed class PixelLightFixtureNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Eave Node")]
public sealed class EaveNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Gutter Node")]
public sealed class GutterNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Awning Node")]
public sealed class AwningNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Front Steps Node")]
public sealed class FrontStepsNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Front Walk Node")]
public sealed class FrontWalkNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Patio Node")]
public sealed class PatioNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Grass Patch Node")]
public sealed class GrassPatchNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Yard Feature Node")]
public sealed class YardFeatureNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Railing Node")]
public sealed class HouseRailingNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Deck Wall Node")]
public sealed class DeckWallNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Support Post Node")]
public sealed class SupportPostNode : HousePartNode { }

[AddComponentMenu("Bedoga/House/Fence Run Node")]
public sealed class FenceRunNode : HousePartNode
{
    public float postSpacingM = 2.4f;

    void Reset()
    {
        placementLimit = 1;
        floorIndex = 1;
    }

    public int CountPosts(float splineLengthM)
    {
        float spacing = Mathf.Max(0.25f, postSpacingM);
        return Mathf.Max(2, Mathf.RoundToInt(Mathf.Max(0.01f, splineLengthM) / spacing) + 1);
    }

    public int CountPanels(int postCount) => Mathf.Max(0, postCount - 1);

    /// <summary>
    /// Posts/panels as UniformQueue children: one run instance, N posts and N-1 panels per parent.
    /// </summary>
    public void ConfigureRepeat(int postCount)
    {
        int posts = Mathf.Max(2, postCount);
        placementLimit = 1;
        var post = GetComponentInChildren<FencePostNode>(true);
        if (post != null)
        {
            post.perParentPlacementLimits = true;
            post.placementLimitType = PlacementLimitType.Specific;
            post.placementLimit = posts;
        }
        var panel = GetComponentInChildren<FencePanelNode>(true);
        if (panel != null)
        {
            panel.perParentPlacementLimits = true;
            panel.placementLimitType = PlacementLimitType.Specific;
            panel.placementLimit = CountPanels(posts);
        }
    }

    public List<string> CompileGateJointIds(RoadLotBoundarySpline spline)
    {
        var ids = new List<string>();
        if (spline?.wallSections == null) return ids;
        for (int i = 0; i < spline.wallSections.Count; i++)
        {
            var s = spline.wallSections[i];
            if (s != null && s.isGap && !string.IsNullOrEmpty(s.gateOpenCloseTopologyId))
                ids.Add(s.gateOpenCloseTopologyId);
        }
        return ids;
    }
}

[AddComponentMenu("Bedoga/House/Fence Post Node")]
public sealed class FencePostNode : HousePartNode
{
    void Reset()
    {
        perParentPlacementLimits = true;
        placementLimitType = PlacementLimitType.Specific;
        placementLimit = 1;
        floorIndex = 1;
    }
}

[AddComponentMenu("Bedoga/House/Fence Panel Node")]
public sealed class FencePanelNode : HousePartNode
{
    void Reset()
    {
        perParentPlacementLimits = true;
        placementLimitType = PlacementLimitType.Specific;
        placementLimit = 1;
        floorIndex = 1;
    }
}

[AddComponentMenu("Bedoga/House/Radial Run Node")]
public sealed class RadialRunNode : HousePartNode
{
    public int pieceCount = 4;

    void Reset()
    {
        placeSearchMode = PlaceSearchMode.Radial;
        placementMode = PlacementMode.Around;
        placementLimit = 1;
        floorIndex = 1;
        if (radialBuild == null)
            radialBuild = new RadialBuildSpec();
        radialBuild.count = pieceCount;
    }

    public void ConfigureRepeat(int count)
    {
        int n = Mathf.Max(1, count);
        pieceCount = n;
        placementLimit = 1;
        placeSearchMode = PlaceSearchMode.Radial;
        placementMode = PlacementMode.Around;
        if (radialBuild == null)
            radialBuild = new RadialBuildSpec();
        radialBuild.count = n;
        var parts = GetComponentsInChildren<HousePartNode>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p == null || p == this)
                continue;
            p.perParentPlacementLimits = true;
            p.placementLimitType = PlacementLimitType.Specific;
            p.placementLimit = n;
            p.placeSearchMode = PlaceSearchMode.Radial;
        }
    }
}

[AddComponentMenu("Bedoga/House/Fence Gate Node")]
public sealed class FenceGateNode : HouseOpeningNode
{
    void Reset()
    {
        jointId = "fence_gate";
        apertureTag = "gate";
        floorIndex = 1;
    }
}
