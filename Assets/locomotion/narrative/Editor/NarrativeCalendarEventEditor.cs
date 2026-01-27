using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using Locomotion.Narrative;

/// <summary>
/// Custom editor for NarrativeCalendarEvent that adds action type dropdown and Add button.
/// </summary>
[CustomEditor(typeof(NarrativeCalendarAsset))]
public class NarrativeCalendarEventEditor : Editor
{
    private int selectedActionTypeIndex = 0;
    private string[] actionTypeNames;
    private System.Type[] actionTypes;

    private void OnEnable()
    {
        // Find all NarrativeActionSpec subclasses using reflection
        FindActionTypes();
    }

    private void FindActionTypes()
    {
        List<System.Type> types = new List<System.Type>();
        List<string> names = new List<string>();

        // Get all types in the Narrative namespace
        var narrativeAssembly = typeof(NarrativeActionSpec).Assembly;
        var allTypes = narrativeAssembly.GetTypes();

        foreach (var type in allTypes)
        {
            if (type.IsSubclassOf(typeof(NarrativeActionSpec)) && !type.IsAbstract)
            {
                types.Add(type);
                names.Add(type.Name);
            }
        }

        // Also check Assembly-CSharp for any action types there
        var defaultAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
        
        if (defaultAssembly != null)
        {
            foreach (var type in defaultAssembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(NarrativeActionSpec)) && !type.IsAbstract && !types.Contains(type))
                {
                    types.Add(type);
                    names.Add(type.Name);
                }
            }
        }

        // Sort alphabetically by name while keeping types and names in sync
        var sortedPairs = types.Zip(names, (t, n) => new { Type = t, Name = n })
            .OrderBy(pair => pair.Name)
            .ToArray();

        actionTypes = sortedPairs.Select(p => p.Type).ToArray();
        actionTypeNames = sortedPairs.Select(p => p.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NarrativeCalendarAsset calendar = (NarrativeCalendarAsset)target;

        if (calendar.events == null)
        {
            calendar.events = new List<NarrativeCalendarEvent>();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Actions Editor", EditorStyles.boldLabel);

        // Draw events with action editing
        for (int i = 0; i < calendar.events.Count; i++)
        {
            var evt = calendar.events[i];
            if (evt == null)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Event: {evt.title}", EditorStyles.boldLabel);

            if (evt.actions == null)
            {
                evt.actions = new List<NarrativeActionSpec>();
            }

            // Draw existing actions
            EditorGUILayout.LabelField("Actions:", EditorStyles.miniLabel);
            for (int j = 0; j < evt.actions.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var action = evt.actions[j];
                if (action != null)
                {
                    EditorGUILayout.LabelField($"{j + 1}. {action.GetType().Name}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"{j + 1}. Null Action", EditorStyles.miniLabel);
                }

                // Remove button
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    evt.actions.RemoveAt(j);
                    j--;
                    EditorUtility.SetDirty(target);
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Action type dropdown and Add button
            EditorGUILayout.BeginHorizontal();
            
            if (actionTypeNames != null && actionTypeNames.Length > 0)
            {
                selectedActionTypeIndex = EditorGUILayout.Popup("Action Type", selectedActionTypeIndex, actionTypeNames);
                
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    AddActionToEvent(evt, selectedActionTypeIndex);
                    EditorUtility.SetDirty(target);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No action types found. Make sure NarrativeActionSpec subclasses exist.", MessageType.Warning);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    private void AddActionToEvent(NarrativeCalendarEvent evt, int actionTypeIndex)
    {
        if (actionTypes == null || actionTypeIndex < 0 || actionTypeIndex >= actionTypes.Length)
        {
            Debug.LogWarning("[NarrativeCalendarEventEditor] Invalid action type index");
            return;
        }

        System.Type actionType = actionTypes[actionTypeIndex];
        
        try
        {
            // Create instance of action type
            NarrativeActionSpec newAction = (NarrativeActionSpec)System.Activator.CreateInstance(actionType);
            
            if (newAction != null)
            {
                evt.actions.Add(newAction);
                Debug.Log($"[NarrativeCalendarEventEditor] Added {actionType.Name} to event '{evt.title}'");
            }
            else
            {
                Debug.LogError($"[NarrativeCalendarEventEditor] Failed to create instance of {actionType.Name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NarrativeCalendarEventEditor] Error creating action: {e.Message}");
        }
    }
}
