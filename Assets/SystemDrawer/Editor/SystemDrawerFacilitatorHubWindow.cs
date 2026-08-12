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
    private Vector2 _scroll;

    private const string HubMenu = "Window/System Drawer/Facilitator Hub";

    [MenuItem(HubMenu, false, 0)]
    public static void ShowWindow()
    {
        var win = GetWindow<SystemDrawerFacilitatorHubWindow>("SD Facilitator");
        win.minSize = new Vector2(400f, 300f);
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

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
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
        EditorGUILayout.EndScrollView();

        FacilitatorHubUi.DrawDimensionFooter();
    }
}

internal static class FacilitatorHubUi
{
    private static readonly Dictionary<string, bool> FoldState = new Dictionary<string, bool>();
    private const string PrefDim = "SystemDrawer.GameDimension.Dim";
    private const string PrefGame = "SystemDrawer.GameDimension.Game";
    private const string PrefApi = "SystemDrawer.GameDimension.ApiBase";

    internal static void DrawDimensionFooter()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
        int dim = EditorPrefs.GetInt(PrefDim, 0);
        string game = EditorPrefs.GetString(PrefGame, "main");
        string api = EditorPrefs.GetString(PrefApi, "http://127.0.0.1:5050");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Current: game={game} dim={dim}", GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        game = EditorGUILayout.TextField("Game", game);
        api = EditorGUILayout.TextField("API base", api);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PrefGame, game);
            EditorPrefs.SetString(PrefApi, api);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Dim 0", EditorStyles.toolbarButton))
            SwitchDimEditor(0, game, api);
        if (GUILayout.Button("Dim 1", EditorStyles.toolbarButton))
            SwitchDimEditor(1, game, api);
        if (GUILayout.Button("Prewarm SG2D/3D/4D", EditorStyles.toolbarButton))
            PrewarmEditor(dim, game, api);
        EditorGUILayout.EndHorizontal();
    }

    static void SwitchDimEditor(int dim, string game, string api)
    {
        EditorPrefs.SetInt(PrefDim, dim);
        EditorPrefs.SetString(PrefGame, game);
        PostGd(api, "/api/gd/dimension-switch", $"{{\"game\":\"{game}\",\"dimension\":{dim}}}");
        if (Application.isPlaying)
        {
            var cache = UnityEngine.Object.FindAnyObjectByType<DimensionSwitchCache>();
            if (cache != null)
                cache.StartCoroutine(cache.SwitchToDimension(dim));
        }
        Debug.Log($"[FacilitatorHub] Switch dimension → {dim} (game={game})");
    }

    static void PrewarmEditor(int dim, string game, string api)
    {
        PostGd(api, "/api/gd/sg-prewarm", $"{{\"game\":\"{game}\",\"dimension\":{dim}}}");
        if (Application.isPlaying)
        {
            var cache = UnityEngine.Object.FindAnyObjectByType<DimensionSwitchCache>();
            if (cache != null)
                cache.StartCoroutine(cache.PrewarmAsync(game, dim));
        }
        Debug.Log($"[FacilitatorHub] Prewarm SG for dim {dim}");
    }

    static void PostGd(string apiBase, string path, string json)
    {
        try
        {
            var url = (apiBase ?? "http://127.0.0.1:5050").TrimEnd('/') + path;
            using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            var raw = System.Text.Encoding.UTF8.GetBytes(json ?? "{}");
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(raw);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-User-ID", "admin");
            req.SetRequestHeader("X-Admin", "1");
            req.SendWebRequest();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FacilitatorHub] GD POST failed: {ex.Message}");
        }
    }

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

            if (GUILayout.Button("Setup Standard Assets (all applicable)", GUILayout.Height(28)))
            {
                if (fac != null)
                {
                    var report = WizardStandardAssetsFacade.SetupAllForFacilitator(fac);
                    EditorUtility.SetDirty(fac);
                    if (facilitatorSo != null)
                        facilitatorSo.Update();
                    EditorUtility.DisplayDialog("Setup Standard Assets", report.Summary, "OK");
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

            if (GUILayout.Button("Validate scene services"))
            {
                IReadOnlyList<string> missing = SystemDrawerSceneServices.GetUnresolvedRequiredKeys(
                    SystemDrawerServiceKeys.WeatherPhysicsManifold,
                    SystemDrawerServiceKeys.PlanetBody,
                    SystemDrawerServiceKeys.PlanetShellGrid,
                    SystemDrawerServiceKeys.HierarchicalPathingSolver,
                    SystemDrawerServiceKeys.SystemDrawerAnimator);
                if (missing.Count == 0)
                    Debug.Log("[SystemDrawerFacilitator] All canonical service keys registered.");
                else
                    Debug.LogWarning("[SystemDrawerFacilitator] Missing keys: " + string.Join(", ", missing));
            }
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
        var budget = Undo.AddComponent<FeatureBudgetRuntime>(root);
        var w = new GameObject("_Wizards");
        Undo.RegisterCreatedObjectUndo(w, "Create _Wizards");
        Undo.RecordObject(w.transform, "Parent _Wizards");
        w.transform.SetParent(root.transform, false);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(svc);
        EditorUtility.SetDirty(fac);
        if (budget.profile == null)
        {
            var profile = ScriptableObject.CreateInstance<FeatureBudgetProfile>();
            profile.EnsureDefaults();
            const string assetDir = "Assets/SystemDrawer/FeatureBudget";
            if (!AssetDatabase.IsValidFolder(assetDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/SystemDrawer"))
                    AssetDatabase.CreateFolder("Assets", "SystemDrawer");
                AssetDatabase.CreateFolder("Assets/SystemDrawer", "FeatureBudget");
            }
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{assetDir}/DefaultFeatureBudgetProfile.asset");
            AssetDatabase.CreateAsset(profile, assetPath);
            budget.profile = profile;
        }
        EditorUtility.SetDirty(budget);
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[SystemDrawerHub] Created SystemDrawer root with service, facilitator, and _Wizards child.");
    }
}
