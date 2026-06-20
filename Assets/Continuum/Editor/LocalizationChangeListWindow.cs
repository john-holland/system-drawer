#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Change-list transaction UI — opens unified Script Editor side panel.</summary>
public sealed class LocalizationChangeListWindow : EditorWindow
{
    [MenuItem("Window/Continuum/Change Lists (Legacy)")]
    public static void Open()
    {
        ContinuumScriptEditorWindow.Open();
    }
}
#endif
