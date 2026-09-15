#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class TayloringTravelAgentWindow : EditorWindow
{
    TayloringTravelAgent _agent;
    Vector2 _scroll;
    PreviewRenderUtility _preview;
    Vector2 _orbit = new Vector2(35f, 220f);

    [MenuItem("Locomotion/Tayloring Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<TayloringTravelAgentWindow>("Tayloring TA");
        w.minSize = new Vector2(460, 640);
    }

    void OnDisable()
    {
        if (_preview != null)
        {
            _preview.Cleanup();
            _preview = null;
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (TayloringTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(TayloringTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a TayloringTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = TayloringTravelAgent.DefaultPipeline();
        _agent.store = (ClothingStoreRagdoll)EditorGUILayout.ObjectField(
            "Store", _agent.store, typeof(ClothingStoreRagdoll), true);
        _agent.bolt = (ClothBoltSpec)EditorGUILayout.ObjectField(
            "Bolt", _agent.bolt, typeof(ClothBoltSpec), false);
        _agent.thread = (ThreadSpoolDriver)EditorGUILayout.ObjectField(
            "Thread", _agent.thread, typeof(ThreadSpoolDriver), true);
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        var step = _agent.SelectedStep;
        if (step != null)
        {
            EditorGUILayout.LabelField("Step", step.kind.ToString());
            step.tension01 = EditorGUILayout.Slider("Tension", step.tension01, 0f, 1f);
            step.pin01 = EditorGUILayout.Slider("Pin", step.pin01, 0f, 1f);
            step.bunch01 = EditorGUILayout.Slider("Bunch", step.bunch01, 0f, 1f);
            step.join01 = EditorGUILayout.Slider("Join", step.join01, 0f, 1f);
            EditorGUILayout.LabelField("Fold cache", step.foldCacheId ?? "");
            EditorGUILayout.Toggle("Bake complete", step.bakeComplete);
        }
        if (GUILayout.Button("Bake selected fold cache"))
        {
            if (step != null && _agent.bolt != null)
                ClothFoldBake.ApplyToStep(step, _agent.bolt);
        }
        if (GUILayout.Button("Try cut bolt"))
            _agent.TryCutBolt();
        if (GUILayout.Button("Open Cloth Pattern Designer"))
            ClothPatternDesignerWindow.Open(_agent.bolt);
        if (GUILayout.Button("Bake Open/Close BT (tayloring order)"))
        {
            var parent = _agent.transform.Find("TayloringOpenClose")
                         ?? new GameObject("TayloringOpenClose").transform;
            parent.SetParent(_agent.transform, false);
            Locomotion.Open.TayloringOpenCloseBt.Bake(_agent, parent, _agent.transform);
        }

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            TayloringTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            _agent.ThreatHalo01());
        if (_agent.OverLimit())
            EditorGUILayout.LabelField("RED — over limit / jammed thread");
        EditorGUILayout.LabelField("Bake completeness", _agent.BakeCompleteness01().ToString("0.00"));
        DrawFoldPreview(step);
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }

    void DrawFoldPreview(TayloringStep step)
    {
        EditorGUILayout.LabelField("Fold preview", EditorStyles.boldLabel);
        if (step?.bakedMesh == null)
        {
            EditorGUILayout.HelpBox("Bake the selected fold cache to preview.", MessageType.Info);
            return;
        }
        if (_preview == null)
        {
            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = 28f;
            _preview.camera.nearClipPlane = 0.01f;
            _preview.camera.farClipPlane = 20f;
            _preview.camera.clearFlags = CameraClearFlags.SolidColor;
            _preview.camera.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
        }
        Rect rect = GUILayoutUtility.GetRect(256f, 180f, GUILayout.ExpandWidth(true));
        var e = Event.current;
        if (e.type == EventType.MouseDrag && e.button == 0 && rect.Contains(e.mousePosition))
        {
            _orbit.y += e.delta.x;
            _orbit.x = Mathf.Clamp(_orbit.x + e.delta.y, -80f, 80f);
            e.Use();
            Repaint();
        }
        if (e.type != EventType.Repaint) return;
        var mesh = step.bakedMesh;
        var b = mesh.bounds;
        float dist = Mathf.Max(0.08f, b.extents.magnitude * 2.8f);
        Quaternion rot = Quaternion.Euler(_orbit.x, _orbit.y, 0f);
        _preview.BeginPreview(rect, GUIStyle.none);
        _preview.camera.transform.position = b.center + rot * (Vector3.back * dist);
        _preview.camera.transform.LookAt(b.center);
        var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/Internal-Colored");
        var mat = shader != null ? new Material(shader) : null;
        if (mat != null)
        {
            _preview.DrawMesh(mesh, Matrix4x4.identity, mat, 0);
            DestroyImmediate(mat);
        }
        _preview.camera.Render();
        GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.ScaleToFit, false);
    }
}
#endif
