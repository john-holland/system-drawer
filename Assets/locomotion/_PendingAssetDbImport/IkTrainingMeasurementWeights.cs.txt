using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Rig;

[Serializable]
public struct IkTrainingObjectWeight
{
    public string hierarchyPath;
    public float weight;
}

[Serializable]
public struct IkTrainingLimbWeight
{
    public string traitId;
    public float weight;
}

/// <summary>Weighted limb-to-object distance scoring for edit-mode IK measurement.</summary>
public static class IkTrainingLiveScore
{
    public static float ObjectWeightOf(GameObject go, float authored)
    {
        if (authored > 0f)
            return authored;
        if (go == null)
            return 1f;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null && rb.mass > 0f)
            return rb.mass;
        return 1f;
    }

    public static bool TryScore(PhysicsIKTrainingRunAsset run, BoneMap map, out float accuracy)
    {
        accuracy = 0f;
        if (run == null || map == null)
            return false;
        List<GameObject> objects = run.ResolveMeasurementObjects();
        if (objects == null || objects.Count == 0)
            return false;

        var limbs = run.actorLimbWeights;
        if (limbs == null || limbs.Count == 0)
        {
            limbs = new List<IkTrainingLimbWeight>
            {
                new IkTrainingLimbWeight { traitId = "Human:RightHand", weight = 1f }
            };
        }

        float sum = 0f;
        int n = 0;
        for (int li = 0; li < limbs.Count; li++)
        {
            var limb = limbs[li];
            if (limb.weight <= 0f || string.IsNullOrEmpty(limb.traitId))
                continue;
            if (!map.TryGet(limb.traitId, out Transform t) || t == null)
                continue;
            Vector3 limbPos = t.position;
            for (int oi = 0; oi < objects.Count; oi++)
            {
                var go = objects[oi];
                if (go == null)
                    continue;
                float ow = ObjectWeightOf(go, WeightForObject(run, go, oi));
                if (ow <= 0f)
                    continue;
                float d = Vector3.Distance(limbPos, go.transform.position);
                sum += limb.weight * ow * d;
                n++;
            }
        }

        if (n == 0)
            return false;
        accuracy = Mathf.Clamp01(1f / (1f + sum));
        return true;
    }

    public static List<(GameObject go, bool wasActive)> ActivateInEditor(IList<GameObject> objects)
    {
        var snap = new List<(GameObject, bool)>();
        if (objects == null)
            return snap;
        for (int i = 0; i < objects.Count; i++)
        {
            var go = objects[i];
            if (go == null)
                continue;
            snap.Add((go, go.activeSelf));
            go.SetActive(true);
        }
        return snap;
    }

    public static void RestoreActiveFlags(IList<(GameObject go, bool wasActive)> snap)
    {
        if (snap == null)
            return;
        for (int i = 0; i < snap.Count; i++)
        {
            if (snap[i].go != null)
                snap[i].go.SetActive(snap[i].wasActive);
        }
    }

    static float WeightForObject(PhysicsIKTrainingRunAsset run, GameObject go, int index)
    {
        var list = run.measurementObjectWeights;
        if (list == null || list.Count == 0)
            return 1f;
        if (index >= 0 && index < list.Count)
            return list[index].weight;
        for (int i = 0; i < list.Count; i++)
        {
            if (go != null && go.name == list[i].hierarchyPath)
                return list[i].weight;
        }
        return 1f;
    }
}
