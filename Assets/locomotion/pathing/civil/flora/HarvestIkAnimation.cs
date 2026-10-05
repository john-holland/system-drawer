using System;
using System.Collections.Generic;
using UnityEngine;

public enum HarvestIkKind
{
    Pluck = 0,
    Pull = 1,
    Weed = 2,
    Pick = 3,
    Pinch = 4
}

/// <summary>Item identity and the inclusive count range a harvest IK clip writes into inventory.</summary>
[Serializable]
public sealed class HarvestInventorySave
{
    public string itemId = "yield";
    public string itemName = "yield";
    public string partName = "fruit";
    public int countMin = 1;
    public int countMax = 1;

    public int SampleCount(int seed)
    {
        int min = Mathf.Min(countMin, countMax);
        int max = Mathf.Max(countMin, countMax);
        if (min == max) return min;
        var rng = new System.Random(seed);
        return rng.Next(min, max + 1);
    }

    public InventoryItem ToItem(int count)
    {
        return new InventoryItem
        {
            id = itemId,
            name = string.IsNullOrEmpty(itemName) ? itemId : itemName,
            count = Mathf.Max(0, count)
        };
    }

    public static HarvestInventorySave Copy(HarvestInventorySave src)
    {
        if (src == null) return new HarvestInventorySave();
        return new HarvestInventorySave
        {
            itemId = src.itemId,
            itemName = src.itemName,
            partName = src.partName,
            countMin = src.countMin,
            countMax = src.countMax
        };
    }
}

[Serializable]
public struct HarvestIkSample
{
    public float t01;
    public Vector3 handLocal;
    [Range(0f, 1f)] public float fingerCurl01;
    public float wristTwistDeg;
}

/// <summary>Hand IK for one harvest verb. <see cref="save"/> is the inventory item and count range it produces.</summary>
[Serializable]
public sealed class HarvestIkAnimation
{
    public HarvestIkKind kind;
    public HarvestInventorySave save = new HarvestInventorySave();
    public HarvestIkSample[] keys = Array.Empty<HarvestIkSample>();

    public static HarvestIkAnimation[] CreateSet()
    {
        return new[]
        {
            CreateDefault(HarvestIkKind.Pluck),
            CreateDefault(HarvestIkKind.Pull),
            CreateDefault(HarvestIkKind.Weed),
            CreateDefault(HarvestIkKind.Pick),
            CreateDefault(HarvestIkKind.Pinch)
        };
    }

    public static HarvestIkAnimation CreateDefault(HarvestIkKind kind, HarvestInventorySave save = null)
    {
        var anim = new HarvestIkAnimation
        {
            kind = kind,
            save = HarvestInventorySave.Copy(save ?? DefaultSave(kind)),
            keys = DefaultKeys(kind)
        };
        return anim;
    }

    public HarvestIkAnimation Clone()
    {
        var copy = new HarvestIkAnimation
        {
            kind = kind,
            save = HarvestInventorySave.Copy(save),
            keys = keys == null ? Array.Empty<HarvestIkSample>() : (HarvestIkSample[])keys.Clone()
        };
        return copy;
    }

    public HarvestIkSample Evaluate(float t01)
    {
        if (keys == null || keys.Length == 0)
            return new HarvestIkSample { t01 = t01 };
        if (keys.Length == 1)
        {
            var only = keys[0];
            only.t01 = t01;
            return only;
        }
        float t = Mathf.Clamp01(t01);
        int hi = 1;
        while (hi < keys.Length - 1 && keys[hi].t01 < t)
            hi++;
        var a = keys[hi - 1];
        var b = keys[hi];
        float span = Mathf.Max(1e-4f, b.t01 - a.t01);
        float u = Mathf.Clamp01((t - a.t01) / span);
        return new HarvestIkSample
        {
            t01 = t,
            handLocal = Vector3.Lerp(a.handLocal, b.handLocal, u),
            fingerCurl01 = Mathf.Lerp(a.fingerCurl01, b.fingerCurl01, u),
            wristTwistDeg = Mathf.Lerp(a.wristTwistDeg, b.wristTwistDeg, u)
        };
    }

    public InventoryItem Produce(int seed)
    {
        if (save == null) return null;
        return save.ToItem(save.SampleCount(seed));
    }

    public static float FingerSpread(HarvestIkKind kind)
    {
        switch (kind)
        {
            case HarvestIkKind.Pluck: return 35f;
            case HarvestIkKind.Pull: return 55f;
            case HarvestIkKind.Weed: return 50f;
            case HarvestIkKind.Pinch: return 12f;
            default: return 40f;
        }
    }

    public static float GripStrength(HarvestIkKind kind)
    {
        switch (kind)
        {
            case HarvestIkKind.Pluck: return 18f;
            case HarvestIkKind.Pull: return 40f;
            case HarvestIkKind.Weed: return 35f;
            case HarvestIkKind.Pinch: return 8f;
            default: return 22f;
        }
    }

    static HarvestInventorySave DefaultSave(HarvestIkKind kind)
    {
        switch (kind)
        {
            case HarvestIkKind.Pluck:
                return new HarvestInventorySave { itemId = "fruit", itemName = "fruit", partName = "fruit", countMin = 1, countMax = 3 };
            case HarvestIkKind.Pull:
                return new HarvestInventorySave { itemId = "stem", itemName = "stem", partName = "branch", countMin = 1, countMax = 1 };
            case HarvestIkKind.Weed:
                return new HarvestInventorySave { itemId = "weed", itemName = "weed", partName = "root", countMin = 1, countMax = 5 };
            case HarvestIkKind.Pinch:
                return new HarvestInventorySave { itemId = "pinch", itemName = "pinch", partName = "bud", countMin = 1, countMax = 2 };
            default:
                return new HarvestInventorySave { itemId = "pick", itemName = "pick", partName = "leaf", countMin = 1, countMax = 4 };
        }
    }

    static HarvestIkSample[] DefaultKeys(HarvestIkKind kind)
    {
        switch (kind)
        {
            case HarvestIkKind.Pluck:
                return new[]
                {
                    Sample(0f, Vector3.zero, 0.2f),
                    Sample(1f, new Vector3(0f, 0.12f, 0f), 0.95f)
                };
            case HarvestIkKind.Pull:
                return new[]
                {
                    Sample(0f, Vector3.zero, 0.8f),
                    Sample(1f, new Vector3(0f, 0f, -0.3f), 0.85f)
                };
            case HarvestIkKind.Weed:
                return new[]
                {
                    Sample(0f, new Vector3(0f, -0.05f, 0f), 0.6f),
                    Sample(1f, new Vector3(0f, 0.2f, 0f), 0.9f)
                };
            case HarvestIkKind.Pinch:
                return new[]
                {
                    Sample(0f, Vector3.zero, 0.4f),
                    Sample(1f, new Vector3(0f, 0.01f, 0f), 0.98f)
                };
            default:
                return new[]
                {
                    Sample(0f, new Vector3(0f, 0f, 0.05f), 0.3f),
                    Sample(1f, new Vector3(0f, 0.05f, -0.02f), 0.8f)
                };
        }
    }

    static HarvestIkSample Sample(float t, Vector3 hand, float curl)
    {
        return new HarvestIkSample { t01 = t, handLocal = hand, fingerCurl01 = curl };
    }
}

/// <summary>One prebaked Consider grab: grabber pose plus the harvest IK that fills inventory.</summary>
[Serializable]
public sealed class ConsiderGrabPrebake
{
    public string planeId;
    public Vector3 graspPoint;
    public Vector3 approachDirection;
    public float grabberRotationDeg;
    public float fingerSpread;
    public float gripStrength;
    public bool canGrab;
    public HarvestIkSample endPose;
    public HarvestIkAnimation animation;

    public static ConsiderGrabPrebake Bake(BranchPathBake path, PlantBranchDef branch, Hand hand, HarvestIkAnimation animation)
    {
        var result = new ConsiderGrabPrebake();
        if (branch == null)
        {
            result.canGrab = false;
            return result;
        }
        var clip = animation != null
            ? animation.Clone()
            : HarvestIkAnimation.CreateDefault(branch.harvest, branch.harvestSave);
        result.animation = clip;
        result.planeId = string.IsNullOrEmpty(branch.branchTypeLabel) ? "branch" : branch.branchTypeLabel;
        result.grabberRotationDeg = branch.grabberRotationDeg;
        result.fingerSpread = HarvestIkAnimation.FingerSpread(clip.kind);
        result.gripStrength = HarvestIkAnimation.GripStrength(clip.kind);
        if (path != null)
        {
            result.graspPoint = path.GrabberPoint(branch.grabberT01);
            Vector3 tangent = path.GrabberTangent(branch.grabberT01);
            result.approachDirection = Quaternion.AngleAxis(branch.grabberRotationDeg, tangent) * (-tangent);
        }
        else
        {
            result.approachDirection = -(branch.originRotation * Vector3.forward);
        }
        result.endPose = clip.Evaluate(1f);
        result.endPose.wristTwistDeg = branch.grabberRotationDeg;
        bool spreadOk = hand == null || result.fingerSpread <= hand.maxFingerSpread + 1e-3f;
        bool gripOk = hand == null || result.gripStrength <= hand.maxGripStrength + 1e-3f;
        result.canGrab = spreadOk && gripOk;
        return result;
    }
}
