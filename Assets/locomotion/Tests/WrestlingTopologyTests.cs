#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

public sealed class WrestlingTopologyTests
{
    [Test]
    public void SizeGate_HeavyActorHeavyOpp_Passes()
    {
        var actor = MakeMassBody("Actor", 120f, 2f);
        var opp = MakeMassBody("Opp", 100f, 1.8f);
        try
        {
            var gate = new WrestlingBodySizeGate
            {
                minOpponentMass = 40f,
                maxOpponentMass = 500f
            };
            Assert.IsTrue(gate.Passes(actor, opp, out _));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void SizeGate_LightOpp_FailsHighMinMass()
    {
        var actor = MakeMassBody("Rock", 110f, 2f);
        var baby = MakeMassBody("Baby", 8f, 0.3f);
        try
        {
            var gate = new WrestlingBodySizeGate { minOpponentMass = 25f };
            Assert.IsFalse(gate.Passes(actor, baby, out var reason));
            Assert.IsTrue(reason.Contains("mass"));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(baby);
        }
    }

    [Test]
    public void Card_RequiredLimbMissing_Infeasible()
    {
        var actor = new GameObject("Wrestler");
        var opp = MakeMassBody("Opp", 80f, 1.5f);
        try
        {
            var card = WrestlingCard.GenerateLock(opp, null);
            card.requiredLimbBones = new List<string> { "MissingBoneXYZ" };
            // No RagdollSystem → MeetsWrestlingRequirements treats limbs as present (true).
            // With ragdoll present but bone missing → false.
            var rd = actor.AddComponent<RagdollSystem>();
            Assert.IsFalse(card.MeetsWrestlingRequirements(actor, opp, rd));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void Planner_LiftBranch_RewritesToThrowTag()
    {
        var go = new GameObject("Planner");
        var opp = MakeMassBody("Opp", 90f, 1.7f);
        try
        {
            var planner = go.AddComponent<WrestlingPlannerService>();
            var lift = WrestlingCard.GenerateLift(opp, null);
            lift.liftBranch = WrestlingMoveKind.Throw;
            var rewritten = planner.RewriteToBranch(lift);
            Assert.AreEqual(WrestlingMoveKind.Throw, rewritten.moveKind);
            Assert.AreEqual(WrestlingAnimationGroup.Throw, rewritten.AnimationGroupTag);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void Counter_BespokeTag_Preferred()
    {
        var go = new GameObject("Planner");
        var opp = MakeMassBody("Opp", 90f, 1.7f);
        try
        {
            var planner = go.AddComponent<WrestlingPlannerService>();
            var card = WrestlingCard.GenerateCounter(opp, null);
            card.bespokeCounterAnimTag = "wrestling.counter.special";
            string tag = planner.ResolveCounterAnimTag(card, Vector3.forward);
            Assert.AreEqual("wrestling.counter.special", tag);
            Assert.AreEqual("wrestling.counter.special", card.AnimationGroupTag);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void SlowTimeSession_EnterCancel_RestoresScale()
    {
        var root = new GameObject("WrestleSession");
        try
        {
            float prev = Time.timeScale;
            Time.timeScale = 1f;
            var session = root.AddComponent<WrestlingCardSelectionSession>();
            var slow = root.AddComponent<SlowTimeController>();
            session.slowTime = slow;
            var opp = MakeMassBody("Opp", 80f, 1.5f);
            var card = WrestlingCard.GenerateLock(opp, null);
            session.Begin(new List<WrestlingCard> { card }, 0.25f);
            Assert.AreEqual(0.25f, Time.timeScale, 0.001f);
            Assert.IsTrue(session.slowTimeActive);
            session.Cancel();
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.IsFalse(session.slowTimeActive);
            Time.timeScale = prev;
            Object.DestroyImmediate(opp);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SlowTimeSession_HotkeySelectsMatchingMoveKind()
    {
        var root = new GameObject("WrestleHotkey");
        var opp = MakeMassBody("Opp", 80f, 1.5f);
        try
        {
            var session = root.AddComponent<WrestlingCardSelectionSession>();
            var lockCard = WrestlingCard.GenerateLock(opp, null);
            var throwCard = WrestlingCard.GenerateThrow(opp, null);
            session.Begin(new List<WrestlingCard> { lockCard, throwCard }, 0f);
            Assert.IsTrue(session.TrySelectMoveKind(WrestlingMoveKind.Throw));
            Assert.AreEqual(WrestlingMoveKind.Throw, session.selectedCard.moveKind);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void AngularSelect_PrefersNearerConeCenter()
    {
        var root = new GameObject("Angular");
        var camGo = new GameObject("Cam");
        var opp = new GameObject("Opp");
        try
        {
            camGo.transform.position = Vector3.zero;
            camGo.transform.LookAt(Vector3.forward);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            var select = root.AddComponent<AngularWrestlingCardSelectMode>();
            select.viewCamera = cam;
            select.opponent = opp;
            select.coneHalfAngleDegrees = 25f;

            var near = WrestlingCard.GenerateLock(opp, null);
            near.requiredLimbBones.Clear();
            var offAxis = WrestlingCard.GenerateThrow(opp, null);
            offAxis.requiredLimbBones.Clear();

            var nearAnchor = new GameObject("NearAnchor");
            nearAnchor.transform.position = new Vector3(0f, 0f, 2f);
            near.aimAnchorOverride = nearAnchor.transform;
            var offAnchor = new GameObject("OffAnchor");
            // Far outside cone so nearer on-axis wins when both otherwise valid.
            offAnchor.transform.position = new Vector3(10f, 0f, 2f);
            offAxis.aimAnchorOverride = offAnchor.transform;

            select.SetCandidates(new List<WrestlingCard> { offAxis, near });
            var ray = new Ray(Vector3.zero, Vector3.forward);
            Assert.IsTrue(select.TryScanRay(ray, out var hit, out _));
            Assert.AreEqual(WrestlingMoveKind.LockGrapple, hit.moveKind);

            // Nearer of two on-axis anchors wins.
            var farther = WrestlingCard.GenerateDropOn(opp, null);
            farther.requiredLimbBones.Clear();
            var farAnchor = new GameObject("FarOnAxis");
            farAnchor.transform.position = new Vector3(0f, 0f, 8f);
            farther.aimAnchorOverride = farAnchor.transform;
            select.SetCandidates(new List<WrestlingCard> { farther, near });
            Assert.IsTrue(select.TryScanRay(ray, out hit, out _));
            Assert.AreEqual(WrestlingMoveKind.LockGrapple, hit.moveKind);
            Object.DestroyImmediate(farAnchor);

            Object.DestroyImmediate(nearAnchor);
            Object.DestroyImmediate(offAnchor);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(opp);
        }
    }

    [Test]
    public void ClothUv_MaskBlocksSlip_ElasticRecovers_ContactRaisesA()
    {
        var go = new GameObject("Cloth");
        try
        {
            var driver = go.AddComponent<ClothUvStretchDriver>();
            var stickLayer = new ClothUvStretchLayer
            {
                layerId = "stick",
                elastic = new ClothElasticProperties { slideGain = 2f, maxSlipUv = 0.2f, recovery01 = 0.9f, friction01 = 0f }
            };
            // slideMask=0 via synthetic: Integrate with NotifyContact then zero mask by setting slideMaskTex null defaults to 1.
            // Use two layers — one with contact, check A; recovery clears slip.
            var slideLayer = new ClothUvStretchLayer
            {
                layerId = "slide",
                elastic = new ClothElasticProperties
                {
                    slideGain = 4f,
                    maxSlipUv = 0.2f,
                    recovery01 = 0.95f,
                    friction01 = 0f,
                    stiffness = 80f,
                    damping = 20f
                }
            };
            driver.layers.Add(slideLayer);
            driver.NotifyContact(go, 1f);
            driver.DebugIntegrate(0.02f, new Vector3(5f, 0f, 0f));
            Assert.Greater(slideLayer.contactWeight01, 0.5f);
            Assert.Greater(Mathf.Abs(slideLayer.slipUv.x) + Mathf.Abs(slideLayer.slipUv.y), 0f);
            Assert.NotNull(driver.Cache);
            Assert.Greater(driver.Cache.Texture.GetPixel(32, 32).a, 0.1f);

            // Recovery when contact ends
            driver.ClearContact();
            float slipBefore = slideLayer.slipUv.magnitude;
            for (int i = 0; i < 30; i++)
                driver.DebugIntegrate(0.02f, Vector3.zero);
            Assert.Less(slideLayer.slipUv.magnitude, slipBefore);

            // Stick mask: create readable texture with R=0
            var mask = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var black = new Color[16];
            for (int i = 0; i < black.Length; i++) black[i] = Color.black;
            mask.SetPixels(black);
            mask.Apply();
            slideLayer.slideMaskTex = mask;
            slideLayer.slipUv = new Vector2(0.1f, 0.1f);
            driver.NotifyContact(go, 1f);
            driver.DebugIntegrate(0.02f, new Vector3(10f, 0f, 0f));
            Assert.AreEqual(0f, slideLayer.slipUv.magnitude, 1e-4f);
            Object.DestroyImmediate(mask);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ClothUv_TwoLayers_SlideIndependently()
    {
        var go = new GameObject("Cloth2");
        try
        {
            var driver = go.AddComponent<ClothUvStretchDriver>();
            var a = new ClothUvStretchLayer
            {
                layerId = "a",
                elastic = new ClothElasticProperties { slideGain = 3f, friction01 = 0f, maxSlipUv = 0.2f }
            };
            var b = new ClothUvStretchLayer
            {
                layerId = "b",
                elastic = new ClothElasticProperties { slideGain = 0.5f, friction01 = 0.8f, maxSlipUv = 0.2f }
            };
            driver.layers.Add(a);
            driver.layers.Add(b);
            driver.NotifyContact(go, 1f);
            driver.DebugIntegrate(0.05f, new Vector3(8f, 0f, 2f));
            Assert.Greater(a.slipUv.magnitude, b.slipUv.magnitude);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void NarrativeBioRhythm_AppliesAdrenalineAndAmplitude()
    {
        var root = new GameObject("LifeActor");
        try
        {
            var life = root.AddComponent<LifeSystemsServices>();
            var sheet = life.GetOrCreate(root);
            float ampBefore = sheet.bioRhythm.amplitude01;
            float adrBefore = sheet.Get01(LifeSystemsChannelCatalog.Adrenaline);

            var bindings = root.AddComponent<NarrativeBindings>();
            bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "agent", value = root });
            bindings.RebuildIndex();
            var ctx = new NarrativeExecutionContext(null, bindings, null);
            var action = new NarrativeWrestlingBioRhythmAction
            {
                actorKey = "agent",
                bioRhythmAmplitudeDelta = 0.2f,
                adrenalineChannelDelta = 0.25f,
                queueWrestlingGoal = false
            };
            Assert.AreEqual(Locomotion.Narrative.BehaviorTreeStatus.Success, action.Execute(ctx, null));
            Assert.Greater(sheet.bioRhythm.amplitude01, ampBefore);
            Assert.Greater(sheet.Get01(LifeSystemsChannelCatalog.Adrenaline), adrBefore);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AnimationGroup_ProAppendsSuffix()
    {
        Assert.AreEqual("wrestling.throw", WrestlingAnimationGroup.ForMove(WrestlingMoveKind.Throw, false));
        Assert.AreEqual("wrestling.throw.pro", WrestlingAnimationGroup.ForMove(WrestlingMoveKind.Throw, true));
    }

    static GameObject MakeMassBody(string name, float mass, float scale)
    {
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        var rend = go.AddComponent<MeshRenderer>();
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (filter.sharedMesh == null)
        {
            // Fallback bounds via collider
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * scale;
        }
        _ = rend;
        return go;
    }
}
#endif
