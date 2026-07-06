using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class WizardStandardAssetsCore
{
    internal const string StandardSceneChildName = "_StandardScene";

    internal static void EnsureFolder(string assetFolderPath)
    {
        if (string.IsNullOrWhiteSpace(assetFolderPath))
            return;
        assetFolderPath = assetFolderPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        var parts = assetFolderPath.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
            throw new InvalidOperationException("Asset paths must start with Assets/: " + assetFolderPath);

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    internal static T LoadAssetAtPath<T>(string path) where T : Object
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    internal static T FindOrCreateAsset<T>(string path, Func<T> factory, WizardSetupReport report, string label)
        where T : Object
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var existing = LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            report?.Skipped.Add(label + " (" + path + ")");
            return existing;
        }

        var asset = factory();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        report?.Created.Add(label + " (" + path + ")");
        return asset;
    }

    internal static TextAsset FindOrCreateTextAsset(string path, string contents, WizardSetupReport report, string label)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var existing = LoadAssetAtPath<TextAsset>(path);
        if (existing != null)
        {
            report?.Skipped.Add(label + " (" + path + ")");
            return existing;
        }

        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, contents ?? string.Empty);
        AssetDatabase.ImportAsset(path);
        var imported = LoadAssetAtPath<TextAsset>(path);
        report?.Created.Add(label + " (" + path + ")");
        return imported;
    }

    internal static Transform ResolveHubRoot(Component wizard)
    {
        if (wizard == null)
            return null;
        var fac = wizard.GetComponentInParent<SystemDrawerFacilitator>();
        if (fac != null)
            return fac.transform;
        var svc = wizard.GetComponentInParent<SystemDrawerService>();
        if (svc != null)
            return svc.transform;
        return wizard.transform.root;
    }

    internal static Transform EnsureStandardSceneRoot(Transform hubRoot, WizardSetupReport report)
    {
        if (hubRoot == null)
            return null;

        var existing = hubRoot.Find(StandardSceneChildName);
        if (existing != null)
            return existing;

        var go = new GameObject(StandardSceneChildName);
        Undo.RegisterCreatedObjectUndo(go, "Create _StandardScene");
        Undo.SetTransformParent(go.transform, hubRoot, "Parent _StandardScene");
        report?.Created.Add(StandardSceneChildName + " under " + hubRoot.name);
        MarkSceneDirty(go);
        return go.transform;
    }

    internal static GameObject FindOrCreateChild(Transform parent, string name, WizardSetupReport report)
    {
        if (parent == null)
            return null;

        var existing = parent.Find(name);
        if (existing != null)
        {
            report?.Skipped.Add("GameObject " + name);
            return existing.gameObject;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        report?.Created.Add("GameObject " + name);
        MarkSceneDirty(go);
        return go;
    }

    internal static T FindOrAddComponent<T>(GameObject go, WizardSetupReport report, string label = null) where T : Component
    {
        if (go == null)
            return null;
        var c = go.GetComponent<T>();
        if (c != null)
        {
            report?.Skipped.Add(label ?? typeof(T).Name + " on " + go.name);
            return c;
        }

        c = Undo.AddComponent<T>(go);
        report?.Created.Add(label ?? typeof(T).Name + " on " + go.name);
        EditorUtility.SetDirty(go);
        MarkSceneDirty(go);
        return c;
    }

    internal static void MarkSceneDirty(Object context)
    {
        if (context is Component c && c.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        else if (context is GameObject go && go.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(go.scene);
    }

    internal static T FindFirstInScene<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }
}
