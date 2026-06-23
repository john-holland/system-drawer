using UnityEngine;

/// <summary>Optional tag on prefabs — links runtime spawns to a lemma entry for component reporting.</summary>
public class ContinuumLemmaReportTag : MonoBehaviour
{
    [Tooltip("Thesaurus entry id for component-creation runtime reports")]
    public string entryId;

    [Tooltip("Optional prefab asset path for blueprint correlation")]
    public string prefabRef;
}
