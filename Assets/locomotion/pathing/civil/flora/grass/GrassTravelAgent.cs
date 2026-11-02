using System.Collections.Generic;
using UnityEngine;

/// <summary>Grass phenology. Diamond axes are water, heat, ground, and light.</summary>
[AddComponentMenu("Locomotion/Travel/Grass Travel Agent")]
public class GrassTravelAgent : PlantTravelAgent
{
    public static readonly string[] DiamondAxes = { "Water", "Heat", "Ground", "Light" };

    public List<GrassDevStep> steps = new List<GrassDevStep>();
    public int selectedStepIndex;
    public string speciesId = "grass";
    public GrassTypeDef grassType;
    public GroundCompositionSet composition;
    public bool blockProceduralMotion;
    [Range(0f, 1f)] public float blockAlong0;
    [Range(0f, 1f)] public float blockAlong1 = 1f;

    public GrassDevStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = GrassDevelopment.DefaultPipeline();
    }

    public void ApplyType()
    {
        if (grassType == null)
            return;
        speciesId = grassType.speciesId;
        if (grassType.steps != null && grassType.steps.Count > 0)
            steps = grassType.steps;
        waterRequirement01 = grassType.waterRequirement01;
        if (organism == null)
            organism = grassType.CreateOrganism();
    }

    public float[] BlueOptimal01()
    {
        GrassDevStep step = SelectedStep;
        if (step == null)
            return new[] { 0.7f, 0.55f, 0.6f, 0.75f };
        return new[] { step.optimalWater01, step.optimalHeat01, step.optimalGround01, step.optimalLight01 };
    }

    public float[] RedLimit01()
    {
        GrassDevStep step = SelectedStep;
        if (step == null)
            return new[] { 0.2f, 0.15f, 0.1f, 0.1f };
        return new[] { step.limitWater01, step.limitHeat01, step.limitGround01, step.limitLight01 };
    }

    public float[] DashedGreenCurrent01()
    {
        GrassDevStep step = SelectedStep;
        if (step == null)
            return new[] { 0.5f, 0.5f, 0.5f, liveSun01 };
        return new[]
        {
            step.water01 * compositionWaterScale01,
            step.heat01,
            step.ground01 * compositionGroundScale01,
            step.light01 * compositionSunScale01
        };
    }

    public bool TrySelect(GrassDevStepKind kind)
    {
        if (steps == null)
            return false;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].kind == kind)
            {
                selectedStepIndex = i;
                return true;
            }
        }
        return false;
    }

    public void AdvanceThrough(GrassDevStepKind kind)
    {
        if (steps == null || steps.Count == 0)
            steps = GrassDevelopment.DefaultPipeline();
        TrySelect(kind);
    }
}
