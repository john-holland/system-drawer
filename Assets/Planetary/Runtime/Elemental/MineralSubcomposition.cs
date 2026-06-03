using System;
using UnityEngine;

namespace Planetary.Elemental
{
    [Serializable]
    public struct MineralWeight
    {
        public string mineralId;
        public float weight;
    }

    [Serializable]
    public class MineralStack
    {
        public MineralWeight[] weights = Array.Empty<MineralWeight>();

        public float GetWeight(string mineralId)
        {
            if (weights == null)
                return 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i].mineralId == mineralId)
                    return weights[i].weight;
            }
            return 0f;
        }
    }
}
