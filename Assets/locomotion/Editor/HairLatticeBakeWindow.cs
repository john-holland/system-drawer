using UnityEditor;
using UnityEngine;
using SdfMax;

/// <summary>
/// Editor window: bake lattice waterfall radial cache, fiber maps, hairline/part defaults, and gizmos.
/// </summary>
public sealed class HairLatticeBakeWindow : EditorWindow
{
    HairPlumeConfig _config;
    HairPlumePhysicsDriver _driver;
    Transform _scalpRoot;
    Color _rootColor = new Color(0.25f, 0.14f, 0.08f, 1f);
    Color _tipColor = new Color(0.45f, 0.28f, 0.14f, 1f);
    string _outputFolder = "Assets/locomotion/hair/Baked";

    // Defaults: all lattice bake features on
    bool _includeHairline = true;
    bool _includeAnglePate = true;
    bool _includeHairPart = true;
    bool _includeSdf = true;
    bool _includeFiber = true;
    bool _includePassthrough = true;
    bool _assignDriver = true;
    bool _ensurePartGizmo = true;

    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Hair Lattice Bake")]
    public static void Open()
    {
        GetWindow<HairLatticeBakeWindow>("Hair Lattice Bake");
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
        _config = (HairPlumeConfig)EditorGUILayout.ObjectField("Config", _config, typeof(HairPlumeConfig), false);
        _driver = (HairPlumePhysicsDriver)EditorGUILayout.ObjectField("Physics Driver", _driver, typeof(HairPlumePhysicsDriver), true);
        _scalpRoot = (Transform)EditorGUILayout.ObjectField("Scalp Root", _scalpRoot, typeof(Transform), true);
        _rootColor = EditorGUILayout.ColorField("Root Color", _rootColor);
        _tipColor = EditorGUILayout.ColorField("Tip Color", _tipColor);
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lattice bake defaults (all on)", EditorStyles.boldLabel);
        _includeHairline = EditorGUILayout.ToggleLeft("Hairline ring curves", _includeHairline);
        _includeAnglePate = EditorGUILayout.ToggleLeft("Hairline angle → CenterPatePoint", _includeAnglePate);
        _includeHairPart = EditorGUILayout.ToggleLeft("Hair part spline (bisect gaussian)", _includeHairPart);
        _includeSdf = EditorGUILayout.ToggleLeft("SDF Max composition", _includeSdf);
        _includeFiber = EditorGUILayout.ToggleLeft("Fiber diffuse / specular", _includeFiber);
        _includePassthrough = EditorGUILayout.ToggleLeft("Passthrough shape layer", _includePassthrough);
        _assignDriver = EditorGUILayout.ToggleLeft("Assign bake to physics driver", _assignDriver);
        _ensurePartGizmo = EditorGUILayout.ToggleLeft("Ensure green part ribbon gizmo", _ensurePartGizmo);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Config", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Default Config Asset"))
            CreateConfig();
        using (new EditorGUI.DisabledScope(_config == null))
        {
            if (GUILayout.Button("Apply Lattice Bake Defaults To Config"))
                ApplyDefaultsToConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(_config == null))
        {
            if (GUILayout.Button("Bake All (Radial + SDF + Fiber + Part)"))
                BakeAll();
            if (GUILayout.Button("Bake Radial Lattice Only"))
                BakeRadialOnly();
            if (GUILayout.Button("Bake SDF Max Only"))
                BakeSdfOnly();
            if (GUILayout.Button("Bake Fiber Maps Only"))
                BakeFiberOnly();
            if (GUILayout.Button("Bake Hairline + Part Into Radial"))
                BakeHairlinePartRadial();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(_driver == null && _scalpRoot == null))
        {
            if (GUILayout.Button("Add / Refresh Hair Line Part Gizmo"))
                EnsureGizmo();
        }
        using (new EditorGUI.DisabledScope(_driver == null))
        {
            if (GUILayout.Button("Auto Set Body Capsule Binder Overrides"))
                AutoSetBodyCapsuleOverrides();
        }

        EditorGUILayout.EndScrollView();
    }

    void CreateConfig()
    {
        EnsureFolder(_outputFolder);
        var cfg = CreateInstance<HairPlumeConfig>();
        cfg.ApplyLatticeBakeDefaults();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairPlumeConfig.asset");
        AssetDatabase.CreateAsset(cfg, path);
        AssetDatabase.SaveAssets();
        _config = cfg;
        EditorGUIUtility.PingObject(cfg);
    }

    void ApplyDefaultsToConfig()
    {
        if (_config == null) return;
        Undo.RecordObject(_config, "Apply Hair Lattice Bake Defaults");
        _config.ApplyLatticeBakeDefaults();
        if (!_includeHairPart && _config.hairPartSpline != null)
            _config.hairPartSpline.enabled = false;
        if (_includeHairPart)
        {
            _config.hairPartSpline ??= new HairPartSpline();
            _config.hairPartSpline.enabled = true;
            _config.hairPartSpline.EnsureDefaults();
        }
        if (!_includeAnglePate)
            _config.pateAngleBlend = 0f;
        else if (_config.pateAngleBlend <= 0f)
            _config.pateAngleBlend = 0.35f;
        EditorUtility.SetDirty(_config);
        AssetDatabase.SaveAssets();
    }

    void EnsureConfigDefaultsBeforeBake()
    {
        if (_config == null) return;
        Undo.RecordObject(_config, "Hair Lattice Bake Defaults");
        _config.ApplyLatticeBakeDefaults();
        if (_includeHairPart)
        {
            _config.hairPartSpline ??= new HairPartSpline();
            _config.hairPartSpline.enabled = true;
            _config.hairPartSpline.EnsureDefaults();
        }
        else if (_config.hairPartSpline != null)
        {
            _config.hairPartSpline.enabled = false;
        }

        if (!_includeAnglePate)
            _config.pateAngleBlend = 0f;

        if (!_includeHairline)
        {
            _config.hairLineCurve = HairLineCurve.Constant(1f);
            _config.hairLineAngleCurve = HairLineAngleCurve.Zero();
        }

        EditorUtility.SetDirty(_config);
    }

    void BakeAll()
    {
        if (!RequireConfig()) return;
        EnsureConfigDefaultsBeforeBake();
        EnsureFolder(_outputFolder);

        SdfMaxCompositionAsset composition = null;
        string sdfPath = null;
        if (_includeSdf)
        {
            composition = HairPlumeSdfComposer.ComposeGaussianPlume(_config);
            sdfPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairPlumeSdf.asset");
            AssetDatabase.CreateAsset(composition, sdfPath);
        }

        var bake = HairLatticeWaterfallBaker.Bake(_config, composition, _scalpRoot);
        string texPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairRadial.png");
        WritePng(texPath, bake.texture);
        var radialTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        string diffPath = null;
        string specPath = null;
        if (_includeFiber)
        {
            HairFiberMaterialBaker.Bake(_config, _rootColor, _tipColor, out var diffuse, out var specular);
            diffPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairDiffuse.png");
            specPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairSpecular.png");
            WritePng(diffPath, diffuse);
            WritePng(specPath, specular);
            DestroyImmediate(diffuse);
            DestroyImmediate(specular);
        }

        string passPath = null;
        if (_includePassthrough)
        {
            var passA = HairPassthroughShapeBaker.Bake(_config, new[]
            {
                new HairPassthroughShapeBaker.CurveDef
                {
                    azimuth01 = 0.15f,
                    lengthStart01 = 0.2f,
                    lengthEnd01 = 0.95f,
                    width01 = 0.08f,
                    height01 = 0.7f
                }
            }, "HairPassthroughA");
            passPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairPassthroughA.png");
            WritePng(passPath, passA);
            DestroyImmediate(passA);
        }

        if (_assignDriver && _driver != null)
            AssignToDriver(bake.pixels, radialTex, diffPath, specPath, passPath, composition);

        if (_ensurePartGizmo)
            EnsureGizmo();

        DestroyImmediate(bake.texture);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Hair Bake",
            $"Baked to {_outputFolder}" + (sdfPath != null ? $"\nSDF: {sdfPath}" : ""),
            "OK");
    }

    void BakeRadialOnly()
    {
        if (!RequireConfig()) return;
        EnsureConfigDefaultsBeforeBake();
        EnsureFolder(_outputFolder);
        var bake = HairLatticeWaterfallBaker.Bake(_config, null, _scalpRoot);
        string texPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairRadial.png");
        WritePng(texPath, bake.texture);
        if (_assignDriver && _driver != null)
        {
            Undo.RecordObject(_driver, "Assign hair radial");
            _driver.config = _config;
            _driver.LoadBake(bake.pixels);
            var mat = _driver.hairRenderer != null ? _driver.hairRenderer.sharedMaterial : null;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat != null && tex != null)
            {
                mat.SetTexture("_HairRadialTex", tex);
                EditorUtility.SetDirty(mat);
            }
            EditorUtility.SetDirty(_driver);
        }
        DestroyImmediate(bake.texture);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void BakeSdfOnly()
    {
        if (!RequireConfig()) return;
        EnsureConfigDefaultsBeforeBake();
        EnsureFolder(_outputFolder);
        var composition = HairPlumeSdfComposer.ComposeGaussianPlume(_config);
        string sdfPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairPlumeSdf.asset");
        AssetDatabase.CreateAsset(composition, sdfPath);
        AssetDatabase.SaveAssets();
        if (_assignDriver && _driver != null)
        {
            Undo.RecordObject(_driver, "Assign plume SDF composition");
            _driver.SetPlumeSdfComposition(composition);
            if (_config != null) _driver.config = _config;
            EditorUtility.SetDirty(_driver);
        }
        EditorGUIUtility.PingObject(composition);
    }

    void BakeFiberOnly()
    {
        if (!RequireConfig()) return;
        EnsureFolder(_outputFolder);
        HairFiberMaterialBaker.Bake(_config, _rootColor, _tipColor, out var diffuse, out var specular);
        string diffPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairDiffuse.png");
        string specPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairSpecular.png");
        WritePng(diffPath, diffuse);
        WritePng(specPath, specular);
        if (_assignDriver && _driver != null && _driver.hairRenderer != null)
        {
            var mat = _driver.hairRenderer.sharedMaterial;
            HairFiberMaterialBaker.Bind(mat,
                AssetDatabase.LoadAssetAtPath<Texture2D>(diffPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(specPath));
            EditorUtility.SetDirty(mat);
        }
        DestroyImmediate(diffuse);
        DestroyImmediate(specular);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void BakeHairlinePartRadial()
    {
        _includeHairline = true;
        _includeAnglePate = true;
        _includeHairPart = true;
        BakeRadialOnly();
        if (_ensurePartGizmo)
            EnsureGizmo();
    }

    void AssignToDriver(Color[] pixels, Texture2D radialTex, string diffPath, string specPath, string passPath,
        SdfMaxCompositionAsset composition = null)
    {
        Undo.RecordObject(_driver, "Assign hair bake");
        _driver.config = _config;
        if (_scalpRoot != null)
            _driver.scalpRoot = _scalpRoot;
        if (composition != null)
            _driver.SetPlumeSdfComposition(composition);
        _driver.LoadBake(pixels);
        var mat = _driver.hairRenderer != null ? _driver.hairRenderer.sharedMaterial : null;
        if (mat != null)
        {
            if (radialTex != null) mat.SetTexture("_HairRadialTex", radialTex);
            if (!string.IsNullOrEmpty(diffPath) && !string.IsNullOrEmpty(specPath))
            {
                HairFiberMaterialBaker.Bind(mat,
                    AssetDatabase.LoadAssetAtPath<Texture2D>(diffPath),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(specPath));
            }
            if (!string.IsNullOrEmpty(passPath))
            {
                HairPassthroughShapeBaker.BindLayers(mat,
                    AssetDatabase.LoadAssetAtPath<Texture2D>(passPath), null);
            }
            EditorUtility.SetDirty(mat);
        }
        EditorUtility.SetDirty(_driver);
    }

    void EnsureGizmo()
    {
        Transform host = _driver != null ? _driver.transform : _scalpRoot;
        if (host == null)
        {
            EditorUtility.DisplayDialog("Hair Bake", "Assign a Physics Driver or Scalp Root.", "OK");
            return;
        }

        var gizmo = host.GetComponent<HairLinePartGizmo>();
        if (gizmo == null)
            gizmo = Undo.AddComponent<HairLinePartGizmo>(host.gameObject);
        Undo.RecordObject(gizmo, "Hair Line Part Gizmo");
        gizmo.config = _config != null ? _config : (_driver != null ? _driver.config : null);
        gizmo.scalpRoot = _scalpRoot != null ? _scalpRoot : (_driver != null ? _driver.scalpRoot : host);
        gizmo.drawHairline = true;
        gizmo.drawPartRibbon = true;
        gizmo.drawCenterPate = true;
        gizmo.partRibbonColor = new Color(0.15f, 1f, 0.2f, 1f);
        EditorUtility.SetDirty(gizmo);
        if (_driver != null)
        {
            Undo.RecordObject(_driver, "Wire hair gizmo");
            if (_config != null) _driver.config = _config;
            if (_scalpRoot != null) _driver.scalpRoot = _scalpRoot;
            EditorUtility.SetDirty(_driver);
        }
        Selection.activeGameObject = host.gameObject;
    }

    void AutoSetBodyCapsuleOverrides()
    {
        if (_driver == null)
        {
            EditorUtility.DisplayDialog("Hair Bake", "Assign a Physics Driver.", "OK");
            return;
        }

        var binder = _driver.GetComponent<HairBodyCapsuleBinder>()
                     ?? _driver.GetComponentInParent<HairBodyCapsuleBinder>()
                     ?? _driver.GetComponentInChildren<HairBodyCapsuleBinder>();
        if (binder == null)
            binder = Undo.AddComponent<HairBodyCapsuleBinder>(_driver.gameObject);

        Undo.RecordObject(binder, "Auto Set Body Capsule Overrides");
        if (binder.ragdoll == null)
            binder.ragdoll = _driver.GetComponentInParent<RagdollSystem>();
        if (binder.config == null && _config != null)
            binder.config = _config;
        if (binder.scalpRoot == null)
            binder.scalpRoot = _scalpRoot != null ? _scalpRoot : _driver.scalpRoot;

        int n = binder.AutoSetOptionalOverrides();
        if (_driver.bodyBinder == null)
        {
            Undo.RecordObject(_driver, "Wire body capsule binder");
            _driver.bodyBinder = binder;
            EditorUtility.SetDirty(_driver);
        }
        EditorUtility.SetDirty(binder);
        EditorGUIUtility.PingObject(binder);
        EditorUtility.DisplayDialog("Hair Bake", $"Auto-set {n} optional bone override(s) on Body Capsule Binder.", "OK");
    }

    bool RequireConfig()
    {
        if (_config != null) return true;
        EditorUtility.DisplayDialog("Hair Bake", "Assign a HairPlumeConfig.", "OK");
        return false;
    }

    static void WritePng(string path, Texture2D tex)
    {
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Replace("\\", "/").Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
