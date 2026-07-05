#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Work orders browser for continuuuum DB. View, assign, and export fungible work orders from causality tree.
/// Requires work_orders table (Scripts/continuuuum_episodes_schema.sql).
/// </summary>
public class ContinuuuumWorkOrdersWindow : EditorWindow
{
    // todo: review: again!
    [SerializeField] private string dbPath = "";
    [SerializeField] private string pythonPath = "";
    [SerializeField] private string tenantId = "";
    [SerializeField] private string lastError = "";
    [SerializeField] private Vector2 scroll;
    [SerializeField] private string statusFilter = "pending";

    [MenuItem("Window/Continuuuum/Continuuuum Work Orders")]
    public static void ShowWindow()
    {
        var w = GetWindow<ContinuuuumWorkOrdersWindow>("Continuuuum Work Orders");
        w.minSize = new Vector2(420, 320);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Continuuuum Work Orders", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Browse work orders from causality tree or from screenplay (dialogue/SFX). Filter by source; prompt_description shows quote or SFX text for screenplay-derived orders. Use POST /api/episodes/<id>/extract-screenplay-work-orders to extract from script_speech_audio and script_sound_effects.", MessageType.Info);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DB Path", GUILayout.Width(60));
        dbPath = EditorGUILayout.TextField(dbPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string p = EditorUtility.OpenFilePanel("Select continuuuum.db", string.IsNullOrEmpty(dbPath) ? Application.dataPath : Path.GetDirectoryName(dbPath), "db");
            if (!string.IsNullOrEmpty(p)) dbPath = p;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Python path", GUILayout.Width(80));
        pythonPath = EditorGUILayout.TextField(pythonPath);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tenant", GUILayout.Width(60));
        tenantId = EditorGUILayout.TextField(tenantId);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Browse Work Orders", EditorStyles.boldLabel);
        statusFilter = EditorGUILayout.TextField("Status filter", statusFilter);
        if (GUILayout.Button("Browse All Work Orders"))
            OpenExplorerWithQuery("SELECT * FROM work_orders LIMIT 100");
        if (GUILayout.Button("Browse by Status"))
        {
            string status = string.IsNullOrWhiteSpace(statusFilter) ? "pending" : statusFilter.Trim();
            OpenExplorerWithQuery($"SELECT * FROM work_orders WHERE status = '{status.Replace("'", "''")}' LIMIT 100");
        }
        EditorGUILayout.LabelField("By source (screenplay)", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Causality"))
            OpenExplorerWithQuery("SELECT id, episode_id, work_order_source, causality_leaf_id, prompt_description, status FROM work_orders WHERE work_order_source = 'causality' LIMIT 100");
        if (GUILayout.Button("Dialogue"))
            OpenExplorerWithQuery("SELECT id, episode_id, work_order_source, speech_audio_id, episode_script_id, prompt_description, status FROM work_orders WHERE work_order_source = 'dialogue' LIMIT 100");
        if (GUILayout.Button("SFX"))
            OpenExplorerWithQuery("SELECT id, episode_id, work_order_source, sound_effect_id, episode_script_id, prompt_description, status FROM work_orders WHERE work_order_source = 'sfx' LIMIT 100");
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Browse Narrative Type Detections"))
            OpenExplorerWithQuery("SELECT * FROM narrative_type_detections LIMIT 100");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Export work orders to JSON or CSV for Jira, Linear, or dev studio. Requires work order generation from causality tree.", MessageType.None);
        if (GUILayout.Button("Export Work Orders (stub)"))
            EditorUtility.DisplayDialog("Export", "Export to JSON/CSV will be implemented when work order generation pipeline is wired. Use Browse and copy data for now.", "OK");

        if (!string.IsNullOrEmpty(lastError))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(lastError, MessageType.Error);
        }
        EditorGUILayout.EndScrollView();
    }

    private void OpenExplorerWithQuery(string sql)
    {
        ContinuuuumExplorerWindow.ShowAndRunQuery(dbPath, pythonPath, tenantId, sql);
    }
}
#endif
