using System.Collections.Generic;
using UnityEngine;

/// <summary>Flower phenology. Diamond axes are water, sun, ground, and retinue.</summary>
[AddComponentMenu("Locomotion/Travel/Flower Travel Agent")]
public class FlowerTravelAgent : PlantTravelAgent
{
    public static readonly string[] DiamondAxes = { "Water", "Sun", "Ground", "Retinue" };

    public List<FlowerDevStep> steps = new List<FlowerDevStep>();
    public int selectedStepIndex;
    public string speciesId = "flower";
    public string bindingId;
    public FlowerFruitMapping fruitMapping;
    public bool fertilized;
    public bool aborted;
    public string fruitSlotId;
    [Range(0f, 1f)] public float retinuePressure01;

    public FlowerDevStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public string SpeciesId =>
        organism != null && !string.IsNullOrEmpty(organism.speciesId) ? organism.speciesId : speciesId;

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = FlowerDevelopment.DefaultPipeline();
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.7f, 0.8f, 0.6f, 0.55f };
        return new[] { s.optimalWater01, s.optimalSun01, s.optimalGround01, s.optimalRetinue01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.2f, 0.15f, 0.1f, 0f };
        return new[] { s.limitWater01, s.limitSun01, s.limitGround01, s.limitRetinue01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.5f, 0.5f, retinuePressure01 };
        return new[]
        {
            s.water01 * compositionWaterScale01,
            s.sun01 * compositionSunScale01,
            s.ground01 * compositionGroundScale01,
            retinuePressure01
        };
    }

    public bool RequirementsInsideLimits()
    {
        var s = SelectedStep;
        if (s == null) return false;
        float water = s.water01 * compositionWaterScale01;
        float sun = s.sun01 * compositionSunScale01;
        float ground = s.ground01 * compositionGroundScale01;
        return water + 1e-4f >= s.limitWater01
               && sun + 1e-4f >= s.limitSun01
               && ground + 1e-4f >= s.limitGround01;
    }

    public Vector3 EvaluatePiece(string pieceId, float t)
    {
        var s = SelectedStep;
        if (s == null) return Vector3.zero;
        if (s.maps != null)
        {
            for (int i = 0; i < s.maps.Count; i++)
            {
                var map = s.maps[i];
                if (map != null && map.pieceId == pieceId)
                    return FlowerPieceMap.Evaluate(map, t);
            }
        }
        if (s.pieces != null)
        {
            for (int i = 0; i < s.pieces.Count; i++)
            {
                var piece = s.pieces[i];
                if (piece != null && piece.pieceId == pieceId)
                    return piece.centroidLocal;
            }
        }
        return Vector3.zero;
    }

    public float PetalPackContact01(ClothUvStretchCache cache, Vector2 uv)
    {
        var s = SelectedStep;
        if (cache == null || s == null || s.kind != FlowerDevStepKind.PetalPack)
            return 0f;
        return cache.ContactWeight01(uv);
    }

    public bool TryAdvancePhenology()
    {
        if (aborted) return false;
        var s = SelectedStep;
        if (s == null || steps == null) return false;
        if (!RequirementsInsideLimits()) return false;
        if (s.kind == FlowerDevStepKind.Fertilization && !fertilized)
        {
            aborted = true;
            fruitSlotId = "";
            return false;
        }
        int next = selectedStepIndex + 1;
        if (next >= steps.Count) return false;
        if (steps[next] != null && steps[next].kind == FlowerDevStepKind.FruitSet && !fertilized)
        {
            aborted = true;
            fruitSlotId = "";
            return false;
        }
        s.success = true;
        s.progress01 = 1f;
        selectedStepIndex = next;
        var now = SelectedStep;
        if (now != null && now.kind == FlowerDevStepKind.FruitSet && fertilized)
            fruitSlotId = fruitMapping != null ? fruitMapping.Resolve(SpeciesId) : SpeciesId;
        if (now != null && now.kind == FlowerDevStepKind.SeedMaturity)
            CommitOvulesToSeeds(now);
        return true;
    }

    public void CommitOvulesToSeeds(FlowerDevStep step)
    {
        if (step?.pieces == null) return;
        for (int i = 0; i < step.pieces.Count; i++)
        {
            var piece = step.pieces[i];
            if (piece != null && piece.kind == FlowerOrganKind.Ovule)
                piece.committedSeed = true;
        }
    }

    public void PublishStepGrowth(float mult = 1.25f, float hours = 1f)
    {
        var s = SelectedStep;
        PublishGrowthEmpower("flower", s != null ? s.kind.ToString() : "step", mult, hours);
    }
}
