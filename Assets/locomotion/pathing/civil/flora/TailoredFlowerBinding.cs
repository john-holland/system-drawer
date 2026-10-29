using System;
using System.Collections.Generic;
using UnityEngine;

public enum TailoredOrganRole
{
    Receptacle = 0,
    Sepal = 1,
    Stigma = 2,
    Ovule = 3,
    Petal = 4
}

[Serializable]
public sealed class TailoredFlowerOrgan
{
    public TailoredOrganRole role;
    public Vector3 centroid;
    public Vector3 planeNormal = Vector3.up;
    public string sourcePathId;
}

public static class TailoredFlowerMapping
{
    public const float BalanceTolerance = 0.02f;

    public static List<TailoredFlowerOrgan> FromBolt(ClothBoltSpec bolt, Vector3 patternCentroid)
    {
        var organs = new List<TailoredFlowerOrgan>
        {
            new TailoredFlowerOrgan
            {
                role = TailoredOrganRole.Receptacle,
                centroid = patternCentroid,
                planeNormal = Vector3.up
            },
            new TailoredFlowerOrgan
            {
                role = TailoredOrganRole.Sepal,
                centroid = patternCentroid,
                planeNormal = Vector3.up
            }
        };
        if (bolt?.paths == null) return organs;

        var centers = new Vector3[bolt.paths.Count];
        var stigma = new bool[bolt.paths.Count];
        for (int i = 0; i < bolt.paths.Count; i++)
            centers[i] = PathCenter(bolt.paths[i], patternCentroid);

        float tol = BalanceTolerance;
        for (int i = 0; i < bolt.paths.Count; i++)
        {
            for (int j = i + 1; j < bolt.paths.Count; j++)
            {
                Vector3 mid = (centers[i] + centers[j]) * 0.5f;
                if ((mid - patternCentroid).sqrMagnitude > tol * tol) continue;
                float di = (centers[i] - patternCentroid).magnitude;
                float dj = (centers[j] - patternCentroid).magnitude;
                if (Mathf.Abs(di - dj) > tol) continue;
                if (di < 0.01f && dj < 0.01f) continue;
                stigma[i] = true;
                stigma[j] = true;
            }
        }

        for (int i = 0; i < bolt.paths.Count; i++)
        {
            var path = bolt.paths[i];
            if (path == null) continue;
            if (stigma[i])
            {
                organs.Add(new TailoredFlowerOrgan
                {
                    role = TailoredOrganRole.Stigma,
                    centroid = centers[i],
                    planeNormal = Vector3.up,
                    sourcePathId = path.pathId
                });
            }
            if (IsTack(path))
            {
                organs.Add(new TailoredFlowerOrgan
                {
                    role = TailoredOrganRole.Ovule,
                    centroid = centers[i],
                    planeNormal = Vector3.up,
                    sourcePathId = path.pathId
                });
            }
            if (IsSeam(path))
            {
                Vector3 split = SplitDirection(path);
                organs.Add(new TailoredFlowerOrgan
                {
                    role = TailoredOrganRole.Petal,
                    centroid = centers[i],
                    planeNormal = split,
                    sourcePathId = path.pathId
                });
            }
        }
        return organs;
    }

    public static bool IsTack(ClothSplinePath path)
    {
        if (path == null || path.kind != ClothSplineKind.Stitch) return false;
        var program = path.program;
        if (program?.steps == null || program.steps.Count == 0 || program.steps.Count > 4) return false;
        bool entry = false, exit = false, strand = false;
        for (int i = 0; i < program.steps.Count; i++)
        {
            var step = program.steps[i];
            if (step == null) continue;
            if (step.phase == SewingNeedlePhase.Entry) entry = true;
            if (step.phase == SewingNeedlePhase.Exit) exit = true;
            if (step.connectingStrand) strand = true;
        }
        return entry && exit && strand;
    }

    public static bool IsSeam(ClothSplinePath path)
    {
        if (path == null) return false;
        if (path.kind == ClothSplineKind.Hem) return true;
        return !string.IsNullOrEmpty(path.joinLoopIdA) || !string.IsNullOrEmpty(path.joinLoopIdB);
    }

    public static Vector3 PathCenter(ClothSplinePath path, Vector3 fallback)
    {
        if (path?.controlPoints == null || path.controlPoints.Count == 0) return fallback;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < path.controlPoints.Count; i++)
            sum += path.controlPoints[i];
        return sum / path.controlPoints.Count;
    }

    public static Vector3 SplitDirection(ClothSplinePath path)
    {
        if (path?.controlPoints == null || path.controlPoints.Count < 2)
            return Vector3.right;
        Vector3 split = path.controlPoints[path.controlPoints.Count - 1] - path.controlPoints[0];
        if (split.sqrMagnitude < 1e-8f) return Vector3.right;
        return split.normalized;
    }
}

[CreateAssetMenu(fileName = "TailoredFlower", menuName = "Locomotion/Civil/Tailored Flower Binding")]
public sealed class TailoredFlowerBinding : ScriptableObject
{
    public ClothBoltSpec bolt;
    public Vector3 patternCentroid;
    public List<TailoredFlowerOrgan> organs = new List<TailoredFlowerOrgan>();

    public void Rebuild()
    {
        organs = TailoredFlowerMapping.FromBolt(bolt, patternCentroid);
    }
}
