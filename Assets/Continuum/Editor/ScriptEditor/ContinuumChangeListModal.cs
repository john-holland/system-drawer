#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Change-list modal (Save / Submit for review) — IMGUI port of web ContinuumChangeListModal.</summary>
public static class ContinuumChangeListModal
{
    public static bool IsOpen { get; private set; }

    static string _changeListId;
    static LocalizationChangeListRecord _data;
    static LocalizationChangeListItemRecord[] _required = System.Array.Empty<LocalizationChangeListItemRecord>();
    static LocalizationChangeListItemRecord[] _warnings = System.Array.Empty<LocalizationChangeListItemRecord>();
    static System.Action<string, LocalizationChangeListRecord> _onSave;
    static System.Action<string> _onSubmit;
    static bool _warningsExpanded;

    public static void Open(
        string changeListId,
        LocalizationChangeListRecord data,
        LocalizationChangeListItemRecord[] required,
        LocalizationChangeListItemRecord[] warnings,
        System.Action<string, LocalizationChangeListRecord> onSave,
        System.Action<string> onSubmit)
    {
        _changeListId = changeListId;
        _data = data;
        _required = required ?? System.Array.Empty<LocalizationChangeListItemRecord>();
        _warnings = warnings ?? System.Array.Empty<LocalizationChangeListItemRecord>();
        _onSave = onSave;
        _onSubmit = onSubmit;
        _warningsExpanded = false;
        IsOpen = true;
    }

    public static void Close() => IsOpen = false;

    public static void DrawModal()
    {
        if (!IsOpen) return;

        var win = EditorWindow.focusedWindow;
        float w = win != null ? win.position.width : 600f;
        float h = win != null ? win.position.height : 400f;
        var rect = new Rect(w / 2f - 280, h / 2f - 200, 560, 400);
        GUI.ModalWindow(0xC0FFEE, rect, DrawWindow, "Change list");
    }

    static void DrawWindow(int id)
    {
        EditorGUILayout.LabelField($"Change list {_changeListId} (rev {_data?.revision ?? 0})", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Required", EditorStyles.miniBoldLabel);
        foreach (var item in _required)
        {
            if (item == null) continue;
            EditorGUILayout.BeginHorizontal();
            item.userAcknowledged = EditorGUILayout.Toggle(item.userAcknowledged, GUILayout.Width(20));
            EditorGUILayout.LabelField(item.description ?? item.itemType, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
        }
        if (_required.Length == 0)
            EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);

        _warningsExpanded = EditorGUILayout.Foldout(_warningsExpanded, $"Warnings ({_warnings.Length})");
        if (_warningsExpanded)
        {
            foreach (var w in _warnings)
                EditorGUILayout.LabelField(w?.description ?? "", EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            foreach (var item in _required)
            {
                if (item != null && !item.userAcknowledged)
                {
                    EditorUtility.DisplayDialog("Change list", "Acknowledge all required items before save.", "OK");
                    return;
                }
            }
            _onSave?.Invoke(_changeListId, _data);
            Close();
        }
        if (GUILayout.Button("Submit for review"))
        {
            foreach (var item in _required)
            {
                if (item != null && !item.userAcknowledged)
                {
                    EditorUtility.DisplayDialog("Change list", "Acknowledge all required items before submit.", "OK");
                    return;
                }
            }
            _onSubmit?.Invoke(_changeListId);
            Close();
        }
        if (GUILayout.Button("Cancel"))
            Close();
        EditorGUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}

#endif
