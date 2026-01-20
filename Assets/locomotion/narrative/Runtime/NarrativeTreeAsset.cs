using UnityEngine;

namespace Locomotion.Narrative
{
    [CreateAssetMenu(menuName = "Locomotion/Narrative/Narrative Tree", fileName = "NarrativeTree")]
    public class NarrativeTreeAsset : ScriptableObject
    {
        [Header("Schema")]
        public int schemaVersion = 1;

        [Header("Tree")]
        [SerializeReference]
        public NarrativeNode root = new NarrativeSequenceNode { title = "Root" };
    }
}

