#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class KissingHeavyPettingTests
{
    [Test]
    public void Mouth_GetLipLoopMidpoint_PrefersRimThenJaw()
    {
        var go = new GameObject("MouthHost");
        try
        {
            var mouth = go.AddComponent<MouthInteriorRuntime>();
            var rim = new GameObject("Rim");
            rim.transform.SetParent(go.transform, false);
            rim.transform.position = new Vector3(1f, 2f, 3f);
            var loop = go.AddComponent<MouthExteriorEdgeLoop>();
            loop.rimCenter = rim.transform;
            mouth.salivaLoop = loop;

            Vector3 mid = mouth.GetLipLoopMidpointWorld();
            Assert.AreEqual(rim.transform.position, mid);

            var anchor = mouth.EnsureLipMidAnchor();
            Assert.IsNotNull(anchor);
            Assert.AreEqual(mid.x, anchor.position.x, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Registry_TryGet_AndSectionMask_RoundTrip()
    {
        var host = new GameObject("RegistryHost");
        var actor = new GameObject("Actor");
        try
        {
            var reg = host.AddComponent<HeavyPettingIKActorRegistry>();
            var entry = new HeavyPettingIKActorEntry
            {
                actorKey = "egon",
                actor = actor,
                enabledRagdollSections = new[] { "Head" },
                disabledRagdollSections = new[] { "LeftFoot" }
            };
            reg.entries.Add(entry);
            reg.AutoResolveAll();

            Assert.IsTrue(reg.TryGet("egon", out var found));
            Assert.AreEqual(actor, found.actor);

            var en = new HashSet<string>();
            var dis = new HashSet<string>();
            reg.ResolveSectionMask(found, en, dis);
            Assert.IsTrue(en.Contains("Head"));
            Assert.IsTrue(dis.Contains("LeftFoot"));
            Assert.IsNull(found.openCloseTopology);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void AnimationGroup_IntensityBands_AndAuthoredKeyWins()
    {
        Assert.AreEqual(LoveMakingAnimationGroup.KissPeck, LoveMakingAnimationGroup.ForKiss(0.1f));
        Assert.AreEqual(LoveMakingAnimationGroup.Kiss, LoveMakingAnimationGroup.ForKiss(0.35f));
        Assert.AreEqual(LoveMakingAnimationGroup.KissSmooch, LoveMakingAnimationGroup.ForKiss(0.55f));
        Assert.AreEqual(LoveMakingAnimationGroup.KissMakingOut, LoveMakingAnimationGroup.ForKiss(0.9f));
        Assert.AreEqual("slimer-kiss", LoveMakingAnimationGroup.ForKiss(0.55f, "slimer-kiss"));
        Assert.AreEqual("slimer-kiss.intimate",
            LoveMakingAnimationGroup.ForKiss(0.55f, "slimer-kiss", intimateStyle: true));
    }

    [Test]
    public void Lemma_PaintCard_SetsKeyAndIntensity()
    {
        var card = LoveCard.Generate(LoveMakingMode.Playful, LoveMakingMoveKind.Embrace, null, null);
        var props = new Dictionary<string, string>
        {
            { LoveMakingKissLemmaPropertyKeys.KissAnimation, "slimer-kiss" }
        };
        LoveMakingKissLemmaPropertyResolver.PaintCard(card, props, "smooch");
        Assert.AreEqual(LoveMakingMoveKind.Kiss, card.loveMoveKind);
        Assert.AreEqual("slimer-kiss", card.kissAnimationKey);
        Assert.AreEqual(LoveMakingAnimationGroup.SmoochIntensity, card.kissAnimationIntensity, 1e-4f);
        Assert.AreEqual("slimer-kiss", card.LoveAnimationGroupTag);
    }

    [Test]
    public void LemmaDefaults_MapPeckKissSmoochMakingOut()
    {
        Assert.AreEqual(LoveMakingAnimationGroup.PeckIntensity,
            LoveMakingAnimationGroup.DefaultIntensityForLemma("peck"), 1e-4f);
        Assert.AreEqual(LoveMakingAnimationGroup.DefaultKissIntensity,
            LoveMakingAnimationGroup.DefaultIntensityForLemma("kiss"), 1e-4f);
        Assert.AreEqual(LoveMakingAnimationGroup.SmoochIntensity,
            LoveMakingAnimationGroup.DefaultIntensityForLemma("smooching"), 1e-4f);
        Assert.AreEqual(LoveMakingAnimationGroup.MakingOutIntensity,
            LoveMakingAnimationGroup.DefaultIntensityForLemma("making out"), 1e-4f);
    }

    [Test]
    public void PsychEffect_Kiss_RaisesSerotoninScaledByIntensity()
    {
        var a = new GameObject("A");
        var b = new GameObject("B");
        try
        {
            var sheetA = a.AddComponent<LifeSystemsSheet>();
            sheetA.EnsureDefaults();
            float beforeS = sheetA.Get01(LifeSystemsChannelCatalog.Serotonin);
            float beforeO = sheetA.Get01(LifeSystemsChannelCatalog.Oxytocin);

            var session = a.AddComponent<LoveMakingSession>();
            session.Begin(new[] { a, b }, 10f, null);
            var card = LoveCard.Generate(LoveMakingMode.Passionate, LoveMakingMoveKind.Kiss, b, null,
                kissAnimationIntensity: 0.85f);
            LoveMakingPsychEffectService.Apply(session, a, b, card);

            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Serotonin), beforeS);
            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Oxytocin), beforeO);
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void PsychEffect_UnrequitedPoorResponse_DropsBpAndRaisesReflux()
    {
        var a = new GameObject("A");
        var b = new GameObject("B");
        try
        {
            var sheetA = a.AddComponent<LifeSystemsSheet>();
            sheetA.EnsureDefaults();
            float bpBefore = sheetA.GetClinical(LifeSystemsChannelCatalog.BloodPressureSys);
            float refluxBefore = sheetA.Get01(LifeSystemsChannelCatalog.Reflux);

            var pb = b.AddComponent<RomanceProfile>();
            pb.direction = RomanceDirection.Unrequited;
            pb.severity = RomanceSeverity.FriendZone;
            pb.harshRejectionResponse = true;

            var session = a.AddComponent<LoveMakingSession>();
            session.Begin(new[] { a, b }, 10f, null);
            var card = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Kiss, b, null,
                kissAnimationIntensity: 0.5f);
            card.kissResponseNegative = true;
            LoveMakingPsychEffectService.Apply(session, a, b, card);

            Assert.Less(sheetA.GetClinical(LifeSystemsChannelCatalog.BloodPressureSys), bpBefore);
            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Reflux), refluxBefore);
            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Acidity), 0f);
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void KissingExecution_BeginEnd_CreatesAndClearsTows()
    {
        var a = new GameObject("Actor");
        var b = new GameObject("Partner");
        try
        {
            a.AddComponent<MouthInteriorRuntime>();
            b.AddComponent<MouthInteriorRuntime>();
            var card = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Kiss, b, null);
            Assert.IsTrue(KissingExecution.Begin(a, b, card, null));
            Assert.IsTrue(KissingExecution.IsActive(a));
            KissingExecution.Tick(a, 0.02f);
            Assert.Less(KissingExecution.LipDistance(a), float.MaxValue);
            KissingExecution.End(a);
            Assert.IsFalse(KissingExecution.IsActive(a));
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void HeavyPettingIKAnimation_MatchesIntensityBand()
    {
        var asset = ScriptableObject.CreateInstance<HeavyPettingIKAnimation>();
        try
        {
            asset.minIntensity01 = 0.7f;
            asset.maxIntensity01 = 1f;
            Assert.IsTrue(asset.MatchesIntensity(0.85f));
            Assert.IsFalse(asset.MatchesIntensity(0.2f));
            Assert.AreEqual(PhysicsIKTrainingCategory.LoveHeavyPetting, asset.category);
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }
}
#endif
