using System;
using SystemDrawer.DreamCycle;
using SystemDrawer.Quest;
using UnityEditor;
using UnityEngine;

internal static class SystemDrawerFacilitatorEditorUtility
{
    internal static void EnsureWizardsChildAndBind(SystemDrawerFacilitator f, SerializedObject so)
    {
        if (f == null || so == null)
            return;

        var tr = f.transform.Find("_Wizards");
        GameObject child;
        if (tr == null)
        {
            child = new GameObject("_Wizards");
            Undo.RegisterCreatedObjectUndo(child, "Ensure _Wizards");
            Undo.RecordObject(child.transform, "Ensure _Wizards Parent");
            child.transform.SetParent(f.transform, false);
        }
        else
            child = tr.gameObject;

        var svc = f.GetComponent<SystemDrawerService>();
        if (svc == null)
            svc = Undo.AddComponent<SystemDrawerService>(f.gameObject);
        var pSvc = so.FindProperty("service");
        if (pSvc != null)
            pSvc.objectReferenceValue = svc;

        EnsureBind<CalendarServiceWizard>(child, so, "calendarWizard");
        EnsureBind<NarrativePromptServiceWizard>(child, so, "narrativePromptWizard");
        EnsureBind<RagdollServiceWizard>(child, so, "ragdollWizard");
        EnsureBind<UscBuildServiceWizard>(child, so, "uscBuildWizard");
        EnsureBind<WeatherServiceWizardComponent>(child, so, "weatherWizard");
        EnsureBind<PlanetServiceWizardComponent>(child, so, "planetWizard");
        EnsureBind<QuestServiceWizard>(child, so, "questServiceWizard");
        EnsureBind<DreamCycleServiceWizard>(child, so, "dreamServiceWizard");

        EnsureBindNetworkingWizards(child, so);

        var spatialType = ResolveSpatialWizardType();
        if (spatialType != null)
        {
            Component spatialComp = child.GetComponent(spatialType);
            if (spatialComp == null)
                spatialComp = Undo.AddComponent(child, spatialType);
            var pSp = so.FindProperty("spatial4DServiceWizard");
            if (pSp != null)
                pSp.objectReferenceValue = spatialComp;
        }

        /* BrainMessageService / SystemDrawerAnimator / AmbulatingActorRegistrar: assign manually — need scene context */
    }

    private static void EnsureBind<T>(GameObject go, SerializedObject so, string propName)
        where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null)
            c = Undo.AddComponent<T>(go);
        var p = so.FindProperty(propName);
        if (p != null)
            p.objectReferenceValue = c;
    }

    private static void EnsureBindNetworkingWizards(GameObject child, SerializedObject so)
    {
        var netType = Type.GetType("NetworkServiceWizard, SystemDrawer.Networking");
        if (netType != null)
            EnsureBindByType(child, so, "networkServiceWizard", netType);

        var menuType = Type.GetType("MenuRagdollServiceWizard, SystemDrawer.Networking");
        if (menuType != null)
            EnsureBindByType(child, so, "menuRagdollServiceWizard", menuType);
    }

    private static void EnsureBindByType(GameObject go, SerializedObject so, string propName, Type componentType)
    {
        var c = go.GetComponent(componentType);
        if (c == null)
            c = Undo.AddComponent(go, componentType);
        var p = so.FindProperty(propName);
        if (p != null)
            p.objectReferenceValue = c;
    }

    private static Type ResolveSpatialWizardType()
    {
        var t = Type.GetType("Spatial4DServiceWizard, BedogaGenerator");
        if (t != null)
            return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetName().Name != "BedogaGenerator")
                continue;
            t = asm.GetType("Spatial4DServiceWizard");
            if (t != null)
                return t;
        }

        return null;
    }
}
