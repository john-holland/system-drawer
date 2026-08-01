#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Window → System Drawer → Active Cards — live pool + history ring buffer.</summary>
public sealed class ActiveCardsEditorWindow : EditorWindow
{
    PhysicsCardSolver _solver;
    Vector2 _scrollActive;
    Vector2 _scrollHistory;
    string _filter = "";
    int _bufferSize = 5000;

    [MenuItem("Window/System Drawer/Active Cards", false, 230)]
    public static void ShowWindow() => GetWindow<ActiveCardsEditorWindow>("Active Cards");

    void OnEnable() => EditorApplication.update += Repaint;

    void OnDisable() => EditorApplication.update -= Repaint;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Active Cards", EditorStyles.boldLabel);
        _solver = (PhysicsCardSolver)EditorGUILayout.ObjectField("Solver", _solver, typeof(PhysicsCardSolver), true);
        if (_solver == null && Selection.activeGameObject != null)
            _solver = Selection.activeGameObject.GetComponent<PhysicsCardSolver>();

        var hist = CardHistoryManager.Instance;
        if (hist == null)
        {
            if (GUILayout.Button("Create CardHistoryManager in scene"))
            {
                var go = new GameObject("CardHistoryManager");
                go.AddComponent<CardHistoryManager>();
            }
        }
        else
        {
            _bufferSize = EditorGUILayout.IntField("History buffer", hist.historyBufferSize);
            if (_bufferSize != hist.historyBufferSize && GUILayout.Button("Apply buffer size"))
                hist.SetBufferSize(_bufferSize);
            if (GUILayout.Button("Clear history"))
                hist.ClearHistory();
        }

        _filter = EditorGUILayout.TextField("Filter type", _filter);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Active (live pool snapshots)", EditorStyles.boldLabel);
        _scrollActive = EditorGUILayout.BeginScrollView(_scrollActive, GUILayout.Height(position.height * 0.4f));
        if (_solver != null)
        {
            IReadOnlyList<CardHistorySnapshot> active = hist != null
                ? hist.CopyActiveFrom(_solver)
                : CopyLocal(_solver);
            DrawSnaps(active);
        }
        else
            EditorGUILayout.HelpBox("Assign a PhysicsCardSolver.", MessageType.Info);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("History (newest first)", EditorStyles.boldLabel);
        _scrollHistory = EditorGUILayout.BeginScrollView(_scrollHistory);
        if (hist != null)
            DrawSnaps(hist.GetHistoryNewestFirst(300));
        else
            EditorGUILayout.HelpBox("No CardHistoryManager in scene.", MessageType.Warning);
        EditorGUILayout.EndScrollView();
    }

    void DrawSnaps(IReadOnlyList<CardHistorySnapshot> snaps)
    {
        if (snaps == null) return;
        for (int i = 0; i < snaps.Count; i++)
        {
            var s = snaps[i];
            if (s == null) continue;
            if (!string.IsNullOrEmpty(_filter) &&
                (s.typeName == null || s.typeName.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0) &&
                (s.displayName == null || s.displayName.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{s.eventKind} | {s.typeName} | {s.displayName}");
            EditorGUILayout.LabelField($"tag={s.physicalPathingTag}  duty={s.dutyOrActivitySummary}");
            EditorGUILayout.LabelField($"solver={s.actorOrSolverId}  t={s.unixMs}");
            EditorGUILayout.EndVertical();
        }
    }

    static List<CardHistorySnapshot> CopyLocal(PhysicsCardSolver solver)
    {
        var list = new List<CardHistorySnapshot>();
        if (solver?.availableCards == null) return list;
        for (int i = 0; i < solver.availableCards.Count; i++)
            list.Add(CardHistorySnapshot.FromCard(solver.availableCards[i], solver.name, "active"));
        return list;
    }
}

public sealed class ChefCardEditorWindow : EditorWindow
{
    ChefCard _card = ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Sear, null);

    [MenuItem("Window/System Drawer/Cards/Chef", false, 242)]
    public static void ShowWindow() => GetWindow<ChefCardEditorWindow>("Chef Cards");

    void OnGUI()
    {
        _card.dutyMode = (ChefDutyMode)EditorGUILayout.EnumPopup("Duty", _card.dutyMode);
        _card.activity = (ChefActivity)EditorGUILayout.EnumPopup("Activity", _card.activity);
        _card.stationOrTarget = (GameObject)EditorGUILayout.ObjectField("Station", _card.stationOrTarget, typeof(GameObject), true);
        _card.pourRateLitersPerSec = EditorGUILayout.FloatField("Pour L/s", _card.pourRateLitersPerSec);
        _card.accuracy01 = EditorGUILayout.Slider("Accuracy", _card.accuracy01, 0f, 1f);
        if (GUILayout.Button("Reset Defaults"))
            _card = ChefCard.Generate(_card.dutyMode, _card.activity, _card.stationOrTarget);
    }
}
#endif
