#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class CombatCardEditorWindow : EditorWindow
{
    CombatCard _card = new CombatCard();
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Cards/Combat", false, 240)]
    public static void ShowWindow() => GetWindow<CombatCardEditorWindow>("Combat Cards");

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Combat Card", EditorStyles.boldLabel);
        _card.sectionName = EditorGUILayout.TextField("Section Name", _card.sectionName);
        _card.combatMode = (CombatMode)EditorGUILayout.EnumPopup("Mode", _card.combatMode);
        _card.combatMoveKind = (CombatMoveKind)EditorGUILayout.EnumPopup("Move", _card.combatMoveKind);
        _card.primaryTarget = (GameObject)EditorGUILayout.ObjectField("Primary Target", _card.primaryTarget, typeof(GameObject), true);
        _card.attackBehaviorKey = EditorGUILayout.TextField("Attack BT Key", _card.attackBehaviorKey);
        _card.defenseBehaviorKey = EditorGUILayout.TextField("Defense BT Key", _card.defenseBehaviorKey);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Impact", EditorStyles.boldLabel);
        if (_card.impact == null) _card.impact = new CombatImpactSpec();
        _card.impact.damageType = (CombatDamageType)EditorGUILayout.EnumPopup("Damage Type", _card.impact.damageType);
        _card.impact.damage01 = EditorGUILayout.Slider("Damage", _card.impact.damage01, 0f, 1f);
        _card.impact.primaryLimbBone = EditorGUILayout.TextField("Limb", _card.impact.primaryLimbBone);
        _card.impact.throughOrStop = EditorGUILayout.Toggle("Through", _card.impact.throughOrStop);
        _card.impact.healthMode = (DamageHealthMode)EditorGUILayout.EnumPopup("Health Mode", _card.impact.healthMode);
        EditorGUILayout.Space(6);
        DrawProxy(_card.instrumentProxy ??= new CardInstrumentProxyOptions());
        EditorGUILayout.Space(6);
        if (GUILayout.Button("Reset to Generate Defaults"))
            _card = CombatCard.Generate(_card.combatMode, _card.combatMoveKind, _card.primaryTarget);
        EditorGUILayout.EndScrollView();
    }

    public static void DrawProxy(CardInstrumentProxyOptions p)
    {
        EditorGUILayout.LabelField("Proxy Instrument", EditorStyles.boldLabel);
        p.useProxyInstrument = EditorGUILayout.Toggle("Use Proxy", p.useProxyInstrument);
        p.sourceMap = (VehicleInstrumentMap)EditorGUILayout.ObjectField("Source Map", p.sourceMap, typeof(VehicleInstrumentMap), false);
        p.localSurfaceId = EditorGUILayout.TextField("Local Surface Id", p.localSurfaceId);
        p.safetyLockForceN = EditorGUILayout.FloatField("Safety Lock Force (N)", p.safetyLockForceN);
        p.appliedForce01 = EditorGUILayout.Slider("Applied Force 01", p.appliedForce01, 0f, 1f);
        p.hardwareFlavorNote = EditorGUILayout.TextField("Hardware Note", p.hardwareFlavorNote);
        EditorGUILayout.HelpBox(
            p.SafetyLockSatisfied ? "Safety lock satisfied." : "Safety lock NOT satisfied — fire gated.",
            p.SafetyLockSatisfied ? MessageType.Info : MessageType.Warning);
    }
}

public sealed class LoveCardEditorWindow : EditorWindow
{
    LoveCard _card = new LoveCard();
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Cards/Love", false, 241)]
    public static void ShowWindow() => GetWindow<LoveCardEditorWindow>("Love Cards");

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Love Card", EditorStyles.boldLabel);
        _card.loveMode = (LoveMakingMode)EditorGUILayout.EnumPopup("Mode", _card.loveMode);
        _card.loveMoveKind = (LoveMakingMoveKind)EditorGUILayout.EnumPopup("Move", _card.loveMoveKind);
        _card.physicality01 = EditorGUILayout.Slider("Physicality", _card.physicality01, 0f, 1f);
        _card.requiresConsent = EditorGUILayout.Toggle("Requires Consent", _card.requiresConsent);
        CombatCardEditorWindow.DrawProxy(_card.instrumentProxy ??= new CardInstrumentProxyOptions());
        if (GUILayout.Button("Reset Defaults"))
            _card = LoveCard.Generate(_card.loveMode, _card.loveMoveKind, null, null);
        EditorGUILayout.EndScrollView();
    }
}

public sealed class WrestlingCardEditorWindow : EditorWindow
{
    WrestlingCard _card = WrestlingCard.Generate(WrestlingMode.Play, WrestlingMoveKind.LockGrapple, null, null);
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Cards/Wrestling", false, 242)]
    public static void ShowWindow() => GetWindow<WrestlingCardEditorWindow>("Wrestling Cards");

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Wrestling Card", EditorStyles.boldLabel);
        _card.mode = (WrestlingMode)EditorGUILayout.EnumPopup("Mode", _card.mode);
        _card.moveKind = (WrestlingMoveKind)EditorGUILayout.EnumPopup("Move", _card.moveKind);
        _card.professionalStyle = EditorGUILayout.Toggle("Professional", _card.professionalStyle);
        CombatCardEditorWindow.DrawProxy(_card.instrumentProxy ??= new CardInstrumentProxyOptions());
        if (GUILayout.Button("Reset Defaults"))
            _card = WrestlingCard.Generate(_card.mode, _card.moveKind, null, null);
        EditorGUILayout.EndScrollView();
    }
}

public sealed class DamageTypeEditorWindow : EditorWindow
{
    CombatDamageProfileAsset _asset;
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Combat/Damage Types", false, 250)]
    public static void ShowWindow() => GetWindow<DamageTypeEditorWindow>("Damage Types");

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _asset = (CombatDamageProfileAsset)EditorGUILayout.ObjectField("Profile", _asset, typeof(CombatDamageProfileAsset), false);
        if (_asset == null && GUILayout.Button("Create New Profile Asset"))
        {
            var path = EditorUtility.SaveFilePanelInProject("Combat Damage Profile", "CombatDamageProfile", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                _asset = CreateInstance<CombatDamageProfileAsset>();
                AssetDatabase.CreateAsset(_asset, path);
                AssetDatabase.SaveAssets();
            }
        }
        if (_asset != null)
        {
            Undo.RecordObject(_asset, "Edit Damage Profile");
            _asset.damageType = (CombatDamageType)EditorGUILayout.EnumPopup("Type", _asset.damageType);
            _asset.healthMode = (DamageHealthMode)EditorGUILayout.EnumPopup("Health Mode", _asset.healthMode);
            _asset.materialKind = (CombatMaterialKind)EditorGUILayout.EnumPopup("Material", _asset.materialKind);
            _asset.defaultAmount01 = EditorGUILayout.Slider("Amount", _asset.defaultAmount01, 0f, 1f);
            _asset.defaultDepth01 = EditorGUILayout.Slider("Depth / Close preview", _asset.defaultDepth01, 0f, 1f);
            _asset.throughOrStop = EditorGUILayout.Toggle("Through", _asset.throughOrStop);
            _asset.cutInterval = EditorGUILayout.FloatField("Cut Interval", _asset.cutInterval);
            _asset.cutterProfileId = EditorGUILayout.TextField("Cutter Profile", _asset.cutterProfileId);
            _asset.cutProfileId = EditorGUILayout.TextField("Cut Profile", _asset.cutProfileId);
            _asset.smellSignature = EditorGUILayout.TextField("Smell", _asset.smellSignature);
            _asset.autoSuture = EditorGUILayout.Toggle("Auto Suture", _asset.autoSuture);
            _asset.notes = EditorGUILayout.TextArea(_asset.notes, GUILayout.MinHeight(48));
            EditorGUILayout.HelpBox($"closeAmount preview ≈ {_asset.defaultDepth01:0.00}  |  rip risk poles at 0 and 1", MessageType.None);
            if (GUI.changed) EditorUtility.SetDirty(_asset);
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
