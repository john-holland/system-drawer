using System;
using System.Collections.Generic;
using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public enum AsteroidMutationKind
    {
        Destroyed,
        Mined,
        TractorMoved,
        TeleportMined
    }

    [Serializable]
    public struct AsteroidBeltMutation
    {
        public int sectorIndex;
        public int slotIndex;
        public AsteroidMutationKind kind;
        public float timestamp;
        public Vector3 deltaPosition;
        public int destructionSeed;
    }

    [CreateAssetMenu(fileName = "AsteroidBeltMutationLog", menuName = "Planetary/Asteroid Belt Mutation Log")]
    public sealed class AsteroidBeltMutationLog : ScriptableObject
    {
        public int beltSeed = 12345;
        public List<AsteroidBeltMutation> mutations = new List<AsteroidBeltMutation>();

        public void Record(AsteroidBeltMutation mutation)
        {
            mutations.Add(mutation);
        }

        public void Clear() => mutations.Clear();

        public bool IsSlotDestroyed(int sectorIndex, int slotIndex)
        {
            for (int i = 0; i < mutations.Count; i++)
            {
                var m = mutations[i];
                if (m.sectorIndex == sectorIndex && m.slotIndex == slotIndex
                    && (m.kind == AsteroidMutationKind.Destroyed || m.kind == AsteroidMutationKind.TeleportMined))
                    return true;
            }
            return false;
        }

        public bool TryGetMutation(int sectorIndex, int slotIndex, out AsteroidBeltMutation mutation)
        {
            for (int i = mutations.Count - 1; i >= 0; i--)
            {
                if (mutations[i].sectorIndex == sectorIndex && mutations[i].slotIndex == slotIndex)
                {
                    mutation = mutations[i];
                    return true;
                }
            }
            mutation = default;
            return false;
        }
    }
}
