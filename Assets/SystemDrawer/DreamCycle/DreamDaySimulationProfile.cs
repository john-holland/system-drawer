using System;
using Locomotion.DreamCycle;
using UnityEngine;

namespace SystemDrawer.DreamCycle
{
    [Serializable]
    public struct GoodDayHorizonSettings
    {
        [Range(0f, 1f)] public float minSatisfied;
        [Range(0f, 1f)] public float maxSatisfied;
        [Range(0f, 1f)] public float blendSocietyWeight;

        public static GoodDayHorizonSettings Default => new GoodDayHorizonSettings
        {
            minSatisfied = 0.72f,
            maxSatisfied = 0.92f,
            blendSocietyWeight = 0.85f
        };
    }

    /// <summary>Tunables for double-day dream simulation and safe LSTM refrain.</summary>
    [CreateAssetMenu(fileName = "DreamDaySimulationProfile", menuName = "System Drawer/Dream Day Simulation Profile")]
    public sealed class DreamDaySimulationProfile : ScriptableObject
    {
        public bool doubleDayEnabled = true;
        public GoodDayHorizonSettings goodDayHorizon = GoodDayHorizonSettings.Default;

        [TextArea(2, 8)]
        public string dreamDayPrompt;

        public DreamSafeRefrainSettings safeRefrain = DreamSafeRefrainSettings.Default;
    }
}
