using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class FeatureBudgetWindow : EditorWindow
{
    FeatureBudgetProfile _profile;
    FeatureBudgetRuntime _runtime;
    Vector2 _scroll;
    string _unlockReasonDraft = "";

    [MenuItem("Window/System Drawer/Diagnostics/Feature Budget")]
    public static void Open() => GetWindow<FeatureBudgetWindow>("Feature Budget");

    void OnEnable() => RefreshRuntime();

    void RefreshRuntime()
    {
        _runtime = FindFirstObjectByType<FeatureBudgetRuntime>(FindObjectsInactive.Include);
        if (_runtime != null && _runtime.profile != null)
            _profile = _runtime.profile;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("System Drawer Feature Budget", EditorStyles.boldLabel);
        DrawRuntimeSection();
        EditorGUILayout.Space(4);
        _profile = (FeatureBudgetProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(FeatureBudgetProfile), false);
        if (_profile == null)
        {
            if (GUILayout.Button("Create Default Profile Asset"))
                _profile = FeatureBudgetEditorUtility.CreateDefaultProfileAsset();
            return;
        }

        _profile.EnsureDefaults();
        DrawTopBar();
        EditorGUILayout.Space(6);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawFeatureList();
        EditorGUILayout.Space(8);
        DrawRatioPanel();
        EditorGUILayout.EndScrollView();

        if (GUI.changed && _profile != null)
            EditorUtility.SetDirty(_profile);
    }

    void DrawRuntimeSection()
    {
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _runtime = (FeatureBudgetRuntime)EditorGUILayout.ObjectField(
            "Feature Budget Runtime", _runtime, typeof(FeatureBudgetRuntime), true);
        if (EditorGUI.EndChangeCheck() && _runtime != null && _runtime.profile != null && _profile == null)
            _profile = _runtime.profile;

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(_runtime == null))
        {
            if (GUILayout.Button("Select", GUILayout.Width(64)))
                Selection.activeGameObject = _runtime.gameObject;
            if (GUILayout.Button("Ping", GUILayout.Width(52)))
                EditorGUIUtility.PingObject(_runtime);
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(64)))
            RefreshRuntime();
        EditorGUILayout.EndHorizontal();

        if (_runtime == null)
        {
            EditorGUILayout.HelpBox(
                "No FeatureBudgetRuntime in loaded scenes. Add one to the SystemDrawer hub root to sample CPU and drive granularity at play time.",
                MessageType.Info);
            if (GUILayout.Button("Create Feature Budget Runner on Hub", GUILayout.Height(26)))
            {
                _runtime = FeatureBudgetEditorUtility.CreateRuntimeRunner(_profile);
                if (_runtime != null && _runtime.profile != null && _profile == null)
                    _profile = _runtime.profile;
            }
        }
        else if (_profile != null && _runtime.profile != _profile)
        {
            EditorGUILayout.HelpBox("Runtime profile differs from the window profile.", MessageType.Warning);
            if (GUILayout.Button("Assign window profile to runtime"))
            {
                Undo.RecordObject(_runtime, "Assign feature budget profile");
                _runtime.profile = _profile;
                EditorUtility.SetDirty(_runtime);
            }
        }
    }

    void DrawTopBar()
    {
        float target = _profile.targetFrameCpuMs;
        float rolling = _runtime != null ? _runtime.RollingCpuMs : 0f;
        var state = _runtime != null ? _runtime.BudgetState : FeatureBudgetState.Normal;
        EditorGUILayout.LabelField($"State: {state}  |  Rolling CPU: {rolling:F2} ms / {target:F2} ms");

        EditorGUI.BeginChangeCheck();
        _profile.targetFrameCpuMs = EditorGUILayout.FloatField("Target CPU ms", _profile.targetFrameCpuMs);
        _profile.warnThreshold = EditorGUILayout.Slider("Warn threshold", _profile.warnThreshold, 0.5f, 1f);
        _profile.rollingWindowFrames = EditorGUILayout.IntField("Rolling window frames", _profile.rollingWindowFrames);
        if (EditorGUI.EndChangeCheck() && _runtime != null)
            EditorUtility.SetDirty(_runtime);
    }

    void DrawFeatureList()
    {
        EditorGUILayout.LabelField("Features (importance order)", EditorStyles.boldLabel);
        if (_profile.entries == null)
            return;

        for (int i = 0; i < _profile.entries.Count; i++)
        {
            var entry = _profile.entries[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▲", GUILayout.Width(22)) && i > 0)
            {
                SwapEntries(i, i - 1);
                ReindexRanks();
            }
            if (GUILayout.Button("▼", GUILayout.Width(22)) && i < _profile.entries.Count - 1)
            {
                SwapEntries(i, i + 1);
                ReindexRanks();
            }
            EditorGUILayout.LabelField($"{entry.importanceRank}. {entry.displayName}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            entry.controlMode = (FeatureBudgetControlMode)EditorGUILayout.EnumPopup("Mode", entry.controlMode);
            if (entry.controlMode == FeatureBudgetControlMode.Manual)
                entry.manualEnabled = EditorGUILayout.Toggle("Manual enabled", entry.manualEnabled);

            if (entry.supportsAestheticGranularity)
            {
                float g = Application.isPlaying ? entry.granularityLevel : 1f;
                if (_runtime != null && Application.isPlaying)
                    g = _runtime.GetGranularity(entry.featureId);
                EditorGUILayout.LabelField($"Granularity: {g * 100f:F0}%");
            }

            float lastMs = entry.lastFrameMs;
            float avgMs = entry.rollingAvgMs;
            if (Application.isPlaying)
                EditorGUILayout.LabelField($"Frame: {lastMs:F2} ms  |  Avg: {avgMs:F2} ms");
            EditorGUILayout.EndVertical();
        }
    }

    void DrawRatioPanel()
    {
        EditorGUILayout.LabelField("Ratio lock (Composition UI fields)", EditorStyles.boldLabel);
        if (GUILayout.Button("Open Planetary Composition UI"))
            EditorApplication.ExecuteMenuItem("Window/System Drawer/Planet/Composition UI");

        if (_profile.ratioBindings == null)
            return;

        for (int i = 0; i < _profile.ratioBindings.Count; i++)
        {
            var b = _profile.ratioBindings[i];
            if (!b.budgetGoverned)
                continue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(b.fieldId, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Source feature: {b.sourceFeatureId}");
            EditorGUILayout.LabelField($"Ratio: {b.ratio:F6}  Locked: {b.ratioLocked}");

            float anchor = _runtime != null && _runtime.RatioRegistry != null
                ? _runtime.RatioRegistry.AnchorRadius
                : 500f;
            float effective = b.EffectiveValue(anchor);
            EditorGUILayout.LabelField($"Effective value @ R={anchor:F0}: {effective:F3}");

            if (b.ratioLocked)
            {
                EditorGUILayout.LabelField("Unlock reason (required to unlock):");
                _unlockReasonDraft = EditorGUILayout.TextField(_unlockReasonDraft);
                if (GUILayout.Button("Unlock ratio"))
                {
                    if (!FeatureBudgetRatioBinding.IsValidUnlockReason(_unlockReasonDraft))
                        EditorUtility.DisplayDialog("Unlock blocked", "Provide a non-empty unlock reason.", "OK");
                    else
                    {
                        b.unlockReason = _unlockReasonDraft.Trim();
                        b.ratioLocked = false;
                        _unlockReasonDraft = "";
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Unlocked: {b.unlockReason}");
                b.manualOverride = EditorGUILayout.FloatField("Manual override", b.manualOverride);
                if (GUILayout.Button("Relock ratio"))
                {
                    b.ratioLocked = true;
                    b.unlockReason = "";
                    if (anchor > 1e-6f)
                        b.ratio = b.manualOverride / anchor;
                }
            }
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Sync From Active PlanetBody"))
            FeatureBudgetEditorUtility.SyncProfileFromScenePlanet(_profile);
    }

    void SwapEntries(int a, int b)
    {
        var tmp = _profile.entries[a];
        _profile.entries[a] = _profile.entries[b];
        _profile.entries[b] = tmp;
    }

    void ReindexRanks()
    {
        for (int i = 0; i < _profile.entries.Count; i++)
            _profile.entries[i].importanceRank = i;
    }
}

static class FeatureBudgetEditorUtility
{
    public static FeatureBudgetProfile CreateDefaultProfileAsset()
    {
        var profile = ScriptableObject.CreateInstance<FeatureBudgetProfile>();
        profile.EnsureDefaults();
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Feature Budget Profile", "FeatureBudgetProfile", "asset", "Choose location");
        if (string.IsNullOrEmpty(path))
            return profile;
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        return profile;
    }

    public static void SyncProfileFromScenePlanet(FeatureBudgetProfile profile)
    {
        var body = Object.FindFirstObjectByType<Planetary.PlanetBody>();
        if (body == null || profile == null)
            return;
        if (body.ratioModel == null)
            body.ratioModel = Planetary.Composition.PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
        Planetary.Composition.PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
            body.ratioModel, body, body.compositionProfile, null, body.horizonLodSettings,
            body.sdfLodProfile ?? (body.sdfLodRenderer != null ? body.sdfLodRenderer.profile : null));
        EditorUtility.SetDirty(body);

        var registry = new FeatureBudgetRatioRegistry();
        registry.LoadFromProfile(profile);
        registry.SyncFromPlanetSource(new Planetary.PlanetRatioSource(body), profile);
        registry.WriteBackToProfile(profile);
        EditorUtility.SetDirty(profile);
    }

    public static void EnsureRuntimeOnHub(GameObject hubRoot, FeatureBudgetProfile profile)
    {
        if (hubRoot == null)
            return;
        var runtime = hubRoot.GetComponent<FeatureBudgetRuntime>();
        if (runtime == null)
            runtime = Undo.AddComponent<FeatureBudgetRuntime>(hubRoot);
        if (profile != null)
            runtime.profile = profile;
        EditorUtility.SetDirty(runtime);
    }

    public static FeatureBudgetRuntime CreateRuntimeRunner(FeatureBudgetProfile profile)
    {
        var hubGo = ResolveHubRoot();
        if (hubGo == null)
        {
            if (!EditorUtility.DisplayDialog(
                    "No System Drawer Hub",
                    "No SystemDrawer facilitator or service in loaded scenes. Create a hub now?",
                    "Create Hub",
                    "Cancel"))
                return null;

            SystemDrawerHubSetup.CreateHierarchyHub();
            hubGo = ResolveHubRoot();
            if (hubGo == null)
            {
                EditorUtility.DisplayDialog("Feature Budget", "Hub creation failed or facilitator not found.", "OK");
                return null;
            }
        }

        EnsureRuntimeOnHub(hubGo, profile);
        var runtime = hubGo.GetComponent<FeatureBudgetRuntime>();
        if (runtime != null && runtime.profile == null && profile == null)
        {
            var defaultProfile = AssetDatabase.LoadAssetAtPath<FeatureBudgetProfile>(
                WizardStandardAssetsPaths.FeatureBudget.DefaultProfile);
            if (defaultProfile != null)
            {
                Undo.RecordObject(runtime, "Assign default feature budget profile");
                runtime.profile = defaultProfile;
                EditorUtility.SetDirty(runtime);
            }
        }

        if (hubGo.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(hubGo.scene);
        Selection.activeGameObject = hubGo;
        EditorGUIUtility.PingObject(runtime);
        return runtime;
    }

    static GameObject ResolveHubRoot()
    {
        var fac = Object.FindAnyObjectByType<SystemDrawerFacilitator>(FindObjectsInactive.Include);
        if (fac != null)
            return fac.gameObject;
        var svc = Object.FindAnyObjectByType<SystemDrawerService>(FindObjectsInactive.Include);
        return svc != null ? svc.gameObject : null;
    }
}
