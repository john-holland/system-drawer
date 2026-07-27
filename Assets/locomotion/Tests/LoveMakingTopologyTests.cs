#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LoveMakingTopologyTests
{
    [Test]
    public void LoveCard_InheritsWrestlingCard_AndFlagsLoveGoal()
    {
        var card = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Embrace, null, null);
        Assert.IsInstanceOf<WrestlingCard>(card);
        Assert.IsTrue(card.isLoveMakingGoal);
        Assert.IsFalse(card.isWrestlingGoal);
        Assert.IsTrue(card.physicalPathingTag.StartsWith("lovemaking"));
    }

    [Test]
    public void MeetsLoveRequirements_ConsentGate_BlocksWithoutProfileConsent()
    {
        var actor = new GameObject("Actor");
        var partner = new GameObject("Partner");
        try
        {
            var profile = partner.AddComponent<RomanceProfile>();
            profile.severity = RomanceSeverity.FriendZone;
            profile.direction = RomanceDirection.Unannounced;
            profile.defaultConsentWithSteadyPartner = false;

            var card = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Kiss, partner, null);
            card.requiresConsent = true;
            Assert.IsFalse(card.MeetsLoveRequirements(actor, partner, null));

            profile.consentedPartners.Add(actor);
            Assert.IsTrue(card.MeetsLoveRequirements(actor, partner, null));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(partner);
        }
    }

    [Test]
    public void PlannerSolver_PicksCardCoveringDesireGoal()
    {
        var root = new GameObject("LoveSession");
        var partner = new GameObject("Partner");
        try
        {
            var session = root.AddComponent<LoveMakingSession>();
            session.Begin(new[] { root, partner }, 20f, new List<LoveMakingTopologyGoal>
            {
                new LoveMakingTopologyGoal
                {
                    kind = LoveMakingTopologyGoalKind.DesireMet,
                    desire = LoveDesire.Closeness,
                    targetValue = 0.5f
                },
                new LoveMakingTopologyGoal
                {
                    kind = LoveMakingTopologyGoalKind.AnimationTag,
                    animationTag = LoveMakingAnimationGroup.ForMove(LoveMakingMoveKind.Embrace, false)
                }
            });

            var embrace = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Embrace, partner, null);
            var part = LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Part, partner, null);
            var result = LoveMakingPlannerSolver.Solve(session, new List<LoveCard> { part, embrace }, root, partner);
            Assert.IsTrue(result.feasible);
            Assert.IsNotNull(result.sequence);
            Assert.IsTrue(result.sequence.Count >= 1);
            Assert.AreEqual(LoveMakingMoveKind.Embrace, result.sequence[0].loveMoveKind);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(partner);
        }
    }

    [Test]
    public void PsychEffect_AdjustsRomanceChannels()
    {
        var a = new GameObject("A");
        var b = new GameObject("B");
        try
        {
            var sheetA = a.AddComponent<LifeSystemsSheet>();
            sheetA.EnsureDefaults();
            float before = sheetA.Get01(LifeSystemsChannelCatalog.Affection);

            var session = a.AddComponent<LoveMakingSession>();
            session.Begin(new[] { a, b }, 10f, null);
            var card = LoveCard.Generate(LoveMakingMode.Passionate, LoveMakingMoveKind.Caress, b, null);
            LoveMakingPsychEffectService.Apply(session, a, b, card);

            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Affection), before);
            Assert.Greater(sheetA.Get01(LifeSystemsChannelCatalog.Intimacy), 0f);
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void SocietalImpact_DefaultIsOneOverPopulation()
    {
        var go = new GameObject("Society");
        try
        {
            var svc = go.AddComponent<RomanceSocietalImpactService>();
            svc.populationEstimate = 5000f;
            svc.useSocietalImpactOverride = false;
            Assert.AreEqual(1f / 5000f, svc.ResolveImpact(), 1e-6f);

            svc.useSocietalImpactOverride = true;
            svc.societalImpactOverride = 1f;
            Assert.AreEqual(1f, svc.ResolveImpact(), 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LemmaWatch_ParsesNonIkPromptDirective()
    {
        Assert.IsTrue(LemmaWatch.TryParsePromptDirective(
            "looking {P=whistful|non-ik-animation=true}", out var pose, out var nonIk));
        Assert.AreEqual("whistful", pose);
        Assert.IsTrue(nonIk);
    }
}
#endif
