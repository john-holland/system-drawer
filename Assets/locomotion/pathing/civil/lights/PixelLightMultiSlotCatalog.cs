using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Orthographic designer view for PixelLight authoring (matches heli/airplane 6-view).</summary>
public enum PixelLightDesignerView
{
    Top = 0,
    Front = 1,
    Back = 2,
    Left = 3,
    Right = 4,
    Bottom = 5
}

public enum PixelLightDesignerScope
{
    Airframe = 0,
    Magneto = 1,
    Frame = 2,
    Shell = 3
}

/// <summary>Per view × scope × magneto PixelLight property bag (independent saves).</summary>
[Serializable]
public sealed class PixelLightViewScopeSettings
{
    public PixelLightDesignerView view = PixelLightDesignerView.Top;
    public PixelLightDesignerScope scope = PixelLightDesignerScope.Airframe;
    public int magnetoIndex;
    public string slotId;

    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 0.25f;
    public int mountCellX;
    public int mountCellY;
    public int minigridW = 1;
    public int minigridH = 1;
    public int nestedMinigridW = 1;
    public int nestedMinigridH = 1;
    public bool recursiveBlock;
    public int centroidCellX;
    public int centroidCellY;
    public RadialSide radialSide = RadialSide.Center;
    public RadialBuildSpec radialBuild = new RadialBuildSpec();
    public CustomRadialSideAsset customRadialSide;
    public int previewConfigIndex = -1;
    public RadialBuildHost radialHost;
    public Vector3 fineOffset;
    public bool snapToBake = true;
    public bool onlyActivateLightSource;
    public PixelLightPatternAsset pattern;
    public PixelLightColorPackage colors;
    public int paintLayer;
    public int paintFrame;
    public int brushKind;
    public int rigGridWidth = 8;
    public int rigGridHeight = 4;
    public float stepMs = 100f;
    [Range(0f, 1f)] public float brightness01 = 1f;

    public string Key => MakeKey(view, scope, magnetoIndex);

    public static string MakeKey(PixelLightDesignerView view, PixelLightDesignerScope scope, int magnetoIndex) =>
        ((int)view) + "|" + ((int)scope) + "|" + Mathf.Max(0, magnetoIndex);

    public void CopyFromMount(PixelLightGridMountGameObject mount, PixelLightRig rig)
    {
        if (mount == null) return;
        gridWidth = mount.gridWidth;
        gridHeight = mount.gridHeight;
        cellSize = mount.cellSize;
        mountCellX = mount.mountCellX;
        mountCellY = mount.mountCellY;
        minigridW = mount.minigridW;
        minigridH = mount.minigridH;
        nestedMinigridW = mount.nestedMinigridW;
        nestedMinigridH = mount.nestedMinigridH;
        recursiveBlock = mount.recursiveBlock;
        centroidCellX = mount.centroidCellX;
        centroidCellY = mount.centroidCellY;
        radialSide = mount.radialSide;
        radialBuild = mount.radialBuild;
        customRadialSide = mount.customRadialSide;
        previewConfigIndex = mount.previewConfigIndex;
        radialHost = mount.radialHost;
        fineOffset = mount.fineOffset;
        snapToBake = mount.snapToBake;
        onlyActivateLightSource = mount.onlyActivateLightSource;
        if (mount.pattern != null) pattern = mount.pattern;
        if (rig != null)
        {
            if (rig.pattern != null) pattern = rig.pattern;
            if (rig.colorPackage != null) colors = rig.colorPackage;
            rigGridWidth = rig.gridWidth;
            rigGridHeight = rig.gridHeight;
            stepMs = rig.stepMs;
            brightness01 = rig.masterBrightness01;
        }
    }

    public void ApplyToMount(PixelLightGridMountGameObject mount)
    {
        if (mount == null) return;
        mount.gridWidth = gridWidth;
        mount.gridHeight = gridHeight;
        mount.cellSize = cellSize;
        mount.mountCellX = mountCellX;
        mount.mountCellY = mountCellY;
        mount.minigridW = minigridW;
        mount.minigridH = minigridH;
        mount.nestedMinigridW = nestedMinigridW;
        mount.nestedMinigridH = nestedMinigridH;
        mount.recursiveBlock = recursiveBlock;
        mount.centroidCellX = centroidCellX;
        mount.centroidCellY = centroidCellY;
        mount.radialSide = radialSide;
        mount.radialBuild = radialBuild;
        mount.customRadialSide = customRadialSide;
        mount.previewConfigIndex = previewConfigIndex;
        mount.radialHost = radialHost;
        mount.fineOffset = fineOffset;
        mount.snapToBake = snapToBake;
        mount.onlyActivateLightSource = onlyActivateLightSource;
        mount.pattern = pattern;
        var rig = mount.rig != null ? mount.rig : mount.GetComponentInChildren<PixelLightRig>();
        if (rig != null)
        {
            if (pattern != null)
                rig.SetPattern(pattern);
            if (colors != null)
                rig.colorPackage = colors;
            rig.gridWidth = rigGridWidth;
            rig.gridHeight = rigGridHeight;
            rig.stepMs = stepMs;
            rig.masterBrightness01 = brightness01;
        }
    }
}

/// <summary>One grid slot entry for Placement accordion (heli / airplane / airport / Frame-Shell).</summary>
[Serializable]
public sealed class PixelLightGridSlotEntry
{
    public string slotId;
    public string label = "Slot";
    public int cellX;
    public int cellY;
    public Vector3 fineOffset;
    public HelicoptorGridSlotGameObject.SlotContents contents;
    public PixelLightGridMountGameObject mount;
    public HelicoptorGridSlotGameObject heliSlot;
    public bool accordionExpanded = true;
    public PixelLightGridSlotKind kind = PixelLightGridSlotKind.Light;
    public string frameId;
    public string doorId;
    public string hingeLabel;
    public Bounds4SdfInclusionKind inclusion = Bounds4SdfInclusionKind.Shell;
    public float hollowRadius;
    public int zIndex;
}

public enum PixelLightGridSlotKind
{
    Light = 0,
    HollowSubtract = 1,
    Door = 2
}

/// <summary>
/// Default underlying PixelLight multi-slot + per-view/scope settings catalog.
/// Shared by helicopter, airplane, and airport designers.
/// </summary>
[CreateAssetMenu(fileName = "PixelLightMultiSlotCatalog", menuName = "Locomotion/Civil/Pixel Light Multi Slot Catalog")]
public sealed class PixelLightMultiSlotCatalog : ScriptableObject
{
    public List<PixelLightViewScopeSettings> viewScopeSettings = new List<PixelLightViewScopeSettings>();
    public List<PixelLightGridSlotEntry> gridSlots = new List<PixelLightGridSlotEntry>();

    [Tooltip("Soft cap for Feature Budget / designer warnings.")]
    public int maxRecommendedSlots = 16;

    public PixelLightViewScopeSettings GetOrCreate(
        PixelLightDesignerView view, PixelLightDesignerScope scope, int magnetoIndex)
    {
        string key = PixelLightViewScopeSettings.MakeKey(view, scope, magnetoIndex);
        for (int i = 0; i < viewScopeSettings.Count; i++)
        {
            var s = viewScopeSettings[i];
            if (s != null && s.Key == key)
                return s;
        }
        var created = new PixelLightViewScopeSettings
        {
            view = view,
            scope = scope,
            magnetoIndex = Mathf.Max(0, magnetoIndex)
        };
        viewScopeSettings.Add(created);
        return created;
    }

    public string NextIncrementingLabel(string prefix)
    {
        if (gridSlots == null)
            gridSlots = new List<PixelLightGridSlotEntry>();
        string head = (string.IsNullOrEmpty(prefix) ? "Mount" : prefix).Trim() + " ";
        int max = 0;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            string lab = gridSlots[i] != null ? gridSlots[i].label : null;
            if (string.IsNullOrEmpty(lab) || !lab.StartsWith(head, StringComparison.Ordinal))
                continue;
            if (int.TryParse(lab.Substring(head.Length), out int n))
                max = Mathf.Max(max, n);
        }
        return head + (max + 1);
    }

    public PixelLightGridSlotEntry AddSlot(string label = null)
    {
        if (gridSlots == null)
            gridSlots = new List<PixelLightGridSlotEntry>();
        var e = new PixelLightGridSlotEntry
        {
            slotId = Guid.NewGuid().ToString("N").Substring(0, 8),
            label = string.IsNullOrEmpty(label) ? NextIncrementingLabel("Mount") : label,
            accordionExpanded = true,
            zIndex = NextZIndex()
        };
        gridSlots.Add(e);
        return e;
    }

    /// <summary>Bench / well / jury / gallery / bar slots for courtroom PixelLight designers.</summary>
    public void EnsureCourtroomSlots()
    {
        if (gridSlots == null)
            gridSlots = new List<PixelLightGridSlotEntry>();
        EnsureLabeledSlot("court_bench", "Bench");
        EnsureLabeledSlot("court_well", "Well");
        EnsureLabeledSlot("court_jury", "Jury");
        EnsureLabeledSlot("court_gallery", "Gallery");
        EnsureLabeledSlot("court_bar", "Bar");
    }

    public PixelLightGridSlotEntry EnsureLabeledSlot(string slotId, string label)
    {
        if (gridSlots == null)
            gridSlots = new List<PixelLightGridSlotEntry>();
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null && gridSlots[i].slotId == slotId)
                return gridSlots[i];
        }
        var e = AddSlot(label);
        e.slotId = slotId;
        return e;
    }

    public void EnsureSewingMachineSlots()
    {
        string sewingShell = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SewingShell);
        string sewingFrame = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SewingFrame);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.NeedleThroat),
            "Needle throat", sewingShell, Bounds4SdfInclusionKind.Shell, 4, 3, 0.02f);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.BobbinRace),
            "Bobbin race", sewingShell, Bounds4SdfInclusionKind.Shell, 4, 4, 0.018f);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.ThreadPath),
            "Thread path", sewingFrame, Bounds4SdfInclusionKind.Frame, 2, 3, 0.01f);
        string doorBobbin = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorBobbin);
        EnsureDoor(doorBobbin, "Bobbin door", sewingShell, doorBobbin,
            FrameShellInclusionLemmaPropertyKeys.HingeLeft, Bounds4SdfInclusionKind.Shell, 6, 4, 0.03f);
        string doorBed = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorBed);
        EnsureDoor(doorBed, "Bed door", sewingFrame, doorBed,
            FrameShellInclusionLemmaPropertyKeys.HingeRear, Bounds4SdfInclusionKind.Frame, 1, 6, 0.04f);
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Needle, "Needle").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Hook, "Hook").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Bobbin, "Bobbin").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Presser, "Presser").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
    }

    public void EnsureSergerSlots()
    {
        string sergerShell = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SergerShell);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.NeedleThroat),
            "Needle throat", sergerShell, Bounds4SdfInclusionKind.Shell, 4, 3, 0.018f);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LooperRace),
            "Looper race", sergerShell, Bounds4SdfInclusionKind.Shell, 5, 3, 0.02f);
        string doorLooper = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorLooper);
        EnsureDoor(doorLooper, "Looper door", sergerShell, doorLooper,
            FrameShellInclusionLemmaPropertyKeys.HingeFront, Bounds4SdfInclusionKind.Shell, 6, 2, 0.035f);
        EnsureLabeledSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LooperUpper),
            "Upper looper").contents = HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LooperLower),
            "Lower looper").contents = HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Differential, "Differential").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        EnsureLabeledSlot(SewingLemmaPropertyKeys.Needle, "Needle").contents =
            HelicoptorGridSlotGameObject.SlotContents.PixelLight;
    }

    public void EnsureLatheSlots()
    {
        string latheBed = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LatheFrameBed);
        string latheCover = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LatheShellCover);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SpindleBore),
            "Spindle bore", latheBed, Bounds4SdfInclusionKind.Frame, 4, 4, 0.03f);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.TailstockQuill),
            "Tailstock quill", latheBed, Bounds4SdfInclusionKind.Frame, 7, 4, 0.02f);
        EnsureHollow(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.ChipChute),
            "Chip chute", latheCover, Bounds4SdfInclusionKind.Shell, 4, 1, 0.04f);
        string doorHead = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorHeadstock);
        EnsureDoor(doorHead, "Headstock door", latheCover, doorHead,
            FrameShellInclusionLemmaPropertyKeys.HingeLeft, Bounds4SdfInclusionKind.Shell, 1, 4, 0.05f);
        string doorGear = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorGearbox);
        EnsureDoor(doorGear, "Gearbox door", latheBed, doorGear,
            FrameShellInclusionLemmaPropertyKeys.HingeFront, Bounds4SdfInclusionKind.Frame, 2, 2, 0.045f);
        string doorPan = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorChipPan);
        EnsureDoor(doorPan, "Chip pan", latheBed, doorPan,
            FrameShellInclusionLemmaPropertyKeys.HingeBottom, Bounds4SdfInclusionKind.Frame, 4, 0, 0.05f);
    }

    public PixelLightGridSlotEntry EnsureHollow(
        string slotId, string label, string frameId, Bounds4SdfInclusionKind inclusion,
        int cellX, int cellY, float hollowRadius)
    {
        var e = EnsureLabeledSlot(slotId, label);
        e.kind = PixelLightGridSlotKind.HollowSubtract;
        e.frameId = frameId;
        e.inclusion = inclusion;
        e.cellX = cellX;
        e.cellY = cellY;
        e.hollowRadius = hollowRadius;
        e.contents = HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        return e;
    }

    public PixelLightGridSlotEntry EnsureDoor(
        string slotId, string label, string frameId, string doorId, string hingeLabel,
        Bounds4SdfInclusionKind inclusion, int cellX, int cellY, float hollowRadius)
    {
        var e = EnsureLabeledSlot(slotId, label);
        e.kind = PixelLightGridSlotKind.Door;
        e.frameId = frameId;
        e.doorId = doorId;
        e.hingeLabel = hingeLabel;
        e.inclusion = inclusion;
        e.cellX = cellX;
        e.cellY = cellY;
        e.hollowRadius = hollowRadius;
        e.contents = HelicoptorGridSlotGameObject.SlotContents.PixelLight;
        return e;
    }

    public int NextZIndex()
    {
        int max = -1;
        if (gridSlots == null) return 0;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null)
                max = Mathf.Max(max, gridSlots[i].zIndex);
        }
        return max + 1;
    }

    public List<PixelLightGridSlotEntry> SlotsAtCell(int cellX, int cellY)
    {
        var list = new List<PixelLightGridSlotEntry>();
        if (gridSlots == null) return list;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            var e = gridSlots[i];
            if (e != null && e.cellX == cellX && e.cellY == cellY)
                list.Add(e);
        }
        list.Sort((a, b) => a.zIndex.CompareTo(b.zIndex));
        return list;
    }

    public PixelLightGridSlotEntry FindSlot(string slotId)
    {
        if (gridSlots == null || string.IsNullOrEmpty(slotId)) return null;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null && gridSlots[i].slotId == slotId)
                return gridSlots[i];
        }
        return null;
    }

    public bool MoveSlotZ(string slotId, int delta)
    {
        var entry = FindSlot(slotId);
        if (entry == null) return false;
        int from = entry.zIndex;
        int to = from + delta;
        PixelLightGridSlotEntry swap = null;
        int best = delta > 0 ? int.MaxValue : int.MinValue;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            var e = gridSlots[i];
            if (e == null || e == entry) continue;
            if (delta > 0 && e.zIndex > from && e.zIndex < best)
            {
                best = e.zIndex;
                swap = e;
            }
            else if (delta < 0 && e.zIndex < from && e.zIndex > best)
            {
                best = e.zIndex;
                swap = e;
            }
        }
        if (swap != null)
        {
            int tmp = entry.zIndex;
            entry.zIndex = swap.zIndex;
            swap.zIndex = tmp;
        }
        else
            entry.zIndex = Mathf.Max(0, to);
        SortSlotsByZ();
        return true;
    }

    public void MoveSlotZToEdge(string slotId, bool front)
    {
        var entry = FindSlot(slotId);
        if (entry == null) return;
        if (front)
            entry.zIndex = NextZIndex();
        else
        {
            int min = int.MaxValue;
            for (int i = 0; i < gridSlots.Count; i++)
            {
                if (gridSlots[i] != null && gridSlots[i] != entry)
                    min = Mathf.Min(min, gridSlots[i].zIndex);
            }
            entry.zIndex = min == int.MaxValue ? 0 : min - 1;
        }
        SortSlotsByZ();
    }

    public void SortSlotsByZ()
    {
        if (gridSlots == null) return;
        gridSlots.Sort((a, b) =>
        {
            int az = a != null ? a.zIndex : 0;
            int bz = b != null ? b.zIndex : 0;
            int c = az.CompareTo(bz);
            if (c != 0) return c;
            string al = a != null ? a.slotId : "";
            string bl = b != null ? b.slotId : "";
            return string.CompareOrdinal(al, bl);
        });
    }

    public List<PixelLightGridSlotEntry> SubtractSlotsFor(Bounds4SdfInclusionKind inclusion)
    {
        var list = new List<PixelLightGridSlotEntry>();
        if (gridSlots == null) return list;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            var e = gridSlots[i];
            if (e == null) continue;
            if (e.kind != PixelLightGridSlotKind.HollowSubtract && e.kind != PixelLightGridSlotKind.Door)
                continue;
            if (e.inclusion != inclusion) continue;
            list.Add(e);
        }
        list.Sort((a, b) => a.zIndex.CompareTo(b.zIndex));
        return list;
    }

    public int MaxZIndex()
    {
        int max = 0;
        if (gridSlots == null) return 0;
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (gridSlots[i] != null)
                max = Mathf.Max(max, gridSlots[i].zIndex);
        }
        return max;
    }

    public void SyncSlotsFromHeli(HelicopterVehicleRagdoll heli)
    {
        if (heli == null) return;
        heli.gridSlots ??= new List<HelicoptorGridSlotGameObject>();
        var found = heli.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var slot = found[i];
            if (slot == null) continue;
            if (!heli.gridSlots.Contains(slot))
                heli.gridSlots.Add(slot);
            EnsureEntryForHeliSlot(slot);
        }
    }

    public void SyncSlotsFromAirplane(AirplaneVehicleRagdoll plane)
    {
        if (plane == null) return;
        plane.gridSlots ??= new List<HelicoptorGridSlotGameObject>();
        var found = plane.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var slot = found[i];
            if (slot == null) continue;
            if (!plane.gridSlots.Contains(slot))
                plane.gridSlots.Add(slot);
            EnsureEntryForHeliSlot(slot);
        }
        SyncSlotsFromMounts(plane.lightMounts);
    }

    public void SyncSlotsFromMounts(IList<PixelLightGridMountGameObject> mounts)
    {
        if (mounts == null) return;
        for (int i = 0; i < mounts.Count; i++)
        {
            var m = mounts[i];
            if (m == null) continue;
            bool found = false;
            for (int j = 0; j < gridSlots.Count; j++)
            {
                if (gridSlots[j] != null && gridSlots[j].mount == m)
                {
                    found = true;
                    gridSlots[j].cellX = m.mountCellX;
                    gridSlots[j].cellY = m.mountCellY;
                    break;
                }
            }
            if (!found)
            {
                var e = AddSlot(m.gameObject.name);
                e.mount = m;
                e.cellX = m.mountCellX;
                e.cellY = m.mountCellY;
                e.contents = HelicoptorGridSlotGameObject.SlotContents.PixelLight;
            }
        }
    }

    void EnsureEntryForHeliSlot(HelicoptorGridSlotGameObject slot)
    {
        for (int i = 0; i < gridSlots.Count; i++)
            if (gridSlots[i] != null && gridSlots[i].heliSlot == slot)
            {
                gridSlots[i].cellX = slot.cellX;
                gridSlots[i].cellY = slot.cellY;
                gridSlots[i].contents = slot.contents;
                gridSlots[i].mount = slot.lightMount;
                return;
            }
        var e = AddSlot(slot.gameObject.name);
        e.heliSlot = slot;
        e.cellX = slot.cellX;
        e.cellY = slot.cellY;
        e.contents = slot.contents;
        e.mount = slot.lightMount;
    }

    /// <summary>
    /// Removes catalog entry and destroys linked scene slot / mount references so PixelLight G overlays clear.
    /// </summary>
    public bool RemoveSlotAt(
        int index,
        HelicopterVehicleRagdoll heli = null,
        AirplaneVehicleRagdoll airplane = null,
        bool destroySceneObjects = true)
    {
        if (index < 0 || index >= gridSlots.Count) return false;
        var entry = gridSlots[index];
        gridSlots.RemoveAt(index);
        if (entry != null)
            UnlinkAndDestroyEntry(entry, heli, airplane, destroySceneObjects);
        return true;
    }

    /// <summary>Find and remove grid slot(s) at cell, including scene objects on heli/airplane.</summary>
    public bool RemoveSlotAtCell(
        int cellX,
        int cellY,
        HelicopterVehicleRagdoll heli = null,
        AirplaneVehicleRagdoll airplane = null,
        bool destroySceneObjects = true)
    {
        bool removed = false;

        HelicoptorGridSlotGameObject sceneSlot = null;
        if (heli != null)
        {
            var children = heli.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].cellX == cellX && children[i].cellY == cellY)
                {
                    sceneSlot = children[i];
                    break;
                }
            }
        }
        if (sceneSlot == null && airplane != null)
        {
            var children = airplane.GetComponentsInChildren<HelicoptorGridSlotGameObject>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].cellX == cellX && children[i].cellY == cellY)
                {
                    sceneSlot = children[i];
                    break;
                }
            }
        }

        for (int i = gridSlots.Count - 1; i >= 0; i--)
        {
            var e = gridSlots[i];
            if (e == null) continue;
            bool match = (e.heliSlot != null && e.heliSlot.cellX == cellX && e.heliSlot.cellY == cellY)
                         || (e.mount != null && e.mount.mountCellX == cellX && e.mount.mountCellY == cellY)
                         || (e.heliSlot == null && e.mount == null && e.cellX == cellX && e.cellY == cellY)
                         || (sceneSlot != null && e.heliSlot == sceneSlot);
            if (!match) continue;
            RemoveSlotAt(i, heli, airplane, destroySceneObjects);
            removed = true;
        }

        if (sceneSlot != null)
        {
            var orphan = new PixelLightGridSlotEntry
            {
                cellX = cellX,
                cellY = cellY,
                heliSlot = sceneSlot,
                mount = sceneSlot.lightMount
            };
            UnlinkAndDestroyEntry(orphan, heli, airplane, destroySceneObjects);
            removed = true;
        }

        return removed;
    }

    public void UnlinkAndDestroyEntry(
        PixelLightGridSlotEntry entry,
        HelicopterVehicleRagdoll heli = null,
        AirplaneVehicleRagdoll airplane = null,
        bool destroySceneObjects = true)
    {
        if (entry == null) return;

        var slot = entry.heliSlot;
        var mount = entry.mount;
        if (slot != null && mount == null)
            mount = slot.lightMount;

        if (heli != null)
        {
            if (heli.gridSlots != null && slot != null)
                heli.gridSlots.Remove(slot);
            if (heli.lightMounts != null && mount != null)
                heli.lightMounts.Remove(mount);
        }
        if (airplane != null)
        {
            if (airplane.gridSlots != null && slot != null)
                airplane.gridSlots.Remove(slot);
            if (airplane.lightMounts != null && mount != null)
                airplane.lightMounts.Remove(mount);
        }

        // Drop any other catalog rows that pointed at the same scene objects.
        for (int i = gridSlots.Count - 1; i >= 0; i--)
        {
            var e = gridSlots[i];
            if (e == null) continue;
            if ((slot != null && e.heliSlot == slot) || (mount != null && e.mount == mount))
                gridSlots.RemoveAt(i);
        }

        if (!destroySceneObjects) return;

        // Never wipe the craft root: strip the component; destroy the GO only if it becomes empty.
        if (slot != null)
            DestroyComponentOrEmptyHost(slot, ProtectRoots(heli, airplane));
        if (mount != null)
            DestroyComponentOrEmptyHost(mount, ProtectRoots(heli, airplane));
    }

    static GameObject[] ProtectRoots(HelicopterVehicleRagdoll heli, AirplaneVehicleRagdoll airplane)
    {
        int n = 0;
        if (heli != null) n++;
        if (airplane != null) n++;
        if (n == 0) return Array.Empty<GameObject>();
        var roots = new GameObject[n];
        int i = 0;
        if (heli != null) roots[i++] = heli.gameObject;
        if (airplane != null) roots[i] = airplane.gameObject;
        return roots;
    }

    /// <summary>
    /// Removes <paramref name="component"/> only. Destroys its GameObject only when no other
    /// components remain (besides Transform) and the host is not a protected craft root.
    /// </summary>
    public static void DestroyComponentOrEmptyHost(Component component, GameObject[] protectRoots = null)
    {
        if (component == null) return;
        var go = component.gameObject;
        if (go == null) return;

        bool isProtectedRoot = false;
        if (protectRoots != null)
        {
            for (int i = 0; i < protectRoots.Length; i++)
            {
                if (protectRoots[i] != null && protectRoots[i] == go)
                {
                    isProtectedRoot = true;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(component);
        else
            UnityEngine.Object.DestroyImmediate(component);
#else
        UnityEngine.Object.Destroy(component);
#endif

        if (go == null || isProtectedRoot) return;
        if (go.transform.childCount > 0) return;

        // Transform always remains; destroy host only when it is otherwise empty.
        var leftover = go.GetComponents<Component>();
        bool onlyTransform = true;
        for (int i = 0; i < leftover.Length; i++)
        {
            if (leftover[i] == null) continue;
            if (leftover[i] is Transform) continue;
            onlyTransform = false;
            break;
        }
        if (!onlyTransform) return;

#if UNITY_EDITOR
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(go);
        else
            UnityEngine.Object.DestroyImmediate(go);
#else
        UnityEngine.Object.Destroy(go);
#endif
    }
}
