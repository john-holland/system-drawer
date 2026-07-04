using System.Collections.Generic;

using Locomotion.Liquid;

using Locomotion.Liquid.Flood;

using UnityEngine;



namespace Locomotion.Drink

{

    /// <summary>BT node: drink from vessel with sip-count loop, partial raise, and consumption closure.</summary>

    public sealed class DrinkFromVesselNode : BehaviorTreeNode

    {

        [Header("Drink")]

        public GameObject vessel;

        public string task = "drink";

        public DrinkLemmaProperties lemmaOverrides;



        int _sipsCompleted;

        DrinkVesselComponent _vesselComp;

        DrinkFlowModel _flowModel;

        LiquidConsumptionLedger _ledger;

        LemmaConsumptionClosure _closure;

        CabinTurbulenceDriver _turbulence;

        RollingSphereFloodSimulator _floodSim;

        DrinkSpillSurfacePool _spillPool;

        List<GoodSection> _cards;

        int _cardIndex;

        bool _beatStarted;



        public override BehaviorTreeStatus Execute(BehaviorTree tree)

        {

            if (vessel == null)

                return BehaviorTreeStatus.Failure;



            var consider = tree.GetComponent<Consider>();

            if (consider == null)

                return BehaviorTreeStatus.Failure;



            var policy = tree.GetComponent<AnimationPlaybackPolicyContext>();

            if (policy != null && policy.IsPhraseConsumed(policy.activePhrase, policy.activeEventIndex))

                return BehaviorTreeStatus.Success;



            var props = ResolveProps(tree);



            if (_cards == null)

            {

                props = ApplyPartialRaise(tree, props);

                _vesselComp = vessel.GetComponentInChildren<DrinkVesselComponent>();

                _flowModel = vessel.GetComponentInChildren<DrinkFlowModel>();

                _ledger = tree.GetComponent<LiquidConsumptionLedger>() ?? tree.GetComponentInChildren<LiquidConsumptionLedger>();

                _closure = tree.GetComponent<LemmaConsumptionClosure>() ?? tree.GetComponentInChildren<LemmaConsumptionClosure>();

                _turbulence = tree.GetComponent<CabinTurbulenceDriver>() ?? tree.GetComponentInChildren<CabinTurbulenceDriver>();

                _floodSim = vessel.GetComponentInChildren<RollingSphereFloodSimulator>();

                _spillPool = tree.GetComponentInChildren<DrinkSpillSurfacePool>();



                if (_flowModel != null)

                    _flowModel.infiniteDrain = props.infiniteDrain;



                _cards = DrinkToolUsageCardGenerator.Generate(

                    consider, vessel, task,

                    tree.GetComponent<RagdollSystem>()?.GetCurrentState() ?? new RagdollState(),

                    props);

                _cardIndex = 0;

                _sipsCompleted = 0;

                _beatStarted = false;

            }



            if (_cards == null || _cards.Count == 0)

                return BehaviorTreeStatus.Failure;



            if (!_beatStarted)

            {

                _closure?.BeginBeat(props);

                _turbulence?.SetActiveForBeat(HasTurbulenceLemma(policy));

                _beatStarted = true;

            }



            if (_ledger != null && _turbulence != null)

                _ledger.turbulencePenalty = _turbulence.TurbulenceIntensity01;



            while (_cardIndex < _cards.Count)

            {

                var card = _cards[_cardIndex];

                _cardIndex++;



                if (card != null && card.sectionName != null && card.sectionName.StartsWith("raise_"))

                {

                    _ledger?.MarkRaiseCompleted();

                    continue;

                }



                if (card != null && card.sectionName != null && card.sectionName.StartsWith("sip_"))

                {

                    _sipsCompleted++;

                    _ledger?.RecordSipAttempt();

                    var aligner = tree.GetComponentInChildren<DrinkMouthJawAligner>();

                    aligner?.BeginSip(props);



                    float q = _flowModel != null ? _flowModel.ComputeInstantaneousFlowLitersPerSecond() : 0f;

                    _ledger?.RecordDispense(q, 0.25f, props.drinkEfficacy);

                    if (_ledger != null && _spillPool != null)
                    {
                        float spill = q * 0.25f * (1f - Mathf.Clamp01(props.drinkEfficacy));
                        _spillPool.SpawnSpill(_flowModel?.StreamTipPosition() ?? vessel.transform.position, spill);
                    }

                    _ledger?.ApplyVesselDebit();



                    if (_flowModel != null)

                    {

                        Vector3 force = _flowModel.StreamTipForward() * q;

                        _flowModel.SyncManifoldVelocity(force);

                        _ledger?.PublishStreamToManifold(_flowModel.StreamTipPosition(), force, _flowModel.handPressurePa);

                    }



                    _floodSim?.EmitFromFlow(q);

                }

            }



            if (_closure != null && _closure.TryClose(props, out _))

                return BehaviorTreeStatus.Success;



            if (!props.infiniteDrain && _vesselComp != null && _vesselComp.currentVolumeLiters <= 0f && _sipsCompleted > 0)

                return BehaviorTreeStatus.Success;



            return _cards != null && _cardIndex >= _cards.Count

                ? BehaviorTreeStatus.Success

                : BehaviorTreeStatus.Running;

        }



        static bool HasTurbulenceLemma(AnimationPlaybackPolicyContext policy)

        {

            if (policy == null)

                return false;

            string s = policy.GetActiveScriptText() ?? "";

            return s.IndexOf("turbulence", System.StringComparison.OrdinalIgnoreCase) >= 0;

        }



        DrinkLemmaProperties ApplyPartialRaise(BehaviorTree tree, DrinkLemmaProperties props)

        {

            var policy = tree.GetComponent<AnimationPlaybackPolicyContext>();

            if (policy == null)

                return props;



            float raise = LiquidPartialRaiseResolver.Resolve(

                props,

                policy.GetSegmentsForActivePhrase(),

                policy.GetBindingsForActivePhrase(),

                policy.GetActiveScriptText());



            props.partiallyRaiseAmount = raise;

            if (LiquidPartialRaiseResolver.ShouldSuppressDispense(

                    props, policy.GetActiveScriptText(), policy.GetBindingsForActivePhrase()))

            {

                props.closureMode = DrinkClosureMode.Stalled;

            }

            return props;

        }



        DrinkLemmaProperties ResolveProps(BehaviorTree tree)

        {

            var policy = tree.GetComponent<AnimationPlaybackPolicyContext>();

            var resolved = policy != null ? policy.GetDrinkProperties() : DrinkLemmaProperties.Defaults;

            if (lemmaOverrides.sipCount > 0)

                resolved.sipCount = lemmaOverrides.sipCount;

            if (lemmaOverrides.drinkEfficacy > 0f)

                resolved.drinkEfficacy = lemmaOverrides.drinkEfficacy;

            if (lemmaOverrides.infiniteDrain)

                resolved.infiniteDrain = true;

            return resolved;

        }

    }

}


