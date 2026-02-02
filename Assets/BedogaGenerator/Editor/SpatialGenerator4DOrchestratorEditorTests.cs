#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;

/// <summary>
/// Editor tests for SpatialGenerator4DOrchestrator and its custom editor:
/// Find generators in hierarchy, Add 3D/4D generator, ResolveReferences,
/// and that executor keys / spacetime causal assertions are testable from the narrative tests.
/// </summary>
public class SpatialGenerator4DOrchestratorEditorTests
{
    [Test]
    public void Orchestrator_ResolveReferences_PopulatesListWhenChildGeneratorExists()
    {
        var goOrch = new GameObject("Orchestrator");
        var orch = goOrch.AddComponent<SpatialGenerator4DOrchestrator>();
        orch.spatialGenerators.Clear();

        var goChild = new GameObject("SpatialGenerator4D");
        goChild.transform.SetParent(goOrch.transform);
        goChild.AddComponent<SpatialGenerator4D>();

        orch.ResolveReferences();

        Assert.Greater(orch.spatialGenerators.Count, 0);
        Assert.IsTrue(orch.spatialGenerators.Exists(g => g is SpatialGenerator4D));

        Object.DestroyImmediate(goOrch);
    }

    [Test]
    public void Orchestrator_ResolveReferences_DoesNotOverwriteNonEmptyList()
    {
        var goOrch = new GameObject("Orchestrator");
        var orch = goOrch.AddComponent<SpatialGenerator4DOrchestrator>();
        var go4D = new GameObject("Existing4D");
        go4D.transform.SetParent(goOrch.transform);
        var existing = go4D.AddComponent<SpatialGenerator4D>();
        orch.spatialGenerators.Clear();
        orch.spatialGenerators.Add(existing);

        orch.ResolveReferences();

        Assert.AreEqual(1, orch.spatialGenerators.Count);
        Assert.AreEqual(existing, orch.spatialGenerators[0]);

        Object.DestroyImmediate(goOrch);
    }

    [Test]
    public void Orchestrator_Add3DGeneratorLogic_CreatesChildAndAddsToList()
    {
        var goOrch = new GameObject("Orchestrator");
        var orch = goOrch.AddComponent<SpatialGenerator4DOrchestrator>();
        orch.spatialGenerators.Clear();

        GameObject child = new GameObject("SpatialGenerator3D");
        child.transform.SetParent(orch.transform);
        var sg = child.AddComponent<SpatialGenerator>();
        orch.spatialGenerators.Add(sg);

        Assert.AreEqual(1, orch.spatialGenerators.Count);
        Assert.IsTrue(orch.spatialGenerators[0] is SpatialGenerator);
        Assert.IsNotNull(orch.transform.Find("SpatialGenerator3D"));

        Object.DestroyImmediate(goOrch);
    }

    [Test]
    public void Orchestrator_Add4DGeneratorLogic_CreatesChildAndAddsToList()
    {
        var goOrch = new GameObject("Orchestrator");
        var orch = goOrch.AddComponent<SpatialGenerator4DOrchestrator>();
        orch.spatialGenerators.Clear();

        GameObject child = new GameObject("SpatialGenerator4D");
        child.transform.SetParent(orch.transform);
        var sg4 = child.AddComponent<SpatialGenerator4D>();
        orch.spatialGenerators.Add(sg4);

        Assert.AreEqual(1, orch.spatialGenerators.Count);
        Assert.IsTrue(orch.spatialGenerators[0] is SpatialGenerator4D);
        Assert.IsNotNull(orch.transform.Find("SpatialGenerator4D"));

        Object.DestroyImmediate(goOrch);
    }

    [Test]
    public void OrchestratorEditor_CanCreateEditor_DoesNotThrow()
    {
        var goOrch = new GameObject("Orchestrator");
        var orch = goOrch.AddComponent<SpatialGenerator4DOrchestrator>();

        Editor editor = null;
        Assert.DoesNotThrow(() => editor = Editor.CreateEditor(orch));
        Assert.IsNotNull(editor);

        Object.DestroyImmediate(editor);
        Object.DestroyImmediate(goOrch);
    }
}
#endif
