using UnityEngine;

public enum PlumbingCardKind
{
    Clog = 0,
    Plunge = 1,
    Snake = 2
}

/// <summary>Force or simulate a toilet clog.</summary>
[System.Serializable]
public class ClogToiletCard : GoodSection
{
    public ToiletFixture toilet;
    [Range(0f, 1f)] public float forceClog01 = 1f;
    public bool wetPacking = true;

    public ClogToiletCard()
    {
        isPlumbingGoal = true;
        physicalPathingTag = "plumb_clog";
        traversabilityTag = "plumbing";
    }

    public void Apply()
    {
        var clog = toilet != null ? toilet.plumbing?.clog : null;
        if (clog == null) return;
        if (wetPacking) clog.AccumulateWet(forceClog01);
        else clog.AccumulateDry(forceClog01);
        if (forceClog01 >= 0.99f) clog.developerForceClog = true;
    }

    public static ClogToiletCard Generate(ToiletFixture toilet, float clog01 = 1f) =>
        new ClogToiletCard
        {
            toilet = toilet,
            forceClog01 = clog01,
            sectionName = "clog_toilet",
            description = "ClogToilet",
            isPlumbingGoal = true
        };
}

/// <summary>Plunge: SPH mixing perturbation + sealed anti-splat, or explicit clear.</summary>
[System.Serializable]
public class PlungeToiletCard : GoodSection
{
    public ToiletFixture toilet;
    [Range(0f, 1f)] public float clear01 = 0.55f;
    [Range(0f, 1f)] public float mixPerturbation01 = 0.35f;
    public bool sealedAntiSplat = true;
    public float antiSplatRadiusM = 0.45f;

    public PlungeToiletCard()
    {
        isPlumbingGoal = true;
        physicalPathingTag = "plumb_plunge";
        traversabilityTag = "plumbing";
    }

    public void Apply()
    {
        toilet?.plumbing?.clog?.Plunge(clear01, mixPerturbation01);
    }

    public static PlungeToiletCard Generate(ToiletFixture toilet) =>
        new PlungeToiletCard
        {
            toilet = toilet,
            sectionName = "plunge_toilet",
            description = "PlungeToilet",
            isPlumbingGoal = true,
            sealedAntiSplat = true
        };
}

/// <summary>Snake: IK tool + SDF spline extrusion + stiff rope vs SPH liquid.</summary>
[System.Serializable]
public class SnakeToiletCard : GoodSection
{
    public ToiletFixture toilet;
    public GameObject snakeTool;
    [Range(0f, 1f)] public float clear01 = 0.85f;
    public float splineExtrudeMeters = 1.2f;
    public float spinRpm = 90f;
    public bool useStiffRope = true;
    public bool ironicSdfExtrusion = true;

    public SnakeToiletCard()
    {
        isPlumbingGoal = true;
        physicalPathingTag = "plumb_snake";
        traversabilityTag = "plumbing";
    }

    public void Apply()
    {
        toilet?.plumbing?.clog?.SnakeClear(clear01);
    }

    public static SnakeToiletCard Generate(ToiletFixture toilet, GameObject tool = null) =>
        new SnakeToiletCard
        {
            toilet = toilet,
            snakeTool = tool,
            sectionName = "snake_toilet",
            description = "SnakeToilet",
            isPlumbingGoal = true,
            ironicSdfExtrusion = true,
            useStiffRope = true
        };
}
