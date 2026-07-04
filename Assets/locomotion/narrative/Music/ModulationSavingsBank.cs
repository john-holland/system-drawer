using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>Logarithmic modulation savings — earned harmonic distance before key flips.</summary>
    public sealed class ModulationSavingsBank
    {
        public int currentTonic;
        public float savings;
        public int fifthStepsInRow;
        public int historySize = 8;

        readonly Queue<int> _keyHistory = new Queue<int>();

        public float baseModCost = 1f;
        public float savingsDecayK = 2f;
        public float oscillationWeight = 0.5f;

        public void OnSectionAdvance(int nextTonic, int barsHeld)
        {
            nextTonic = MusicTheory.NormalizeTonic(nextTonic);
            int prev = MusicTheory.NormalizeTonic(currentTonic);

            if (prev == nextTonic)
            {
                savings += Mathf.Log(1f + Mathf.Max(0, barsHeld));
            }
            else if (MusicTheory.IsFifthStep(prev, nextTonic))
            {
                fifthStepsInRow++;
                savings += Mathf.Log(1f + fifthStepsInRow);
            }
            else
            {
                fifthStepsInRow = 0;
            }

            _keyHistory.Enqueue(nextTonic);
            while (_keyHistory.Count > historySize)
                _keyHistory.Dequeue();

            currentTonic = nextTonic;
        }

        public float ModulationSpend(int targetTonic)
        {
            targetTonic = MusicTheory.NormalizeTonic(targetTonic);
            if (targetTonic == MusicTheory.NormalizeTonic(currentTonic))
                return 0f;

            float distance = MusicTheory.TonalDistance(currentTonic, targetTonic);
            float spend = baseModCost * distance * Mathf.Exp(-savings / Mathf.Max(0.001f, savingsDecayK));
            return spend;
        }

        public float Credit(int targetTonic) => Mathf.Max(0f, savings - ModulationSpend(targetTonic));

        public float OscillationPenalty(int targetTonic)
        {
            targetTonic = MusicTheory.NormalizeTonic(targetTonic);
            if (_keyHistory.Count < 2) return 0f;

            int[] arr = _keyHistory.ToArray();
            int last = arr[arr.Length - 1];
            if (arr.Length >= 2 && arr[arr.Length - 2] == targetTonic && last != targetTonic)
                return oscillationWeight * (1f + savings);
            return 0f;
        }
    }
}
