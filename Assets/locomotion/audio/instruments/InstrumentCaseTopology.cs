using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    public enum InstrumentCaseState
    {
        ClosedEmpty,
        OpenEmpty,
        OpenSeated,
        ClosedSeated
    }

    /// <summary>
    /// Optional case open/close + insert/remove topological search.
    /// Mechanical open is driven externally (e.g. Locomotion.Open InstrumentOpenCloseBridge → SyncFromOpen01).
    /// </summary>
    public sealed class InstrumentCaseTopology : MonoBehaviour
    {
        public bool enableInstrumentCaseTopology;
        public Transform caseRoot;
        public Transform cavityAnchor;
        public float insertSnapRadius = 0.35f;
        public InstrumentCaseState state = InstrumentCaseState.ClosedEmpty;
        [Range(0f, 1f)] public float openThreshold01 = 0.5f;
        /// <summary>Optional topology asset (OpenCloseTopologyAsset) assigned from Open bridge / editor.</summary>
        public ScriptableObject caseOpenCloseTopology;

        public bool CaseOpen =>
            state == InstrumentCaseState.OpenEmpty || state == InstrumentCaseState.OpenSeated;

        public bool InstrumentSeated =>
            state == InstrumentCaseState.OpenSeated || state == InstrumentCaseState.ClosedSeated;

        public void SyncFromOpen01(float open01)
        {
            if (!enableInstrumentCaseTopology) return;
            SetCaseOpen(open01 >= openThreshold01);
        }

        public void SetCaseOpen(bool open)
        {
            if (!enableInstrumentCaseTopology) return;
            if (open)
                state = InstrumentSeated ? InstrumentCaseState.OpenSeated : InstrumentCaseState.OpenEmpty;
            else
                state = InstrumentSeated ? InstrumentCaseState.ClosedSeated : InstrumentCaseState.ClosedEmpty;
        }

        /// <summary>Topological search: nearest tagged instrument within snap radius of cavity.</summary>
        public bool TryInsertNearest(string instrumentTag = "MusicalInstrument")
        {
            if (!enableInstrumentCaseTopology || !CaseOpen || cavityAnchor == null)
                return false;

            var hits = Physics.OverlapSphere(cavityAnchor.position, insertSnapRadius);
            Transform best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].transform;
                if (t == null || !t.CompareTag(instrumentTag)) continue;
                float d = Vector3.Distance(t.position, cavityAnchor.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
            if (best == null) return false;
            best.SetParent(cavityAnchor, true);
            best.localPosition = Vector3.zero;
            best.localRotation = Quaternion.identity;
            state = InstrumentCaseState.OpenSeated;
            return true;
        }

        public bool TryRemove(Transform instrument)
        {
            if (!enableInstrumentCaseTopology || !InstrumentSeated || instrument == null)
                return false;
            instrument.SetParent(null, true);
            state = CaseOpen ? InstrumentCaseState.OpenEmpty : InstrumentCaseState.ClosedEmpty;
            return true;
        }

        public List<Collider> SearchCavityNeighbors()
        {
            var list = new List<Collider>();
            if (!enableInstrumentCaseTopology || cavityAnchor == null) return list;
            var hits = Physics.OverlapSphere(cavityAnchor.position, insertSnapRadius);
            list.AddRange(hits);
            return list;
        }
    }
}
