using UnityEditor;
using UnityEngine;

/// <summary>Power diamond: one vertex per warden, handle = that warden's limit.</summary>
public sealed class PrisonWardenPowerDiamondWindow : EditorWindow
{
    PrisonWarden _warden;
    JusticeRehabilitationTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Prison Warden Power Diamond")]
    public static void Open()
    {
        var w = GetWindow<PrisonWardenPowerDiamondWindow>("Prison Warden Diamond");
        w.minSize = new Vector2(420, 360);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _warden = (PrisonWarden)EditorGUILayout.ObjectField("Warden", _warden, typeof(PrisonWarden), true);
        _agent = (JusticeRehabilitationTravelAgent)EditorGUILayout.ObjectField(
            "Justice Travel Agent", _agent, typeof(JusticeRehabilitationTravelAgent), true);

        if (_warden == null)
        {
            EditorGUILayout.HelpBox("Assign a PrisonWarden.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        var wardens = _warden.powerDiamondWardens;
        if (wardens == null || wardens.Count == 0)
            wardens = new System.Collections.Generic.List<PrisonWardenLimits> { _warden.limits };

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        HairdoPowerDiamondDrawer.Draw(
            diamond,
            _warden.limits != null ? _warden.limits.dialog01 : 0.5f,
            _warden.limits != null ? _warden.limits.physical01 : 0.5f,
            _warden.limits != null ? _warden.limits.outing01 : 0.5f,
            _warden.limits != null ? _warden.limits.parole01 : 0.5f,
            true);

        if (_agent != null && _agent.steps != null)
        {
            _agent.selectedStepIndex = EditorGUILayout.IntSlider("Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
            var step = _agent.SelectedStep;
            if (step != null)
            {
                bool over = _warden.OverUpperLimit(step.axis, step.intensity01);
                EditorGUILayout.LabelField("Recommendation", _agent.ScoreSelected().ToString());
                EditorGUILayout.LabelField(over ? "RED — over upper limit" : "Within limits");
                EditorGUILayout.LabelField(step.hasInpaint ? "BLUE — CivilianPaperDoll in-paint" : "Procedural");
            }
            CivilianPaperDollPreview.Draw(_agent, _warden, GUILayoutUtility.GetRect(280, 120));
        }

        EditorGUILayout.EndScrollView();
        if (GUI.changed && _warden != null)
            EditorUtility.SetDirty(_warden);
    }
}

/// <summary>2D civilian silhouette: blue = in-paint, dotted white = predicted, red halo = warden limit breach.</summary>
public static class CivilianPaperDollPreview
{
    public static void Draw(JusticeRehabilitationTravelAgent agent, PrisonWarden warden, Rect rect)
    {
        if (Event.current.type != EventType.Repaint || agent == null)
            return;
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.12f, 1f));
        Vector2 c = rect.center;
        bool over = warden != null && agent.SelectedOverLimit();
        Handles.BeginGUI();
        if (over)
        {
            Handles.color = new Color(1f, 0.15f, 0.1f, 0.55f);
            Handles.DrawSolidDisc(c, Vector3.forward, 36f);
        }
        Handles.color = new Color(0.3f, 0.45f, 1f, 0.95f);
        Vector3 inpaint = agent.InpaintPlacement();
        Vector2 blue = c + new Vector2(Mathf.Clamp(inpaint.x, -40f, 40f), Mathf.Clamp(-inpaint.z, -30f, 30f));
        Handles.DrawSolidDisc(blue, Vector3.forward, 10f);
        Handles.color = Color.white;
        Vector3 pred = agent.PredictedPlacement();
        Vector2 white = c + new Vector2(Mathf.Clamp(pred.x, -40f, 40f), Mathf.Clamp(-pred.z, -30f, 30f));
        Handles.DrawDottedLine(c, white, 3f);
        Handles.DrawWireDisc(white, Vector3.forward, 8f);
        Handles.EndGUI();
    }
}
