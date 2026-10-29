using System;
using System.Collections.Generic;
using UnityEngine;

public enum FlowerDevStepKind
{
    Induction = 0,
    Peduncle = 1,
    PedicelReceptacle = 2,
    SepalClose = 3,
    PetalPack = 4,
    InteriorWhorls = 5,
    Anthesis = 6,
    PollenExchange = 7,
    Fertilization = 8,
    PetalSenescence = 9,
    FruitSet = 10,
    SeedMaturity = 11
}

public enum FlowerOrganKind
{
    Peduncle = 0,
    Pedicel = 1,
    Receptacle = 2,
    Sepal = 3,
    Petal = 4,
    Stigma = 5,
    Style = 6,
    Ovary = 7,
    Ovule = 8,
    Anther = 9,
    Nectary = 10,
    Fruit = 11
}

[Serializable]
public sealed class FlowerOrganPiece
{
    public FlowerOrganKind kind;
    public string pieceId;
    public Vector3 centroidLocal;
    public Mesh mesh;
    public Texture2D texture;
    public SdfMax.SdfMaxCompositionAsset sdf;
    public PollenDef pollen;
    public GameObject particleEmitterPrefab;
    public bool committedSeed;
}

[Serializable]
public sealed class FlowerPieceMap
{
    public string pieceId;
    public Vector3 fromCentroid;
    public Vector3 toCentroid;
    public AnimationCurve tween = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public static Vector3 Evaluate(FlowerPieceMap map, float t)
    {
        if (map == null) return Vector3.zero;
        float u = Mathf.Clamp01(t);
        if (map.tween != null && map.tween.length > 0)
            u = map.tween.Evaluate(u);
        return Vector3.LerpUnclamped(map.fromCentroid, map.toCentroid, u);
    }
}

[Serializable]
public sealed class FlowerDevStep
{
    public FlowerDevStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    public List<FlowerOrganPiece> pieces = new List<FlowerOrganPiece>();
    public List<FlowerPieceMap> maps = new List<FlowerPieceMap>();
    [Range(0f, 1f)] public float water01 = 0.6f;
    [Range(0f, 1f)] public float sun01 = 0.7f;
    [Range(0f, 1f)] public float ground01 = 0.55f;
    [Range(0f, 1f)] public float optimalWater01 = 0.7f;
    [Range(0f, 1f)] public float optimalSun01 = 0.8f;
    [Range(0f, 1f)] public float optimalGround01 = 0.6f;
    [Range(0f, 1f)] public float optimalRetinue01 = 0.55f;
    [Range(0f, 1f)] public float limitWater01 = 0.2f;
    [Range(0f, 1f)] public float limitSun01 = 0.15f;
    [Range(0f, 1f)] public float limitGround01 = 0.1f;
    [Range(0f, 1f)] public float limitRetinue01;
    public float releaseImpulse;
    public float intakeAngleDeg;
    [Min(0f)] public float intakeRadius;
    public bool attractsPollinators;
    public bool success;
}

public static class FlowerDevelopment
{
    public static Vector3 DefaultCentroid(FlowerOrganKind kind, FlowerDevStepKind step)
    {
        bool opened = step == FlowerDevStepKind.Anthesis
                      || step == FlowerDevStepKind.PollenExchange
                      || step == FlowerDevStepKind.Fertilization;
        bool droop = step == FlowerDevStepKind.PetalSenescence;
        bool fruiting = step == FlowerDevStepKind.FruitSet || step == FlowerDevStepKind.SeedMaturity;
        switch (kind)
        {
            case FlowerOrganKind.Peduncle: return new Vector3(0f, 0.02f, 0f);
            case FlowerOrganKind.Pedicel: return new Vector3(0f, 0.08f, 0f);
            case FlowerOrganKind.Receptacle: return fruiting ? new Vector3(0f, 0.14f, 0f) : new Vector3(0f, 0.12f, 0f);
            case FlowerOrganKind.Sepal:
                if (droop) return new Vector3(0.04f, 0.08f, 0.02f);
                return opened ? new Vector3(0.06f, 0.10f, 0f) : new Vector3(0.015f, 0.13f, 0f);
            case FlowerOrganKind.Petal:
                if (droop) return new Vector3(0.04f, 0.06f, 0.03f);
                return opened ? new Vector3(0.09f, 0.16f, 0f) : new Vector3(0.012f, 0.145f, 0f);
            case FlowerOrganKind.Stigma: return new Vector3(0f, 0.19f, 0f);
            case FlowerOrganKind.Style: return new Vector3(0f, 0.16f, 0f);
            case FlowerOrganKind.Ovary: return fruiting ? new Vector3(0f, 0.15f, 0f) : new Vector3(0f, 0.125f, 0f);
            case FlowerOrganKind.Ovule: return new Vector3(0.004f, 0.12f, 0.004f);
            case FlowerOrganKind.Anther: return opened ? new Vector3(0.045f, 0.17f, 0f) : new Vector3(0.02f, 0.15f, 0f);
            case FlowerOrganKind.Nectary: return new Vector3(0.02f, 0.118f, 0f);
            case FlowerOrganKind.Fruit: return new Vector3(0f, 0.16f, 0f);
            default: return Vector3.zero;
        }
    }

    public static List<FlowerDevStep> DefaultPipeline()
    {
        var list = new List<FlowerDevStep>();
        foreach (FlowerDevStepKind kind in Enum.GetValues(typeof(FlowerDevStepKind)))
        {
            var step = new FlowerDevStep
            {
                kind = kind,
                sgInstanceId = kind.ToString().ToLowerInvariant()
            };
            if (kind >= FlowerDevStepKind.PollenExchange)
            {
                step.releaseImpulse = 1.4f;
                step.intakeAngleDeg = 48f;
                step.intakeRadius = 0.045f;
            }
            AddOrgans(step);
            if (list.Count > 0)
                LinkFromPrevious(list[list.Count - 1], step);
            list.Add(step);
        }
        return list;
    }

    public static bool IsBudThroughAnthesis(FlowerDevStepKind kind)
    {
        return kind >= FlowerDevStepKind.SepalClose && kind <= FlowerDevStepKind.Anthesis;
    }

    static void AddOrgans(FlowerDevStep step)
    {
        switch (step.kind)
        {
            case FlowerDevStepKind.Induction:
                return;
            case FlowerDevStepKind.Peduncle:
                Add(step, FlowerOrganKind.Peduncle);
                return;
            case FlowerDevStepKind.PedicelReceptacle:
                Add(step, FlowerOrganKind.Peduncle, FlowerOrganKind.Pedicel, FlowerOrganKind.Receptacle);
                return;
            case FlowerDevStepKind.SepalClose:
                Add(step, FlowerOrganKind.Peduncle, FlowerOrganKind.Pedicel, FlowerOrganKind.Receptacle, FlowerOrganKind.Sepal);
                return;
            case FlowerDevStepKind.PetalPack:
                Add(step, FlowerOrganKind.Peduncle, FlowerOrganKind.Pedicel, FlowerOrganKind.Receptacle,
                    FlowerOrganKind.Sepal, FlowerOrganKind.Petal);
                return;
            case FlowerDevStepKind.FruitSet:
            case FlowerDevStepKind.SeedMaturity:
                AddInterior(step);
                Add(step, FlowerOrganKind.Fruit);
                return;
            default:
                AddInterior(step);
                return;
        }
    }

    static void AddInterior(FlowerDevStep step)
    {
        Add(step,
            FlowerOrganKind.Peduncle, FlowerOrganKind.Pedicel, FlowerOrganKind.Receptacle,
            FlowerOrganKind.Sepal, FlowerOrganKind.Petal,
            FlowerOrganKind.Stigma, FlowerOrganKind.Style, FlowerOrganKind.Ovary, FlowerOrganKind.Ovule,
            FlowerOrganKind.Anther, FlowerOrganKind.Nectary);
    }

    static void Add(FlowerDevStep step, params FlowerOrganKind[] kinds)
    {
        for (int i = 0; i < kinds.Length; i++)
        {
            var kind = kinds[i];
            step.pieces.Add(new FlowerOrganPiece
            {
                kind = kind,
                pieceId = kind.ToString().ToLowerInvariant(),
                centroidLocal = DefaultCentroid(kind, step.kind)
            });
        }
    }

    static void LinkFromPrevious(FlowerDevStep prev, FlowerDevStep cur)
    {
        if (prev?.pieces == null || cur?.pieces == null) return;
        for (int i = 0; i < cur.pieces.Count; i++)
        {
            var piece = cur.pieces[i];
            FlowerOrganPiece src = null;
            for (int p = 0; p < prev.pieces.Count; p++)
            {
                if (prev.pieces[p] != null && prev.pieces[p].pieceId == piece.pieceId)
                {
                    src = prev.pieces[p];
                    break;
                }
            }
            if (src == null) continue;
            cur.maps.Add(new FlowerPieceMap
            {
                pieceId = piece.pieceId,
                fromCentroid = src.centroidLocal,
                toCentroid = piece.centroidLocal,
                tween = AnimationCurve.Linear(0f, 0f, 1f, 1f)
            });
        }
    }
}
