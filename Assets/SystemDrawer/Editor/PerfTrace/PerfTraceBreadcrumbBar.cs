#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Reusable breadcrumb navigation for PerfTrace drill-down.</summary>
public static class PerfTraceBreadcrumbBar
{
    public static void Draw(
        IList<PerfTraceNode> focusStack,
        Action onRoot,
        Action onBack,
        Action<int> onJumpToIndex)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Root", EditorStyles.miniButton, GUILayout.Width(50)))
            onRoot?.Invoke();

        for (int i = 0; i < focusStack.Count; i++)
        {
            EditorGUILayout.LabelField("›", GUILayout.Width(12));
            var node = focusStack[i];
            int captured = i;
            if (GUILayout.Button(node.Label, EditorStyles.miniButton))
                onJumpToIndex?.Invoke(captured);
        }

        if (focusStack.Count > 0 && GUILayout.Button("Back", EditorStyles.miniButton, GUILayout.Width(50)))
            onBack?.Invoke();

        EditorGUILayout.EndHorizontal();
    }
}
#endif
