#if UNITY_EDITOR
using SdfMax;
using UnityEditor;
using UnityEngine;

/// <summary>In-window PreviewRenderUtility of a baked <see cref="SdfMaxCompositionAsset"/> surface.</summary>
public sealed class SdfMaxCompositionPreviewDrawer
{
    PreviewRenderUtility _preview;
    Mesh _mesh;
    Material _mat;
    SdfMaxCompositionAsset _bakedFor;
    Vector2 _orbit = new Vector2(35f, 220f);
    string _status = "Bake SDF to preview.";

    public void Invalidate()
    {
        _bakedFor = null;
        if (_mesh != null)
        {
            Object.DestroyImmediate(_mesh);
            _mesh = null;
        }
    }

    public void Dispose()
    {
        Invalidate();
        if (_mat != null)
        {
            Object.DestroyImmediate(_mat);
            _mat = null;
        }
        if (_preview != null)
        {
            _preview.Cleanup();
            _preview = null;
        }
    }

    public void Draw(SdfMaxCompositionAsset composition, float height = 220f)
    {
        EditorGUILayout.LabelField("SDF Max preview", EditorStyles.boldLabel);
        if (composition == null)
        {
            EditorGUILayout.HelpBox("No baked link SDF yet.", MessageType.Info);
            return;
        }

        if (_bakedFor != composition)
            Rebuild(composition);

        EditorGUILayout.LabelField(_status);
        Rect rect = GUILayoutUtility.GetRect(256f, height, GUILayout.ExpandWidth(true));
        HandleOrbit(rect);
        if (_preview == null || _mesh == null || Event.current.type != EventType.Repaint)
            return;

        _preview.BeginPreview(rect, GUIStyle.none);
        var b = _mesh.bounds;
        float dist = Mathf.Max(0.08f, b.extents.magnitude * 2.8f);
        Quaternion rot = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
        _preview.camera.transform.position = b.center + rot * (Vector3.back * dist);
        _preview.camera.transform.LookAt(b.center);
        if (_preview.lights != null && _preview.lights.Length > 0)
        {
            _preview.lights[0].intensity = 1.2f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }
        if (_mat == null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
                _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (_mat != null)
            _preview.DrawMesh(_mesh, Matrix4x4.identity, _mat, 0);
        _preview.camera.Render();
        GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.ScaleToFit, false);
    }

    void HandleOrbit(Rect rect)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDrag && e.button == 0 && rect.Contains(e.mousePosition))
        {
            _orbit.y += e.delta.x;
            _orbit.x = Mathf.Clamp(_orbit.x + e.delta.y, -80f, 80f);
            e.Use();
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }
    }

    void Rebuild(SdfMaxCompositionAsset composition)
    {
        EnsurePreview();
        Invalidate();
        _bakedFor = composition;
        if (composition.nodes == null || composition.nodes.Count == 0)
        {
            _status = "Composition has no nodes.";
            return;
        }

        var graph = new SdfMaxExpressionGraph(composition, null, Matrix4x4.identity);
        var eval = new SdfMaxEvaluator(graph);
        Bounds bounds = eval.WorldBounds;
        if (bounds.size.sqrMagnitude < 1e-8f)
            bounds = new Bounds(Vector3.zero, Vector3.one * 0.08f);
        bounds.Expand(0.01f);
        int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(null, composition);
        var data = SdfMaxSurfaceMesher.Build(eval, bounds, Matrix4x4.identity, 0f, 32, ver, true);
        if (!data.IsValid)
        {
            _status = "Surface mesher produced no triangles.";
            return;
        }

        _mesh = new Mesh { name = "ChainLinkSdfPreview", hideFlags = HideFlags.HideAndDontSave };
        data.ApplyToMesh(_mesh, true);
        _status = $"{_mesh.vertexCount} verts · {_mesh.triangles.Length / 3} tris · {composition.name}";
    }

    void EnsurePreview()
    {
        if (_preview != null)
            return;
        _preview = new PreviewRenderUtility();
        _preview.cameraFieldOfView = 28f;
        _preview.camera.nearClipPlane = 0.01f;
        _preview.camera.farClipPlane = 20f;
        _preview.camera.clearFlags = CameraClearFlags.SolidColor;
        _preview.camera.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
    }
}
#endif
