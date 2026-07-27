#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BathroomHygieneTests
{
    [Test]
    public void PaperScroll_PullSheets_ReducesCount()
    {
        var go = new GameObject("scroll");
        try
        {
            var scroll = go.AddComponent<PaperScrollSystem>();
            scroll.sheetsRemaining = 10;
            float len = scroll.PullSheets(4);
            Assert.AreEqual(6, scroll.sheetsRemaining);
            Assert.AreEqual(4 * scroll.sheetLengthM, len, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ToiletStation_BidetDefaultTrue()
    {
        var go = new GameObject("toilet");
        try
        {
            var t = go.AddComponent<ToiletStation>();
            Assert.IsTrue(t.includesBidet);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PeeJitter_Zero_DisablesCurve()
    {
        var go = new GameObject("pee");
        try
        {
            var pee = go.AddComponent<PeeStreamDirector>();
            pee.peeDirectionJitterDegrees = 0f;
            var bladder = go.AddComponent<BowelBladderRuntime>();
            bladder.bladderFill01 = 1f;
            pee.bowelBladder = bladder;
            pee.BeginRelease(1);
            Assert.AreEqual(0f, pee.peeDirectionJitterDegrees);
            pee.EndRelease();
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SmellWhitelist_BlocksUnknown_AllowsGarlic()
    {
        var wl = ScriptableObject.CreateInstance<EatingSmellWhitelist>();
        try
        {
            Assert.IsTrue(wl.IsAllowed("garlic"));
            Assert.IsFalse(wl.IsAllowed("mystery_perfume"));
        }
        finally
        {
            Object.DestroyImmediate(wl);
        }
    }

    [Test]
    public void FoodProcessor_AdjustsTowardSetpoints()
    {
        var actor = new GameObject("eater");
        var foodGo = new GameObject("food");
        try
        {
            var life = actor.AddComponent<LifeSystemsServices>();
            var proc = actor.AddComponent<FoodProcessorBioRhythmService>();
            proc.adjustToNormalIngredients = true;
            proc.createPoopByDefault = false;
            proc.developerModification01 = 0.5f;
            var sheet = life.GetOrCreate(actor);
            sheet.Set01(LifeSystemsChannelCatalog.Vitamins, 0.1f);
            var food = foodGo.AddComponent<FoodItem>();
            food.nutrients = new FoodNutrientProfile { useExplicitNutrients = false };
            proc.OnSwallow(actor, food);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Vitamins), 0.1f);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(foodGo);
        }
    }

    [Test]
    public void HygieneSmellClear_ZerosEmitters()
    {
        var go = new GameObject("smelly");
        try
        {
            var e = go.AddComponent<Locomotion.Senses.SmellEmitter>();
            e.signature = "garlic";
            e.intensity = 1f;
            e.emissionMultiplier = 1f;
            HygieneSmellClearService.ClearAllOn(go);
            Assert.AreEqual(0f, e.emissionMultiplier);
            Assert.AreEqual(0f, e.intensity);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
