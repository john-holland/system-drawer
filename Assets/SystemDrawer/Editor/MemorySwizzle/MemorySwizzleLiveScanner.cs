using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>Builds object records from loaded Unity objects (editor/play).</summary>
public static class MemorySwizzleLiveScanner
{
    public static List<MemorySwizzleObjectRecord> Scan(bool includeInactive = true)
    {
        var records = new List<MemorySwizzleObjectRecord>(4096);
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        var goById = new Dictionary<int, GameObject>();

        for (int i = 0; i < transforms.Length; i++)
        {
            var t = transforms[i];
            if (t == null)
                continue;
            var go = t.gameObject;
            if (go == null)
                continue;
            if (!includeInactive && !go.activeInHierarchy)
                continue;
            if (EditorUtility.IsPersistent(go) && !go.scene.IsValid())
                continue;

            goById[go.GetInstanceID()] = go;
        }

        foreach (var kv in goById)
        {
            var go = kv.Value;
            int id = kv.Key;
            string path = BuildPath(go.transform);
            long size = SafeRuntimeSize(go);
            int parentId = go.transform.parent != null ? go.transform.parent.gameObject.GetInstanceID() : 0;

            records.Add(new MemorySwizzleObjectRecord
            {
                Name = go.name,
                TypeName = "GameObject",
                SystemType = typeof(GameObject),
                SizeBytes = size,
                InstanceId = id,
                ParentInstanceId = parentId,
                ScenePath = path,
                IsGameObject = true,
                IsComponent = false
            });

            var components = go.GetComponents<Component>();
            for (int c = 0; c < components.Length; c++)
            {
                var comp = components[c];
                if (comp == null)
                    continue;
                var ct = comp.GetType();
                records.Add(new MemorySwizzleObjectRecord
                {
                    Name = ct.Name,
                    TypeName = ct.FullName ?? ct.Name,
                    SystemType = ct,
                    SizeBytes = SafeRuntimeSize(comp),
                    InstanceId = comp.GetInstanceID(),
                    ParentInstanceId = id,
                    ScenePath = path + "/" + ct.Name,
                    IsGameObject = false,
                    IsComponent = true
                });
            }
        }

        return records;
    }

    static long SafeRuntimeSize(UnityEngine.Object obj)
    {
        try
        {
            return Math.Max(0, Profiler.GetRuntimeMemorySizeLong(obj));
        }
        catch
        {
            return 0;
        }
    }

    static string BuildPath(Transform t)
    {
        if (t == null)
            return "Scene";
        string scene = t.gameObject.scene.IsValid() ? t.gameObject.scene.name : "DontDestroyOnLoad";
        var parts = new List<string> { scene };
        var stack = new Stack<string>();
        Transform cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        while (stack.Count > 0)
            parts.Add(stack.Pop());
        return string.Join("/", parts);
    }
}
