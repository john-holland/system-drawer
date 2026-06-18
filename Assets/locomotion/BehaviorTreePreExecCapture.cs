using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Reflection bridge to narrative exec state store without asm reference cycle.</summary>
static class BehaviorTreePreExecCapture
{
    static readonly HashSet<int> Captured = new HashSet<int>();

    public static void TryCapture(BehaviorTreeNode node)
    {
        if (node == null || !node.captureStateBeforeExec)
            return;
        int key = node.GetInstanceID();
        if (Captured.Contains(key))
            return;
        Captured.Add(key);

        var objects = new List<GameObject>();
        if (node.associatedObjects != null)
        {
            for (int i = 0; i < node.associatedObjects.Length; i++)
            {
                if (node.associatedObjects[i] != null)
                    objects.Add(node.associatedObjects[i]);
            }
        }
        if (objects.Count == 0)
            return;

        var storeType = Type.GetType("Locomotion.Narrative.NarrativeNodeExecStateStore, Locomotion.Narrative.Runtime");
        if (storeType == null)
            return;
        var instanceProp = storeType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var store = instanceProp?.GetValue(null);
        if (store == null)
            return;
        var captureMethod = storeType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Instance);
        captureMethod?.Invoke(store, new object[] { "bt", node.name, objects, Time.time });
    }

    public static void ClearSession() => Captured.Clear();
}
