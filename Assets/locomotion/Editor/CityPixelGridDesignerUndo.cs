using System;
using UnityEditor;
using UnityEngine;

public static class CityPixelGridDesignerUndo
{
    public static void BindRepaint(EditorWindow window, Undo.UndoRedoCallback callback)
    {
        if (window == null || callback == null) return;
        Undo.undoRedoPerformed += callback;
    }

    public static void UnbindRepaint(Undo.UndoRedoCallback callback)
    {
        if (callback != null)
            Undo.undoRedoPerformed -= callback;
    }

    public static void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Undo", GUILayout.Width(64)))
            Undo.PerformUndo();
        if (GUILayout.Button("Redo", GUILayout.Width(64)))
            Undo.PerformRedo();
        GUILayout.Label("Ctrl/Cmd+Z · Ctrl+Y / Shift+Ctrl/Cmd+Z", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    public static T Draw<T>(UnityEngine.Object target, string undoName, T current, Func<T, T> drawer)
    {
        EditorGUI.BeginChangeCheck();
        T next = drawer(current);
        if (!EditorGUI.EndChangeCheck())
            return current;
        Record(target, undoName);
        return next;
    }

    public static void Record(UnityEngine.Object target, string undoName)
    {
        if (target == null) return;
        Undo.RecordObject(target, undoName);
        EditorUtility.SetDirty(target);
    }

    public static void RecordComplete(UnityEngine.Object target, string undoName)
    {
        if (target == null) return;
        Undo.RegisterCompleteObjectUndo(target, undoName);
        EditorUtility.SetDirty(target);
    }

    public static readonly Color SelectionTint = new Color(1f, 0.95f, 0.35f);

    public static Color OverlaySelection(Color cell, CityPixelGridCellSelection selection, int x, int y)
    {
        if (selection != null && selection.Contains(x, y))
            return Color.Lerp(cell, SelectionTint, 0.45f);
        return cell;
    }

    public static bool HandleSelectionHotkeys(CityPixelGridCellSelection selection)
    {
        Event e = Event.current;
        if (e == null || selection == null || e.type != EventType.KeyDown) return false;
        if (e.keyCode != KeyCode.Escape) return false;
        selection.Clear();
        e.Use();
        return true;
    }

    public static void DrawSelectionBar(
        CityPixelGridCellSelection selection,
        CityPixelGrid grid,
        int frameIndex,
        bool canPaintSelected,
        System.Action eraseSelected,
        System.Action paintSelected)
    {
        if (selection == null) return;
        int n = selection.Count;
        EditorGUILayout.BeginVertical("box");
        if (n == 0)
            EditorGUILayout.LabelField("Select brush: click or drag cells. Shift adds, Ctrl/Cmd toggles.");
        else
            EditorGUILayout.LabelField(n == 1 ? "1 cell selected" : n + " cells selected");

        if (n == 1)
        {
            foreach (var c in selection.Cells)
            {
                EditorGUILayout.LabelField($"Cell ({c.x}, {c.y})");
                if (grid != null)
                    DrawStampSummary(grid, frameIndex, c.x, c.y);
                break;
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = n > 0;
        if (GUILayout.Button("Clear Selection"))
            selection.Clear();
        if (GUILayout.Button("Erase Selected") && eraseSelected != null)
            eraseSelected();
        GUI.enabled = n > 0 && canPaintSelected;
        if (GUILayout.Button("Paint Selected") && paintSelected != null)
            paintSelected();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    static void DrawStampSummary(CityPixelGrid grid, int frameIndex, int x, int y)
    {
        if (grid.brushStamps == null) return;
        int shown = 0;
        for (int i = 0; i < grid.brushStamps.Count; i++)
        {
            var s = grid.brushStamps[i];
            if (s == null || s.frameIndex != frameIndex || s.cellX != x || s.cellY != y) continue;
            EditorGUILayout.LabelField($"  stamp floor {s.floorIndex}: {s.kind}");
            shown++;
            if (shown >= 6) break;
        }
        if (shown == 0)
            EditorGUILayout.LabelField("  (no stamps)");
    }
}

public sealed class CityPixelGridPaintStroke
{
    bool _active;
    int _group;
    UnityEngine.Object _target;

    public bool Active => _active;

    public void Begin(UnityEngine.Object target, string undoName)
    {
        if (_active || target == null) return;
        Undo.IncrementCurrentGroup();
        _group = Undo.GetCurrentGroup();
        Undo.RegisterCompleteObjectUndo(target, undoName);
        _target = target;
        _active = true;
    }

    public void End()
    {
        if (!_active) return;
        if (_target != null)
            EditorUtility.SetDirty(_target);
        Undo.CollapseUndoOperations(_group);
        _active = false;
        _target = null;
    }
}
