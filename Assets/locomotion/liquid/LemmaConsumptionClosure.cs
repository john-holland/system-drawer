using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Locomotion.Liquid
{
    /// <summary>Evaluates drink beat closure from ledger snapshot and lemma properties.</summary>
    public sealed class LemmaConsumptionClosure : MonoBehaviour
    {
        public LiquidConsumptionLedger ledger;
        public AnimationPlaybackPolicyContext policyContext;
        public LemmaConsumptionRegistry registry;
        public float spillBeatThresholdLiters = 0.02f;
        public float vesselEmptyEpsilon = 0.005f;

        float _beatStartTime;
        DrinkClosureMode _resolvedMode = DrinkClosureMode.Auto;

        public event Action<LiquidConsumptionSnapshot, DrinkClosureMode> OnClosed;

        void Awake()
        {
            if (ledger == null)
                ledger = GetComponent<LiquidConsumptionLedger>();
            if (policyContext == null)
                policyContext = GetComponent<AnimationPlaybackPolicyContext>()
                                ?? GetComponentInParent<AnimationPlaybackPolicyContext>();
        }

        public void BeginBeat(DrinkLemmaProperties props)
        {
            _beatStartTime = Time.time;
            _resolvedMode = ResolveEffectiveMode(props);
            ledger?.ResetBeat();
            if (props.SuppressDispense)
                ledger?.SetDispenseSuppressed(true);
            if (ledger != null)
                ledger.infiniteDrain = props.infiniteDrain;
        }

        public bool TryClose(DrinkLemmaProperties props, out DrinkClosureMode closedMode)
        {
            closedMode = DrinkClosureMode.Auto;
            if (ledger == null)
                return false;

            var snap = ledger.Snapshot;
            var mode = ResolveEffectiveMode(props);

            if (Evaluate(snap, props, mode))
            {
                closedMode = mode;
                ledger.ApplyVesselDebit();
                MarkPhraseConsumed();
                OnClosed?.Invoke(snap, mode);
                LiquidCausalityBridge.NotifyClosed(mode);
                return true;
            }
            return false;
        }

        DrinkClosureMode ResolveEffectiveMode(DrinkLemmaProperties props)
        {
            if (props.closureMode != DrinkClosureMode.Auto)
                return props.closureMode;

            if (props.infiniteDrain || HasLemmaInPhrase("endless"))
                return DrinkClosureMode.InfiniteDrainBeat;
            if (HasLemmaInPhrase("stalled"))
                return DrinkClosureMode.Stalled;
            if (HasLemmaInPhrase("spilled"))
                return DrinkClosureMode.SpillBeat;
            if (props.SuppressDispense)
                return DrinkClosureMode.Stalled;
            return DrinkClosureMode.Mouth;
        }

        bool Evaluate(LiquidConsumptionSnapshot snap, DrinkLemmaProperties props, DrinkClosureMode mode)
        {
            float mouthTarget = props.mouthVolumeLitersTarget > 0f
                ? props.mouthVolumeLitersTarget
                : props.VolumePerSipLiters * Mathf.Max(1, props.sipCount);

            switch (mode)
            {
                case DrinkClosureMode.Stalled:
                    return snap.raiseCompleted && snap.dispenseSuppressed;
                case DrinkClosureMode.Mouth:
                    return snap.mouthReceivedLiters >= mouthTarget * 0.5f || snap.sipsAttempted >= props.sipCount;
                case DrinkClosureMode.EmptyVessel:
                    return snap.vesselRemainingLiters <= vesselEmptyEpsilon;
                case DrinkClosureMode.SpillBeat:
                    return snap.spillLiters >= spillBeatThresholdLiters || snap.sipsAttempted >= props.sipCount;
                case DrinkClosureMode.InfiniteDrainBeat:
                    if (props.infiniteDrainClosureSeconds > 0f &&
                        Time.time - _beatStartTime >= props.infiniteDrainClosureSeconds)
                        return true;
                    return snap.spillLiters >= spillBeatThresholdLiters * 3f ||
                           snap.mouthReceivedLiters >= mouthTarget;
                default:
                    return false;
            }
        }

        bool HasLemmaInPhrase(string term)
        {
            if (policyContext == null || string.IsNullOrEmpty(term))
                return false;
            string script = policyContext.GetActiveScriptText() ?? "";
            string phrase = policyContext.activePhrase ?? "";
            return script.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   phrase.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void MarkPhraseConsumed()
        {
            if (policyContext == null)
                return;
            if (registry == null)
                registry = policyContext.ConsumptionRegistry;
            registry?.MarkConsumed(policyContext.activePhrase, policyContext.activeEventIndex);
        }
    }
}
