using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Narrative action: resolve a spatial generator from bindings (targetKey) and call Generate() on 3D generators.
/// Optional reference to SpatialGeneratorBase or orchestrator; when null, resolved from targetKey (GameObject with generator).
/// </summary>
[System.Serializable]
public class TriggerSpatialGenerateAction : NarrativeActionSpec
{
    [Tooltip("NarrativeBindings key for the GameObject that has a SpatialGenerator (3D) or SpatialGenerator4D. If empty, uses orchestrator in scene.")]
    public string targetKey = "spatialGenerator";
    [Tooltip("When true, run Generate() on 3D SpatialGenerator. When false, no-op for 3D; 4D uses Insert from calendar event volume elsewhere.")]
    public bool trigger3DGenerate = true;

    public override Locomotion.Narrative.BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
    {
        if (!contingency.Evaluate(ctx))
            return Locomotion.Narrative.BehaviorTreeStatus.Success;

        SpatialGeneratorBase gen = null;
        if (!string.IsNullOrEmpty(targetKey) && ctx.TryResolveGameObject(targetKey, out var go) && go != null)
            gen = go.GetComponent<SpatialGeneratorBase>();
        if (gen == null)
        {
            var orch = Object.FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
            if (orch != null && orch.spatialGenerators != null)
            {
                foreach (var g in orch.spatialGenerators)
                {
                    if (g is SpatialGenerator sg3 && trigger3DGenerate) { gen = sg3; break; }
                    if (gen == null && g is SpatialGenerator4D) gen = g;
                }
            }
        }
        if (gen == null)
            return Locomotion.Narrative.BehaviorTreeStatus.Failure;
        if (trigger3DGenerate && gen is SpatialGenerator sg)
        {
            sg.Generate();
            return Locomotion.Narrative.BehaviorTreeStatus.Success;
        }
        return Locomotion.Narrative.BehaviorTreeStatus.Success;
    }
}
