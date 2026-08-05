using UnityEditor;
using UnityEngine;

public sealed class PixelLightTimedDesignerWindow : EditorWindow
{
    PixelLightPatternAsset _pattern;
    PixelLightColorPackage _colors;
    int _paintLayer;
    int _paintFrame;
    bool _paintOn = true;
    Vector2 _scroll;

    [MenuItem("Locomotion/Pixel Light Timed Designer")]
    public static void Open()
    {
        var w = GetWindow<PixelLightTimedDesignerWindow>("Pixel Light Designer");
        w.minSize = new Vector2(420, 360);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Pixel Light Timed Designer", EditorStyles.boldLabel);
        _pattern = (PixelLightPatternAsset)EditorGUILayout.ObjectField("Pattern", _pattern, typeof(PixelLightPatternAsset), false);
        _colors = (PixelLightColorPackage)EditorGUILayout.ObjectField("Colors", _colors, typeof(PixelLightColorPackage), false);

        if (GUILayout.Button("Create Default Prefab"))
        {
            var prefab = PixelLightPrefabFactory.CreateDefaultPrefabAsset();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"Created {PixelLightPrefabFactory.DefaultPrefabPath}");
        }

        if (GUILayout.Button("New Chase Pattern Asset"))
        {
            var p = PixelLightPatternAsset.CreateChasePreset();
            var path = EditorUtility.SaveFilePanelInProject("Save Pattern", "PixelLightChase", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(p, path);
                _pattern = p;
            }
        }

        if (_pattern == null)
        {
            EditorGUILayout.HelpBox("Assign or create a PixelLightPatternAsset.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _pattern.gridWidth = EditorGUILayout.IntSlider("Grid Width", _pattern.gridWidth, 2, 32);
        _pattern.gridHeight = EditorGUILayout.IntSlider("Grid Height", _pattern.gridHeight, 1, 16);
        _pattern.stepMs = EditorGUILayout.FloatField("Step Ms", _pattern.stepMs);
        _pattern.composite = (PixelLightLayerComposite)EditorGUILayout.EnumPopup("Composite", _pattern.composite);

        if (_pattern.layers.Count == 0)
            _pattern.layers.Add(new PixelLightLayer());

        _paintLayer = Mathf.Clamp(_paintLayer, 0, _pattern.layers.Count - 1);
        var layer = _pattern.layers[_paintLayer];
        EditorGUILayout.LabelField($"Layer {_paintLayer}: {layer.layerId}");
        if (layer.frames.Count == 0)
            layer.frames.Add(new PixelLightFrame());
        _paintFrame = Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Frame")) _paintFrame = Mathf.Max(0, _paintFrame - 1);
        if (GUILayout.Button("Next Frame"))
        {
            if (_paintFrame >= layer.frames.Count - 1)
                layer.frames.Add(new PixelLightFrame());
            _paintFrame++;
        }
        EditorGUILayout.EndHorizontal();

        _paintOn = EditorGUILayout.Toggle("Paint On", _paintOn);
        EnsureFrameSize(layer.frames[_paintFrame], _pattern.gridWidth, _pattern.gridHeight);
        DrawGrid(layer.frames[_paintFrame]);

        if (GUILayout.Button("Mark Dirty"))
            EditorUtility.SetDirty(_pattern);

        EditorGUILayout.EndScrollView();
    }

    void DrawGrid(PixelLightFrame frame)
    {
        float cell = 18f;
        var rect = GUILayoutUtility.GetRect(_pattern.gridWidth * cell, _pattern.gridHeight * cell);
        for (int y = 0; y < _pattern.gridHeight; y++)
        {
            string row = frame.rows[y];
            for (int x = 0; x < _pattern.gridWidth; x++)
            {
                var r = new Rect(rect.x + x * cell, rect.y + y * cell, cell - 1, cell - 1);
                bool on = x < row.Length && row[x] != ' ' && row[x] != '.' && row[x] != '_';
                EditorGUI.DrawRect(r, on ? Color.red : Color.gray * 0.4f);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    SetCell(frame, x, y, _paintOn);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    static void EnsureFrameSize(PixelLightFrame frame, int w, int h)
    {
        while (frame.rows.Count < h)
            frame.rows.Add(new string(' ', w));
        for (int y = 0; y < h; y++)
        {
            var row = frame.rows[y] ?? "";
            if (row.Length < w) row = row.PadRight(w, ' ');
            if (row.Length > w) row = row.Substring(0, w);
            frame.rows[y] = row;
        }
    }

    static void SetCell(PixelLightFrame frame, int x, int y, bool on)
    {
        var chars = frame.rows[y].ToCharArray();
        chars[x] = on ? '#' : ' ';
        frame.rows[y] = new string(chars);
    }
}
