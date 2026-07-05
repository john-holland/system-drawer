#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI attach dialog for property / lemma / localization clause bindings.</summary>
public static class ContinuuuumClauseAttachDialog
{
    public static bool IsOpen { get; private set; }

    static ClauseRefRecord _clauseRef;
    static string _scriptText = "";
    static string _activeTab = LocalizationBindingKinds.Property;
    static LocalizationPropertySpecRecord[] _specs = Array.Empty<LocalizationPropertySpecRecord>();
    static string _propertyKey = "";
    static string _propertyValue = "";
    static string _entryId = "";
    static string _lemmaKey = "entry-id";
    static string _langCode = "";
    static string _locValue = "";
    static Func<ClauseRefRecord, string, string, string, string, System.Threading.Tasks.Task> _onAttach;

    public static void Open(
        ClauseRefRecord clauseRef,
        string scriptText,
        LocalizationPropertySpecRecord[] specs,
        Func<ClauseRefRecord, string, string, string, string, System.Threading.Tasks.Task> onAttach)
    {
        _clauseRef = clauseRef;
        _scriptText = scriptText ?? "";
        _specs = specs ?? Array.Empty<LocalizationPropertySpecRecord>();
        _propertyKey = _specs.Length > 0 ? _specs[0].key : LocalizationPropertyKeys.NonIkAnimation;
        _propertyValue = "";
        _entryId = clauseRef?.entryId ?? "";
        _lemmaKey = "entry-id";
        _langCode = "";
        _locValue = "";
        _activeTab = LocalizationBindingKinds.Property;
        _onAttach = onAttach;
        IsOpen = true;
    }

    public static void Close() => IsOpen = false;

    public static void DrawModal()
    {
        if (!IsOpen) return;
        var win = EditorWindow.focusedWindow;
        float w = win != null ? win.position.width : 600f;
        float h = win != null ? win.position.height : 400f;
        var rect = new Rect(w / 2f - 260, h / 2f - 180, 520, 360);
        GUI.ModalWindow(0xC1A053, rect, DrawWindow, "Attach to clause");
    }

    static void DrawWindow(int id)
    {
        EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_clauseRef?.selectionText ?? "(none)", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField($"Range [{_clauseRef?.charStart ?? 0}, {_clauseRef?.charEnd ?? 0})", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(_activeTab == LocalizationBindingKinds.Property, "Property", EditorStyles.toolbarButton))
            _activeTab = LocalizationBindingKinds.Property;
        if (GUILayout.Toggle(_activeTab == LocalizationBindingKinds.Lemma, "Lemma", EditorStyles.toolbarButton))
            _activeTab = LocalizationBindingKinds.Lemma;
        if (GUILayout.Toggle(_activeTab == LocalizationBindingKinds.Localization, "Localization", EditorStyles.toolbarButton))
            _activeTab = LocalizationBindingKinds.Localization;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        if (_activeTab == LocalizationBindingKinds.Property)
        {
            var keys = _specs.Length > 0 ? _specs : Array.Empty<LocalizationPropertySpecRecord>();
            var labels = new string[Math.Max(keys.Length, 1)];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = keys.Length > i ? keys[i].key : LocalizationPropertyKeys.NonIkAnimation;
            int idx = Array.IndexOf(labels, _propertyKey);
            if (idx < 0) idx = 0;
            idx = EditorGUILayout.Popup("Property key", idx, labels);
            _propertyKey = labels[idx];
            _propertyValue = EditorGUILayout.TextField("Value", _propertyValue);
        }
        else if (_activeTab == LocalizationBindingKinds.Lemma)
        {
            _entryId = EditorGUILayout.TextField("Entry ID", _entryId);
            _lemmaKey = EditorGUILayout.TextField("Property key", _lemmaKey);
        }
        else
        {
            _langCode = EditorGUILayout.TextField("Language code", _langCode);
            _locValue = EditorGUILayout.TextField("Translation", _locValue);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Attach"))
        {
            string kind = _activeTab;
            string key = _propertyKey;
            string val = _propertyValue;
            if (kind == LocalizationBindingKinds.Lemma)
            {
                key = string.IsNullOrEmpty(_lemmaKey) ? "entry-id" : _lemmaKey;
                val = _entryId;
            }
            else if (kind == LocalizationBindingKinds.Localization)
            {
                key = "lang:" + (_langCode ?? "").Trim();
                val = _locValue;
            }
            _ = _onAttach?.Invoke(_clauseRef, kind, key, val, _scriptText);
            Close();
        }
        if (GUILayout.Button("Cancel"))
            Close();
        EditorGUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}

#endif
