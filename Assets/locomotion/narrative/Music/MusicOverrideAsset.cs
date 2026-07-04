using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [CreateAssetMenu(fileName = "MusicOverride", menuName = "Locomotion/Narrative/Music Override", order = 12)]
    public sealed class MusicOverrideAsset : ScriptableObject
    {
        public string causalityLeafId;
        public string questObjectiveId;
        public string forceKey = "C";
        public float forceBpm = 120f;
        public string[] forceSectionIds;
        [Range(0f, 1f)] public float smoothness = 0.8f;
        [Range(0f, 1f)] public float awkwardness;
        public bool proceduralChaos;
    }
}
