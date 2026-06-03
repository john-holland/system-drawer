using System;
using UnityEngine;

namespace Planetary.Elemental
{
    [CreateAssetMenu(fileName = "ElementalRule", menuName = "Planetary/Elemental Rule")]
    public sealed class ElementalRule : ScriptableObject
    {
        [Tooltip("All tags must be present on MaterialSpec")]
        public string[] requiredTags = Array.Empty<string>();

        public MineralWeight[] outputWeights = Array.Empty<MineralWeight>();

        public bool Matches(MaterialSpec spec)
        {
            if (requiredTags == null || requiredTags.Length == 0)
                return true;
            if (spec.tags == null)
                return false;
            for (int i = 0; i < requiredTags.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < spec.tags.Length; j++)
                {
                    if (spec.tags[j] == requiredTags[i])
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
    }
}
