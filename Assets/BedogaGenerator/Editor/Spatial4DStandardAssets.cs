using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Spatial4DStandardAssets
{
    public static WizardSetupReport Setup(Spatial4DServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        SpatialGenerator4DOrchestrator orch = wizard.orchestrator;
        if (orch == null)
            orch = Object.FindFirstObjectByType<SpatialGenerator4DOrchestrator>(FindObjectsInactive.Include);

        if (orch == null)
        {
            var parent = ResolveParent(wizard);
            var go = FindOrCreateChild(parent, "Spatial4DOrchestrator", report);
            orch = FindOrAddComponent<SpatialGenerator4DOrchestrator>(go, report);
        }
        else
        {
            report.Skipped.Add("SpatialGenerator4DOrchestrator");
        }

        SpatialGenerator4D gen4d = orch.GetComponentInChildren<SpatialGenerator4D>(true);
        if (gen4d == null)
        {
            var child = FindOrCreateChild(orch.transform, "SpatialGenerator4D", report);
            gen4d = FindOrAddComponent<SpatialGenerator4D>(child, report);
            if (orch.spatialGenerators != null && !orch.spatialGenerators.Contains(gen4d))
            {
                Undo.RecordObject(orch, "Add 4D generator to list");
                orch.spatialGenerators.Add(gen4d);
                EditorUtility.SetDirty(orch);
                report.Linked.Add("Orchestrator.spatialGenerators");
            }
        }
        else
        {
            report.Skipped.Add("SpatialGenerator4D");
        }

        if (wizard.orchestrator != orch)
        {
            Undo.RecordObject(wizard, "Assign orchestrator");
            wizard.orchestrator = orch;
            EditorUtility.SetDirty(wizard);
            report.Linked.Add("Spatial4DServiceWizard.orchestrator");
        }

        MarkSceneDirty(wizard);
        return report;
    }

    static Transform ResolveParent(Spatial4DServiceWizard wizard)
    {
        var fac = wizard.GetComponentInParent<SystemDrawerFacilitator>();
        if (fac != null)
        {
            var scene = fac.transform.Find("_StandardScene");
            if (scene != null)
                return scene;
            var created = new GameObject("_StandardScene");
            Undo.RegisterCreatedObjectUndo(created, "Create _StandardScene");
            Undo.SetTransformParent(created.transform, fac.transform, "Parent _StandardScene");
            MarkSceneDirty(created);
            return created.transform;
        }

        var svc = wizard.GetComponentInParent<SystemDrawerService>();
        return svc != null ? svc.transform : wizard.transform;
    }

    static GameObject FindOrCreateChild(Transform parent, string name, WizardSetupReport report)
    {
        var existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
        {
            report.Skipped.Add("GameObject " + name);
            return existing.gameObject;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        if (parent != null)
            Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        report.Created.Add("GameObject " + name);
        MarkSceneDirty(go);
        return go;
    }

    static T FindOrAddComponent<T>(GameObject go, WizardSetupReport report) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null)
        {
            report.Skipped.Add(typeof(T).Name + " on " + go.name);
            return c;
        }

        c = Undo.AddComponent<T>(go);
        report.Created.Add(typeof(T).Name + " on " + go.name);
        EditorUtility.SetDirty(go);
        MarkSceneDirty(go);
        return c;
    }

    static void MarkSceneDirty(Component c)
    {
        if (c != null && c.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
    }

    static void MarkSceneDirty(GameObject go)
    {
        if (go != null && go.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
