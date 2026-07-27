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
            // Mild healthy meal deltas then pull toward setpoints.
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
            if (autoQueueToiletBt && bowel.bowelFill01 >= bowelToiletThreshold)
                bowel.QueueToiletOrFreeExcrete();
        }
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
