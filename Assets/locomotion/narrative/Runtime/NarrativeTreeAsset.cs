using UnityEngine;

namespace Locomotion.Narrative
{
    [AddComponentMenu("Locomotion/Narrative/Narrative Tree")]
    public class NarrativeTreeAsset : MonoBehaviour
    {
        [Header("Schema")]
        public int schemaVersion = 1;

        [Header("Tree")]
        [SerializeReference]
        public NarrativeNode root = new NarrativeSequenceNode { title = "Root" };
    }
}

