using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Dockable launcher for System Drawer tools; mirrors facilitator inspector toolbox.</summary>
public class SystemDrawerFacilitatorHubWindow : EditorWindow
{
    private SystemDrawerFacilitator _facilitator;
    private SerializedObject _so;
    private string _adHocMenuPath = "";

    private const string HubMenu = "Window/System Drawer/Facilitator Hub";

    [MenuItem(HubMenu, false, 0)]
    public static void ShowWindow()
    {
        var win = GetWindow<SystemDrawerFacilitatorHubWindow>("SD Facilitator");
        win.RefreshTarget();
        win.Show();
    }

    private void OnFocus()
    {
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        _facilitator = UnityEngine.Object.FindAnyObjectByType<SystemDrawerFacilitator>(FindObjectsInactive.Exclude);
        _so = _facilitator != null ? new SerializedObject(_facilitator) : null;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("System Drawer Facilitator Hub", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open editor windows via menu paths below, or bind a facilitator in the scene.",
            MessageType.None);

        if (_facilitator == null || !_facilitator.gameObject.scene.IsValid())
            EditorGUILayout.HelpBox("No facilitator in loaded scenes.", MessageType.Info);
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Target: {_facilitator.name}", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Select", GUILayout.Width(80)))
                Selection.activeGameObject = _facilitator.gameObject;
            if (GUILayout.Button("Ping", GUILayout.Width(52)))
                EditorGUIUtility.PingObject(_facilitator);
            EditorGUILayout.EndHorizontal();
        }

        if (_facilitator == null &&
            GUILayout.Button("Create System Drawer Hub in Hierarchy", GUILayout.Height(28)))
            SystemDrawerHubSetup.CreateHierarchyHub();

        if (_facilitator != null)
        {
            if (_so != null && _so.targetObject != null)
                _so.Update();

            FacilitatorHubUi.DrawToolbox(_facilitator, _so, ref _adHocMenuPath);

            if (_so != null && _so.targetObject != null)
                _so.ApplyModifiedProperties();
        }
        else
            FacilitatorHubUi.DrawToolbox(null, null, ref _adHocMenuPath);
    }
}

internal static class FacilitatorHubUi
{
    private static readonly Dictionary<string, bool> FoldState = new Dictionary<string, bool>();

    internal static void DrawToolbox(SystemDrawerFacilitator fac, SerializedObject facilitatorSo,
        ref string adHocMenuPath)
    {
        DrawMenuCatalog(ref adHocMenuPath);

        if (facilitatorSo == null && fac != null)
            facilitatorSo = new SerializedObject(fac);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scene facilitation", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(facilitatorSo == null))
        {
            if (GUILayout.Button("Ensure child _Wizards + bind references", GUILayout.Height(24)))
            {
                if (fac != null && facilitatorSo != null)
                {
                    SystemDrawerFacilitatorEditorUtility.EnsureWizardsChildAndBind(fac, facilitatorSo);
                    facilitatorSo.Update();
                    fac.EnsureWizardReferencesFilled();
                    EditorUtility.SetDirty(fac);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull from SystemDrawerService"))
            {
                if (fac != null)
                {
                    fac.EnsureWizardReferencesFilled();
                    int n = fac.TryCacheFromService();
                    EditorUtility.SetDirty(fac);
                    Debug.Log($"[SystemDrawerFacilitator] Pull complete (TryComplete assignments: ~{n}).");
                }
            }

            if (GUILayout.Button("Push registrations"))
            {
                if (fac != null)
                {
                    fac.EnsureWizardReferencesFilled();
                    int n = fac.TryRegisterAllKnown();
                    EditorUtility.SetDirty(fac);
                    Debug.Log($"[SystemDrawerFacilitator] Push registrations applied (count≈{n}).");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.HelpBox(
            "Push overlaps each wizard OnEnable registrations; safe for fill-in when service order is uncertain.",
            MessageType.None);
    }

    private static void DrawMenuCatalog(ref string adHocMenuPath)
    {
        EditorGUILayout.LabelField("Open window", EditorStyles.boldLabel);
        adHocMenuPath = EditorGUILayout.DelayedTextField("Menu path (ad-hoc)", adHocMenuPath);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open ad-hoc path", GUILayout.Width(160)))
        {
            if (!string.IsNullOrWhiteSpace(adHocMenuPath) &&
                !EditorApplication.ExecuteMenuItem(adHocMenuPath.Trim()))
                Debug.LogWarning($"[SystemDrawerFacilitator] Menu not found: {adHocMenuPath}");
        }

        EditorGUILayout.EndHorizontal();

        foreach (var cat in SystemDrawerHubMenuCatalog.All.Select(e => e.Category).Distinct())
        {
            if (!FoldState.TryGetValue(cat, out var fold))
                fold = true;
            fold = EditorGUILayout.Foldout(fold, cat, true);
            FoldState[cat] = fold;
            if (!fold)
                continue;
            EditorGUI.indentLevel++;
            foreach (var entry in SystemDrawerHubMenuCatalog.All.Where(e => e.Category == cat))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(entry.Label, GUILayout.ExpandWidth(true)))
                {
                    if (!EditorApplication.ExecuteMenuItem(entry.MenuPath))
                        Debug.LogWarning($"[SystemDrawerFacilitator] Menu not found: {entry.MenuPath}");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}

/// <summary>Creates root + <see cref="SystemDrawerService"/> + <see cref="SystemDrawerFacilitator"/> + _Wizards child.</summary>
internal static class SystemDrawerHubSetup
{
    private const int MenuPriority = 9;

    [MenuItem("GameObject/System Drawer/System Drawer Hub", false, MenuPriority)]
    private static void CreateUnderSelection(MenuCommand command)
    {
        var parent = command.context as GameObject;
        if (parent == null && Selection.activeGameObject != null)
            parent = Selection.activeGameObject;
        CreateHubInternal(parent != null ? parent.transform : null);
    }

    [MenuItem("GameObject/System Drawer/System Drawer Hub", true)]
    private static bool ValidateCreateUnderSelection()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    internal static void CreateHierarchyHub()
    {
        CreateHubInternal(null);
    }

    private static void CreateHubInternal(Transform parent)
    {
        var root = new GameObject("SystemDrawer");
        Undo.RegisterCreatedObjectUndo(root, "Create System Drawer Hub");
        if (parent != null)
        {
            Undo.RecordObject(root.transform, "Parent System Drawer Hub");
            root.transform.SetParent(parent, false);
        }
        var svc = Undo.AddComponent<SystemDrawerService>(root);
        var fac = Undo.AddComponent<SystemDrawerFacilitator>(root);
        var w = new GameObject("_Wizards");
        Undo.RegisterCreatedObjectUndo(w, "Create _Wizards");
        Undo.RecordObject(w.transform, "Parent _Wizards");
        w.transform.SetParent(root.transform, false);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(svc);
        EditorUtility.SetDirty(fac);
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[SystemDrawerHub] Created SystemDrawer root with service, facilitator, and _Wizards child.");
    }
}
