using System;
using System.Collections.Generic;
using UnityEngine;

public enum GrassThermalClass
{
    Cool = 0,
    Hot = 1
}

public enum GrassMorphology
{
    Bunchgrass = 0,
    Stoloniferous = 1,
    Rhizomatous = 2,
    Tussock = 3,
    Aquatic = 4,
    PreternaturalRampant = 5
}

public enum GrassDevStepKind
{
    Emerge = 0,
    Tiller = 1,
    FormLeaf = 2,
    ShedLeaf = 3,
    Stolon = 4,
    RhizomeNode = 5,
    RhizomeEmergence = 6
}

[Serializable]
public sealed class GrassDevStep
{
    public GrassDevStepKind kind;
    [Range(0f, 1f)] public float water01 = 0.55f;
    [Range(0f, 1f)] public float heat01 = 0.5f;
    [Range(0f, 1f)] public float ground01 = 0.5f;
    [Range(0f, 1f)] public float light01 = 0.6f;
    [Range(0f, 1f)] public float limitWater01 = 0.2f;
    [Range(0f, 1f)] public float limitHeat01 = 0.15f;
    [Range(0f, 1f)] public float limitGround01 = 0.1f;
    [Range(0f, 1f)] public float limitLight01 = 0.1f;
    [Range(0f, 1f)] public float optimalWater01 = 0.7f;
    [Range(0f, 1f)] public float optimalHeat01 = 0.55f;
    [Range(0f, 1f)] public float optimalGround01 = 0.6f;
    [Range(0f, 1f)] public float optimalLight01 = 0.75f;
    public float nodeSpacing = 0.15f;
}

public static class GrassDevelopment
{
    public static List<GrassDevStep> DefaultPipeline()
    {
        return new List<GrassDevStep>
        {
            Step(GrassDevStepKind.Emerge),
            Step(GrassDevStepKind.Tiller),
            Step(GrassDevStepKind.FormLeaf),
            Step(GrassDevStepKind.ShedLeaf),
            Step(GrassDevStepKind.Stolon),
            Step(GrassDevStepKind.RhizomeNode),
            Step(GrassDevStepKind.RhizomeEmergence)
        };
    }

    public static bool RhizomeNodePrecedesEmergence(IList<GrassDevStep> steps)
    {
        int node = -1;
        int emerge = -1;
        if (steps == null)
            return true;
        for (int i = 0; i < steps.Count; i++)
        {
            GrassDevStep step = steps[i];
            if (step == null)
                continue;
            if (step.kind == GrassDevStepKind.RhizomeNode && node < 0)
                node = i;
            if (step.kind == GrassDevStepKind.RhizomeEmergence && emerge < 0)
                emerge = i;
        }
        if (emerge < 0)
            return true;
        return node >= 0 && node < emerge;
    }

    static GrassDevStep Step(GrassDevStepKind kind)
    {
        return new GrassDevStep { kind = kind };
    }
}

/// <summary>
/// Hot or cool grass for later biome placement. Temperature and month ranges
/// describe when the species grows, not a live weather sample.
/// </summary>
[CreateAssetMenu(fileName = "GrassType", menuName = "Locomotion/Civil/Grass Type")]
public sealed class GrassTypeDef : ScriptableObject
{
    public string speciesId = "grass";
    public GrassThermalClass thermal = GrassThermalClass.Cool;
    public GrassMorphology morphology = GrassMorphology.Bunchgrass;
    [Range(1, 12)] public int monthStart = 3;
    [Range(1, 12)] public int monthEnd = 10;
    public float biomeTempMinC = 5f;
    public float biomeTempMaxC = 24f;
    [Range(0f, 1f)] public float waterRequirement01 = 0.45f;
    [Min(0f)] public float shedSize = 0.08f;
    public string manifoldConfigId = "loam";
    public LeafSpec leaf;
    public KarmanVortexWindFit windFit;
    public List<GrassDevStep> steps = new List<GrassDevStep>();

    public bool MonthActive(int month)
    {
        int m = Mathf.Clamp(month, 1, 12);
        if (monthStart <= monthEnd)
            return m >= monthStart && m <= monthEnd;
        return m >= monthStart || m <= monthEnd;
    }

    public bool TemperatureInBiome(float celsius)
    {
        return celsius >= biomeTempMinC && celsius <= biomeTempMaxC;
    }

    public void ApplyMorphology(GrassMorphology form)
    {
        morphology = form;
        if (steps == null)
            steps = new List<GrassDevStep>();
        steps.Clear();
        steps.Add(new GrassDevStep { kind = GrassDevStepKind.Emerge });
        steps.Add(new GrassDevStep { kind = GrassDevStepKind.Tiller });
        steps.Add(new GrassDevStep { kind = GrassDevStepKind.FormLeaf });
        steps.Add(new GrassDevStep { kind = GrassDevStepKind.ShedLeaf });
        switch (form)
        {
            case GrassMorphology.Stoloniferous:
                steps.Add(new GrassDevStep { kind = GrassDevStepKind.Stolon, nodeSpacing = 0.2f });
                break;
            case GrassMorphology.Rhizomatous:
            case GrassMorphology.PreternaturalRampant:
                steps.Add(new GrassDevStep { kind = GrassDevStepKind.RhizomeNode, nodeSpacing = 0.25f });
                steps.Add(new GrassDevStep { kind = GrassDevStepKind.RhizomeEmergence });
                break;
            case GrassMorphology.Tussock:
                steps.Add(new GrassDevStep { kind = GrassDevStepKind.Tiller, nodeSpacing = 0.05f });
                break;
            case GrassMorphology.Aquatic:
                waterRequirement01 = 0.9f;
                thermal = GrassThermalClass.Cool;
                break;
        }
        if (form == GrassMorphology.Bunchgrass || form == GrassMorphology.Tussock)
            thermal = GrassThermalClass.Cool;
        if (form == GrassMorphology.Stoloniferous)
            thermal = GrassThermalClass.Hot;
        if (form == GrassMorphology.PreternaturalRampant)
        {
            thermal = GrassThermalClass.Hot;
            biomeTempMinC = -5f;
            biomeTempMaxC = 45f;
        }
    }

    public PlantOrganismDef CreateOrganism()
    {
        var organism = ScriptableObject.CreateInstance<PlantOrganismDef>();
        organism.speciesId = string.IsNullOrEmpty(speciesId) ? "grass" : speciesId;
        organism.form = PlantGrowthForm.Grass;
        organism.waterRequirement01 = waterRequirement01;
        organism.sunRequirement01 = 0.45f;
        organism.mineralWhitelist = new[] { string.IsNullOrEmpty(manifoldConfigId) ? "loam" : manifoldConfigId };
        organism.name = organism.speciesId;
        return organism;
    }
}

public static class GrassTypeCatalog
{
    public static readonly GrassMorphology[] Seeded =
    {
        GrassMorphology.Bunchgrass,
        GrassMorphology.Stoloniferous,
        GrassMorphology.Rhizomatous,
        GrassMorphology.Tussock,
        GrassMorphology.Aquatic,
        GrassMorphology.PreternaturalRampant
    };

    public static GrassTypeDef CreateSeed(GrassMorphology morphology)
    {
        var def = ScriptableObject.CreateInstance<GrassTypeDef>();
        def.speciesId = morphology.ToString().ToLowerInvariant();
        def.ApplyMorphology(morphology);
        return def;
    }
}
