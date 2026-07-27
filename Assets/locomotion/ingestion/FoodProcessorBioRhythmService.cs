using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// On swallow: nutrient deltas + adjust toward healthy setpoints unless explicit nutrients;
/// optional poop queue; whitelisted eating smells.
/// </summary>
[AddComponentMenu("Locomotion/Ingestion/Food Processor Bio Rhythm Service")]
public sealed class FoodProcessorBioRhythmService : MonoBehaviour
{
    public const string ServiceKey = "food.processor";

    static FoodProcessorBioRhythmService _instance;
    public static FoodProcessorBioRhythmService Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<FoodProcessorBioRhythmService>();
            return _instance;
        }
    }

    public EatingSmellWhitelist smellWhitelist;
    public bool createPoopByDefault = true;
    public bool adjustToNormalIngredients = true;
    [Range(0f, 1f)] public float developerModification01 = 0.15f;
    public float bowelFillPerSwallow = 0.08f;
    public float bowelToiletThreshold = 0.75f;
    public bool autoQueueToiletBt = true;
    public PoopRuntime poopPrefab;
    [Range(0f, 1f)] public float defaultPoopWetness01 = 0.4f;
    [Range(0f, 1f)] public float defaultPoopSmell01 = 0.5f;
    public float defaultPoopTextureScale = 1f;

    /// <summary>Last poop instance created by CreatePoop (tests / toilet spawn).</summary>
    public PoopRuntime LastCreatedPoop { get; private set; }

    void Awake()
    {
        _instance = this;
    }

    public void OnSwallow(GameObject actor, FoodItem food)
    {
        if (actor == null || food == null) return;
        var life = LifeSystemsServices.Instance;
        var sheet = life != null ? life.GetOrCreate(actor) : actor.GetComponent<LifeSystemsSheet>();
        if (sheet == null)
            sheet = actor.AddComponent<LifeSystemsSheet>();
        sheet.EnsureDefaults();

        var profile = food.nutrients ?? new FoodNutrientProfile();
        if (profile.useExplicitNutrients)
        {
            sheet.Adjust01(LifeSystemsChannelCatalog.BloodSugar, profile.bloodSugarDelta01);
            sheet.Adjust01(LifeSystemsChannelCatalog.Vitamins, profile.vitaminsDelta01);
            sheet.Adjust01(LifeSystemsChannelCatalog.Hydration, profile.hydrationDelta01);
            sheet.Adjust01(LifeSystemsChannelCatalog.Lipids, profile.lipidsDelta01);
        }
        else if (adjustToNormalIngredients)
        {
            sheet.Adjust01(LifeSystemsChannelCatalog.BloodSugar, 0.04f);
            sheet.Adjust01(LifeSystemsChannelCatalog.Vitamins, 0.03f);
            sheet.Adjust01(LifeSystemsChannelCatalog.Hydration, 0.02f);
            PullTowardSetpoints(sheet, developerModification01);
        }

        sheet.bioRhythm?.ApplyAmplitudeDelta(0.05f);

        if (smellWhitelist != null)
            smellWhitelist.ApplyWhitelistedSmells(actor, food.smellTags);

        bool makePoop = createPoopByDefault && food.createPoopContribution;
        if (makePoop)
        {
            var bowel = BowelBladderRuntime.FindOrCreate(actor);
            bowel.AddBowelFill(bowelFillPerSwallow);
            sheet.Set01(LifeSystemsChannelCatalog.BowelFill, bowel.bowelFill01);
            // Queue stool payload; physical spawn happens at excrete.
            int seed = actor.GetComponent<DeveloperRespectsSeed>()?.Seed ?? food.GetInstanceID();
            bowel.pendingPoop = CreatePoopPayload(food, seed);
            if (autoQueueToiletBt && bowel.bowelFill01 >= bowelToiletThreshold)
                bowel.QueueToiletOrFreeExcrete();
        }
    }

    /// <summary>
    /// Explicit create-poop factory (wetness/smell/texture/seed). Spawns a runtime instance parented under actor (or world).
    /// </summary>
    public PoopRuntime CreatePoop(
        GameObject actor,
        float wetness01,
        float smell01,
        float textureScale,
        int seed,
        Transform parent = null)
    {
        PoopRuntime pr;
        if (poopPrefab != null)
        {
            var go = Object.Instantiate(poopPrefab.gameObject);
            pr = go.GetComponent<PoopRuntime>() ?? go.AddComponent<PoopRuntime>();
        }
        else
        {
            var go = new GameObject("Poop");
            pr = go.AddComponent<PoopRuntime>();
        }

        pr.wetness01 = Mathf.Clamp01(wetness01);
        pr.smell01 = Mathf.Clamp01(smell01);
        pr.textureScale = Mathf.Max(0.01f, textureScale);
        pr.coilSeed = seed;
        if (parent != null)
            pr.transform.SetParent(parent, false);
        else if (actor != null)
            pr.transform.position = actor.transform.position + Vector3.down * 0.1f;

        LastCreatedPoop = pr;
        return pr;
    }
    // todo: dodo
    // hehehehe
    public PoopPayload CreatePoopPayload(FoodItem food, int seed)
    {
        float wet = defaultPoopWetness01;
        float smell = defaultPoopSmell01;
        float tex = defaultPoopTextureScale;
        if (food != null)
        {
            // Kind nudges.
            if (food.kind == FoodKind.FruitVegetable) wet = Mathf.Clamp01(wet + 0.1f);
            if (food.kind == FoodKind.Meat) smell = Mathf.Clamp01(smell + 0.05f);
            if (food.smellTags != null && food.smellTags.Count > 0)
                smell = Mathf.Clamp01(smell + 0.1f);
        }
        return new PoopPayload
        {
            wetness01 = wet,
            smell01 = smell,
            textureScale = tex,
            seed = seed
        };
    }

    /// <summary>Spawn from payload into a bowl (or ground if bowl null) and clear bowel fill.</summary>
    public PoopRuntime SpawnPoopFromPayload(GameObject actor, PoopPayload payload, Transform bowl)
    {
        if (payload == null) return null;
        var pr = CreatePoop(actor, payload.wetness01, payload.smell01, payload.textureScale, payload.seed, null);
        pr.SpawnInBowl(bowl, payload.seed);
        var bowel = actor != null ? BowelBladderRuntime.FindOrCreate(actor) : null;
        if (bowel != null)
        {
            bowel.bowelFill01 = 0f;
            bowel.pendingPoop = null;
            var sheet = actor.GetComponent<LifeSystemsSheet>();
            sheet?.Set01(LifeSystemsChannelCatalog.BowelFill, 0f);
        }
        return pr;
    }

    static void PullTowardSetpoints(LifeSystemsSheet sheet, float strength01)
    {
        if (sheet == null) return;
        float k = Mathf.Clamp01(strength01);
        Pull(sheet, LifeSystemsChannelCatalog.BloodSugar, k);
        Pull(sheet, LifeSystemsChannelCatalog.Vitamins, k);
        Pull(sheet, LifeSystemsChannelCatalog.Hydration, k);
        Pull(sheet, LifeSystemsChannelCatalog.Lipids, k);
    }

    static void Pull(LifeSystemsSheet sheet, string channelId, float k)
    {
        if (!LifeSystemsChannelCatalog.TryGet(channelId, out var def) || def == null) return;
        float cur = sheet.Get01(channelId);
        float target = def.setpoint01;
        sheet.Set01(channelId, Mathf.Lerp(cur, target, k));
    }
}

/// <summary>Queued stool parameters produced on swallow.</summary>
[System.Serializable]
public sealed class PoopPayload
{
    public float wetness01 = 0.4f;
    public float smell01 = 0.5f;
    public float textureScale = 1f;
    public int seed;
}
