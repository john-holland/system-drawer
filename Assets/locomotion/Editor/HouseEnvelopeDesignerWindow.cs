using UnityEditor;
using UnityEngine;

public sealed class HouseEnvelopeDesignerWindow : EditorWindow
{
    HouseConstructionPlan _plan;
    int _side;
    string _floorText = "first";
    int _paintX;
    int _paintY;
    float[,] _height;
    const int Size = 16;

    [MenuItem("Locomotion/House Envelope Designer")]
    public static void Open()
    {
        var w = GetWindow<HouseEnvelopeDesignerWindow>("House Envelope");
        w.minSize = new Vector2(420, 420);
    }

    void OnGUI()
    {
        _plan = (HouseConstructionPlan)EditorGUILayout.ObjectField("Plan", _plan, typeof(HouseConstructionPlan), false);
        _floorText = EditorGUILayout.TextField("Floor", _floorText);
        _side = EditorGUILayout.Popup("House Side", _side, new[] { "Front", "Right", "Back", "Left" });
        if (_height == null)
            _height = new float[Size, Size];

        var rect = GUILayoutUtility.GetRect(Size * 14f, Size * 14f);
        if (Event.current.type == EventType.Repaint)
        {
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float h = _height[y, x];
                EditorGUI.DrawRect(
                    new Rect(rect.x + x * 14f, rect.y + y * 14f, 13f, 13f),
                    new Color(h, h, h, 1f));
            }
        }
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            _paintX = Mathf.Clamp(Mathf.FloorToInt((Event.current.mousePosition.x - rect.x) / 14f), 0, Size - 1);
            _paintY = Mathf.Clamp(Mathf.FloorToInt((Event.current.mousePosition.y - rect.y) / 14f), 0, Size - 1);
            _height[_paintY, _paintX] = 1f;
            Event.current.Use();
        }

        if (GUILayout.Button("Bake Height → Displaced Torus SDF") && _plan != null)
        {
            HouseFloorIndex.TryParse(_floorText, out int floor);
            _plan.envelopeLayers.Add(new HouseEnvelopeDisplacementLayer
            {
                layerId = "height",
                side = (HouseEnvelopeSide)_side,
                floorIndex = floor,
                height = _height
            });
            _plan.BakeSoftToHard();
            EditorUtility.SetDirty(_plan);
        }
    }
}
