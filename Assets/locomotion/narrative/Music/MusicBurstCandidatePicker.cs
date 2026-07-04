using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>Physics-style burst selector for procedural chaos sections.</summary>
    public sealed class MusicBurstCandidatePicker
    {
        public bool enabled = true;
        public int candidateCount = 8;
        public int survivorCount = 3;
        public float attractionWeight = 1f;
        public float repulsionWeight = 0.8f;
        public int simulationSteps = 12;

        readonly List<(MusicSectionAsset section, Vector3 pos, Vector3 vel)> _particles = new List<(MusicSectionAsset, Vector3, Vector3)>();

        public MusicSectionAsset Pick(
            IReadOnlyList<MusicSectionAsset> candidates,
            MusicSectionAsset current,
            RhythmMeterTemplate rhythmFrom,
            RhythmMeterTemplate rhythmTo,
            TransitionScorer scorer,
            ModulationSavingsBank bank)
        {
            if (!enabled || candidates == null || candidates.Count == 0)
                return null;

            _particles.Clear();
            int n = Mathf.Min(candidateCount, candidates.Count);
            for (int i = 0; i < n; i++)
            {
                MusicSectionAsset c = candidates[i % candidates.Count];
                _particles.Add((c, Random.insideUnitSphere * 0.5f, Random.insideUnitSphere * 0.2f));
            }

            for (int step = 0; step < simulationSteps; step++)
            {
                for (int i = 0; i < _particles.Count; i++)
                {
                    var (sec, pos, vel) = _particles[i];
                    Vector3 force = Vector3.zero;

                    if (current != null)
                    {
                        float harm = 1f - MusicTheory.TonalDistance(current.TonicPc, sec.TonicPc);
                        force += Vector3.forward * harm * attractionWeight;
                    }

                    for (int j = 0; j < _particles.Count; j++)
                    {
                        if (i == j) continue;
                        float rhythmClash = MusicTheory.RhythmMismatch(rhythmFrom, rhythmTo);
                        Vector3 diff = pos - _particles[j].pos;
                        if (diff.sqrMagnitude > 0.001f)
                            force += diff.normalized * repulsionWeight * rhythmClash;
                    }

                    vel = (vel + force * 0.1f) * 0.92f;
                    pos += vel * 0.05f;
                    _particles[i] = (sec, pos, vel);
                }
            }

            MusicSectionAsset best = null;
            float bestScore = float.MaxValue;
            int take = Mathf.Min(survivorCount, _particles.Count);

            for (int i = 0; i < take; i++)
            {
                MusicSectionAsset sec = _particles[i].section;
                float cost = current != null
                    ? scorer.Score(current, sec, rhythmFrom, rhythmTo, bank)
                    : 0f;
                if (cost < bestScore)
                {
                    bestScore = cost;
                    best = sec;
                }
            }

            return best;
        }
    }
}
