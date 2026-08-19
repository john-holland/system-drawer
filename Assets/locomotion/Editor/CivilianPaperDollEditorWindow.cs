using UnityEditor;
using UnityEngine;

public sealed class CivilianPaperDollEditorWindow : EditorWindow
{
    public static readonly string[] GradeAxes = CivilianPaperDoll.GradeAxes;

    CivilianPaperDoll _doll;
    CareerWarden _warden;
    Vector2 _scroll;
    int _stepPick;

    [MenuItem("Locomotion/Civilian Paper Doll")]
    public static void Open()
    {
        var w = GetWindow<CivilianPaperDollEditorWindow>("Civilian Paper Doll");
        w.minSize = new Vector2(440, 480);
    }

    public static void OpenWith(CivilianPaperDoll doll)
    {
        Open();
        GetWindow<CivilianPaperDollEditorWindow>()._doll = doll;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _doll = (CivilianPaperDoll)EditorGUILayout.ObjectField("Paper Doll", _doll, typeof(CivilianPaperDoll), false);
        _warden = (CareerWarden)EditorGUILayout.ObjectField("Career Warden", _warden, typeof(CareerWarden), true);
        if (_doll == null)
        {
            EditorGUILayout.HelpBox("Assign a CivilianPaperDoll.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _doll.personaKey = EditorGUILayout.TextField("Persona", _doll.personaKey);
        _doll.ageBand = (CivilianAgeBand)EditorGUILayout.EnumPopup("Age band", _doll.ageBand);
        _doll.education = (CivilianEducationAttainment)EditorGUILayout.EnumPopup("Education", _doll.education);
        _doll.employment = (CivilianEmploymentStatus)EditorGUILayout.EnumPopup("Employment", _doll.employment);
        _doll.currentRoleId = EditorGUILayout.TextField("Role", _doll.currentRoleId);
        _doll.employerCompanyId = EditorGUILayout.TextField("Employer", _doll.employerCompanyId);
        _doll.isGovernmentJob = EditorGUILayout.Toggle("Government job (label only)", _doll.isGovernmentJob);

        var agent = _doll.educationalPlan;
        agent = (EducationalTravelAgent)EditorGUILayout.ObjectField(
            "Educational Plan", agent, typeof(EducationalTravelAgent), true);
        _doll.educationalPlan = agent;

        if (agent != null && agent.steps != null && agent.steps.Count > 0)
        {
            string[] labels = new string[agent.steps.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                var s = agent.steps[i];
                labels[i] = s == null ? i.ToString() : $"{i}: {s.station} {s.effect}";
            }
            _stepPick = Mathf.Clamp(_doll.selectedStepIndex, 0, labels.Length - 1);
            _stepPick = EditorGUILayout.Popup("Plan step", _stepPick, labels);
            _doll.selectedStepIndex = _stepPick;
            agent.selectedStepIndex = _stepPick;
        }

        float[] blue = _doll.Expected01();
        float[] red = _doll.FireLimit01();
        float[] white = _doll.WhiteStep01();
        float halo = 0f;
        if (_warden != null)
        {
            var grade = _warden.GradeEmployee(_doll);
            halo = _warden.OverFireLimit(_doll, grade) ? 0.8f : 0f;
            EditorGUILayout.LabelField("Grade", _warden.lastRecommendation.ToString());
        }

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(diamond, GradeAxes, blue, red, white, halo);
        EditorGUILayout.LabelField("Blue = expected limits  ·  Red = fire ceiling  ·  White = selected educational step");

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_doll);
    }
}
