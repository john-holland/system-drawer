using System;
using System.Collections.Generic;

public enum OrganHostRegion
{
    Head,
    Torso,
    Abdomen,
    NeckTorso
}

[Serializable]
public sealed class OrganDef
{
    public string id;
    public string displayName;
    public OrganHostRegion hostRegion;
    public string[] coupledChannelIds;
}

/// <summary>First-class organ definitions hosted on ragdoll regions.</summary>
public static class OrganCatalog
{
    public const string Heart = "heart";
    public const string Lungs = "lungs";
    public const string Liver = "liver";
    public const string Kidneys = "kidneys";
    public const string Stomach = "stomach";
    public const string Brain = "brain";
    public const string LymphCluster = "lymph_cluster";
    public const string EndocrineCluster = "endocrine_cluster";
    public const string Pancreas = "pancreas";
    public const string Spleen = "spleen";

    /// <summary>Spawn raw slightly above 1 so normalized reads as Great.</summary>
    public const float GreatSpawnRaw = 1.05f;
    public const float HomeostasisSetpointRaw = 1.0f;
    public const float EasyHomeostasisSetpointRaw = 1.05f;

    static readonly OrganDef[] All;
    static readonly Dictionary<string, OrganDef> ById;

    static OrganCatalog()
    {
        All = new[]
        {
            O(Heart, "Heart", OrganHostRegion.Torso,
                LifeSystemsChannelCatalog.HeartRate, LifeSystemsChannelCatalog.BloodPressureSys,
                LifeSystemsChannelCatalog.BloodPressureDia, LifeSystemsChannelCatalog.HypertensiveLoad),
            O(Lungs, "Lungs", OrganHostRegion.Torso, LifeSystemsChannelCatalog.Hydration, LifeSystemsChannelCatalog.LifeForce),
            O(Liver, "Liver", OrganHostRegion.Abdomen, LifeSystemsChannelCatalog.Lipids, LifeSystemsChannelCatalog.Cholesterol),
            O(Kidneys, "Kidneys", OrganHostRegion.Abdomen, LifeSystemsChannelCatalog.Hydration, LifeSystemsChannelCatalog.BloodPressureSys),
            O(Stomach, "Stomach", OrganHostRegion.Abdomen, LifeSystemsChannelCatalog.BloodSugar),
            O(Brain, "Brain", OrganHostRegion.Head,
                LifeSystemsChannelCatalog.ClearThought, LifeSystemsChannelCatalog.Memory,
                LifeSystemsChannelCatalog.Attention, LifeSystemsChannelCatalog.Mania, LifeSystemsChannelCatalog.Depression),
            O(LymphCluster, "Lymph Cluster", OrganHostRegion.NeckTorso, LifeSystemsChannelCatalog.Lymph, LifeSystemsChannelCatalog.Immune),
            O(EndocrineCluster, "Endocrine Cluster", OrganHostRegion.Torso, LifeSystemsChannelCatalog.Endocrine, LifeSystemsChannelCatalog.Adrenaline),
            O(Pancreas, "Pancreas", OrganHostRegion.Abdomen, LifeSystemsChannelCatalog.BloodSugar),
            O(Spleen, "Spleen", OrganHostRegion.Abdomen, LifeSystemsChannelCatalog.Immune),
        };
        ById = new Dictionary<string, OrganDef>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < All.Length; i++)
            ById[All[i].id] = All[i];
    }

    public static IReadOnlyList<OrganDef> Organs => All;

    public static bool TryGet(string id, out OrganDef def) => ById.TryGetValue(id ?? "", out def);

    static OrganDef O(string id, string name, OrganHostRegion region, params string[] channels) =>
        new OrganDef { id = id, displayName = name, hostRegion = region, coupledChannelIds = channels };
}
