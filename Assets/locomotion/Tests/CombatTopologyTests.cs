#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatTopologyTests
{
    [Test]
    public void CombatCard_FlagsCombatGoal()
    {
        var card = CombatCard.Generate(CombatMode.Melee, CombatMoveKind.Strike, null);
        Assert.IsTrue(card.isCombatGoal);
        Assert.IsTrue(card.physicalPathingTag.StartsWith("combat"));
        Assert.AreEqual(CombatAnimationGroup.Strike, card.CombatAnimationGroupTag);
    }

    [Test]
    public void Ward_AbsorbsMatchingDamage()
    {
        var go = new GameObject("Defender");
        try
        {
            var wards = go.AddComponent<DefendWardRuntime>();
            wards.activeWards.Add(new DefendWard
            {
                absorb01 = 1f,
                blockConeDeg = 180f,
                absorbs = new List<CombatDamageType> { CombatDamageType.Blunt }
            });
            var evt = new CombatDamageEvent
            {
                target = go,
                type = CombatDamageType.Blunt,
                direction = -go.transform.forward,
                amount01 = 0.5f,
                createWound = false
            };
            float absorbed = wards.TryAbsorb(evt, 0.5f);
            Assert.Greater(absorbed, 0.4f);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void DamageApplier_PerLimb_AndOverall()
    {
        var go = new GameObject("Victim");
        try
        {
            var limbs = go.AddComponent<LimbIntegrityState>();
            limbs.EnsureDefaults();
            float before = limbs.GetHealth01("RightArm");
            var evt = new CombatDamageEvent
            {
                target = go,
                type = CombatDamageType.Blunt,
                limbId = "RightArm",
                amount01 = 0.2f,
                healthMode = DamageHealthMode.PerLimb,
                createWound = false
            };
            var r = CombatDamageApplier.Apply(evt);
            Assert.IsTrue(r.ok);
            Assert.Less(limbs.GetHealth01("RightArm"), before);

            float overallBefore = limbs.overallHealth01;
            evt.healthMode = DamageHealthMode.Overall;
            evt.amount01 = 0.1f;
            CombatDamageApplier.Apply(evt);
            Assert.Less(limbs.overallHealth01, overallBefore);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void DamageMask_BlocksActorTrauma()
    {
        var go = new GameObject("Armored");
        try
        {
            var mask = go.AddComponent<DamageMask>();
            mask.layers.Add(new DamageMaskLayer { absorb01 = 1f });
            var limbs = go.AddComponent<LimbIntegrityState>();
            limbs.EnsureDefaults();
            float before = limbs.GetHealth01("Chest");
            var r = CombatDamageApplier.Apply(new CombatDamageEvent
            {
                target = go,
                amount01 = 0.4f,
                type = CombatDamageType.Bullet,
                createWound = false
            });
            Assert.IsTrue(r.fullyBlockedByMask);
            Assert.AreEqual(before, limbs.GetHealth01("Chest"), 1e-4f);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void CutTool_IntervalEmitsEvents()
    {
        var toolGo = new GameObject("Saw");
        var target = new GameObject("Target");
        target.AddComponent<BoxCollider>().size = Vector3.one * 2f;
        target.transform.position = toolGo.transform.position + Vector3.forward;
        try
        {
            var tool = toolGo.AddComponent<CutToolComponent>();
            tool.cutInterval = 0.05f;
            tool.range = 3f;
            tool.active = false;
            tool.EmitAt(target, target.transform.position, Vector3.forward);
            tool.EmitAt(target, target.transform.position, Vector3.forward);
            Assert.AreEqual(2, tool.emitCount);
        }
        finally
        {
            Object.DestroyImmediate(toolGo);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void SafetyLock_GatesInsufficientForce()
    {
        var card = CombatCard.Generate(CombatMode.VehicleWeapon, CombatMoveKind.Fire, null);
        card.instrumentProxy.useProxyInstrument = true;
        card.instrumentProxy.safetyLockForceN = 22.24f;
        card.instrumentProxy.appliedForce01 = 0.1f; // ~5 N < 22
        var warden = new GameObject("W").AddComponent<SafetyLockWardenPlannerService>();
        try
        {
            Assert.IsFalse(warden.GateFire(card));
            card.instrumentProxy.appliedForce01 = 1f;
            Assert.IsTrue(warden.GateFire(card));
        }
        finally { Object.DestroyImmediate(warden.gameObject); }
    }

    [Test]
    public void PlannerSolver_FeasibleSequence()
    {
        var root = new GameObject("Fighter");
        var foe = new GameObject("Foe");
        try
        {
            var session = root.AddComponent<CombatSession>();
            session.Begin(new[] { root, foe }, 15f, null);
            var strike = CombatCard.Generate(CombatMode.Melee, CombatMoveKind.Strike, foe);
            var block = CombatCard.Generate(CombatMode.Melee, CombatMoveKind.Block, foe);
            var result = CombatPlannerSolver.Solve(session, new List<CombatCard> { strike, block }, root, foe);
            Assert.IsTrue(result.feasible);
            Assert.Greater(result.sequence.Count, 0);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(foe);
        }
    }
}
#endif
