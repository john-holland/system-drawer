using Locomotion.Liquid;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class LemmaConsumptionClosureTests
    {
        [Test]
        public void StalledMode_ClosesWhenRaiseCompletedAndDispenseSuppressed()
        {
            var actor = new GameObject("actor");
            var policy = actor.AddComponent<AnimationPlaybackPolicyContext>();
            policy.activeScriptText = "{stalled} cup hovers";
            var ledger = actor.AddComponent<LiquidConsumptionLedger>();
            var closure = actor.AddComponent<LemmaConsumptionClosure>();
            closure.ledger = ledger;
            closure.policyContext = policy;
            closure.registry = policy.ConsumptionRegistry;

            var props = DrinkLemmaProperties.Defaults;
            props.closureMode = DrinkClosureMode.Stalled;
            closure.BeginBeat(props);
            ledger.MarkRaiseCompleted();
            ledger.SetDispenseSuppressed(true);

            Assert.IsTrue(closure.TryClose(props, out var mode));
            Assert.AreEqual(DrinkClosureMode.Stalled, mode);
            Assert.IsTrue(policy.IsPhraseConsumed(policy.activePhrase, policy.activeEventIndex));
            Object.DestroyImmediate(actor);
        }

        [Test]
        public void SpillBeat_ClosesWhenSpillExceedsThreshold()
        {
            var actor = new GameObject("actor");
            var policy = actor.AddComponent<AnimationPlaybackPolicyContext>();
            var ledger = actor.AddComponent<LiquidConsumptionLedger>();
            var closure = actor.AddComponent<LemmaConsumptionClosure>();
            closure.ledger = ledger;
            closure.policyContext = policy;
            closure.spillBeatThresholdLiters = 0.02f;

            var props = DrinkLemmaProperties.Defaults;
            props.closureMode = DrinkClosureMode.SpillBeat;
            props.sipCount = 1;
            closure.BeginBeat(props);
            ledger.RecordDispense(0.5f, 1f, 0.1f);

            Assert.IsTrue(closure.TryClose(props, out var mode));
            Assert.AreEqual(DrinkClosureMode.SpillBeat, mode);
            Object.DestroyImmediate(actor);
        }

        [Test]
        public void Registry_MarksPhraseConsumedOnClose()
        {
            var registry = new LemmaConsumptionRegistry();
            Assert.IsFalse(registry.IsConsumed("beat1", 0));
            registry.MarkConsumed("beat1", 0);
            Assert.IsTrue(registry.IsConsumed("beat1", 0));
        }
    }
}
