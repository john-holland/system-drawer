#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Editor for lemma-level localization property bags (syncs with Continuum API).</summary>
public sealed class VocabularyLemmaPropertyEditorWindow : EditorWindow
{
    const string PlaybackPolicyContextTypeName = "AnimationPlaybackPolicyContext";
    const string LocomotionRuntimeAssembly = "Locomotion.Runtime";

    LocalizationPropertySpecCatalog _catalog;
    string _entryId = "";
    ThesaurusEntryPropertyRecord[] _properties = Array.Empty<ThesaurusEntryPropertyRecord>();
    bool _nonIkAnimation;
    Vector2 _scroll;
    GameObject _pushTarget;

    [MenuItem("Window/Continuum/Lemma Properties")]
    public static void Open()
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
    }

    public static void OpenWithEntryId(string entryId)
    {
        var w = GetWindow<VocabularyLemmaPropertyEditorWindow>("Lemma Properties");
        w.minSize = new Vector2(420, 320);
        if (!string.IsNullOrEmpty(entryId))
        {
            w._entryId = entryId;
            _ = w.LoadPropertiesAsync();
        }
    }

    void OnEnable()
    {
        _catalog = Resources.Load<LocalizationPropertySpecCatalog>("LocalizationPropertySpecCatalog");
        if (_catalog == null)
            _catalog = LocalizationPropertySpecCatalog.CreateDefaultAsset();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Localization Property Specs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Lemma-level property bags sync with Continuum thesaurus_entry_properties.", MessageType.Info);

        _entryId = EditorGUILayout.TextField("Entry ID", _entryId);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load properties"))
            _ = LoadPropertiesAsync();
        if (GUILayout.Button("Save"))
            SaveProperties();
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_catalog?.specs != null)
        {
            foreach (var spec in _catalog.specs)
            {
                if (spec == null) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(spec.key, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Type", spec.valueType);
                EditorGUILayout.LabelField("Default", spec.defaultValue);
                EditorGUILayout.LabelField("Description", spec.description ?? "", EditorStyles.wordWrappedLabel);
                if (spec.key == LocalizationPropertyKeys.NonIkAnimation)
                    _nonIkAnimation = EditorGUILayout.Toggle("Value", _nonIkAnimation);
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        _pushTarget = (GameObject)EditorGUILayout.ObjectField("Push to Travel Agent", _pushTarget, typeof(GameObject), true);
        if (GUILayout.Button("Push to runtime cache") && _pushTarget != null)
            PushToRuntime();

        if (GUILayout.Button("Sync specs from API"))
            SyncSpecs();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Open in Lemma Library") && !string.IsNullOrEmpty(_entryId))
        {
            var baseUrl = ContinuumEditorSession.ApiBaseUrl.TrimEnd('/');
            Application.OpenURL($"{baseUrl}/lemma-library#entry/{Uri.EscapeDataString(_entryId)}");
        }
    }

    async Task LoadPropertiesAsync()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        _properties = await client.GetEntryPropertiesAsync(_entryId);
        foreach (var p in _properties)
        {
            if (p != null && p.propertyKey == LocalizationPropertyKeys.NonIkAnimation &&
                TryParseBool(p.propertyValue, out bool v))
                _nonIkAnimation = v;
        }
        Repaint();
    }

    async void SaveProperties()
    {
        if (string.IsNullOrEmpty(_entryId)) return;
        var client = ContinuumEditorLocalizationClient.Instance;
        await client.PutEntryPropertyAsync(_entryId, LocalizationPropertyKeys.NonIkAnimation, _nonIkAnimation ? "true" : "false");
        await LoadPropertiesAsync();
    }

    void PushToRuntime()
    {
        var ctxType = FindLocomotionRuntimeType(PlaybackPolicyContextTypeName);
        if (ctxType == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Locomotion.Runtime is not loaded.", "OK");
            return;
        }

        var ctx = _pushTarget.GetComponent(ctxType) ?? _pushTarget.GetComponentInChildren(ctxType);
        if (ctx == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "Add AnimationPlaybackPolicyContext to Travel Agent hierarchy.", "OK");
            return;
        }

        var method = ctxType.GetMethod("SetLemmaProperties", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            EditorUtility.DisplayDialog("Lemma Properties", "AnimationPlaybackPolicyContext.SetLemmaProperties not found.", "OK");
            return;
        }

        var list = _properties.ToList();
        if (!string.IsNullOrEmpty(_entryId))
        {
            list.RemoveAll(p => p != null && p.propertyKey == LocalizationPropertyKeys.NonIkAnimation && p.entryId == _entryId);
            list.Add(new ThesaurusEntryPropertyRecord
            {
                entryId = _entryId,
                propertyKey = LocalizationPropertyKeys.NonIkAnimation,
                propertyValue = _nonIkAnimation ? "true" : "false"
            });
        }

        method.Invoke(ctx, new object[] { list });
    }

    async void SyncSpecs()
    {
        var specs = await ContinuumEditorLocalizationClient.Instance.GetPropertySpecsAsync();
        if (specs != null && specs.Length > 0)
            Debug.Log($"[Lemma Properties] Loaded {specs.Length} spec(s) from API.");
    }

    static Type FindLocomotionRuntimeType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(assembly.GetName().Name, LocomotionRuntimeAssembly, StringComparison.Ordinal))
                continue;
            var type = assembly.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;
        }
        return null;
    }

    static bool TryParseBool(string value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (bool.TryParse(value.Trim(), out result))
            return true;
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "yes":
            case "on":
                result = true;
                return true;
            case "0":
            case "no":
            case "off":
                result = false;
                return true;
            default:
                return false;
        }
    }
}
#endif
