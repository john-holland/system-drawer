using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NetworkingStandardAssets
{
    const string SettingsPath = "Assets/SystemDrawer/StandardAssets/Networking/DefaultNetworkSettings.asset";

    public static WizardSetupReport Setup(NetworkServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var hub = ResolveHubRoot(wizard);
        var netRoot = FindOrCreateChild(hub, "_Networking", report);

        var client = wizard.clientOrchestrator;
        if (client == null)
            client = netRoot.GetComponentInChildren<ClientOrchestrator>(true);
        if (client == null)
            client = Object.FindFirstObjectByType<ClientOrchestrator>(FindObjectsInactive.Include);
        if (client == null)
            client = FindOrAddComponent<ClientOrchestrator>(netRoot.gameObject, report, "ClientOrchestrator");
        else
            report.Skipped.Add("ClientOrchestrator");

        var server = wizard.serverOrchestrator;
        if (server == null)
            server = netRoot.GetComponentInChildren<ServerOrchestrator>(true);
        if (server == null)
            server = Object.FindFirstObjectByType<ServerOrchestrator>(FindObjectsInactive.Include);
        if (server == null)
            server = FindOrAddComponent<ServerOrchestrator>(netRoot.gameObject, report, "ServerOrchestrator");
        else
            report.Skipped.Add("ServerOrchestrator");

        var settings = LoadOrCreateNetworkSettings(report);
        AssignSettings(client, settings, report);
        AssignSettings(server, settings, report);

        Undo.RecordObject(wizard, "Assign network orchestrators");
        wizard.clientOrchestrator = client;
        wizard.serverOrchestrator = server;
        EditorUtility.SetDirty(wizard);
        report.Linked.Add("NetworkServiceWizard orchestrators");

        if (netRoot.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(netRoot.scene);
        return report;
    }

    static NetworkSettings LoadOrCreateNetworkSettings(WizardSetupReport report)
    {
        EnsureFolder("Assets/SystemDrawer/StandardAssets/Networking");
        var existing = AssetDatabase.LoadAssetAtPath<NetworkSettings>(SettingsPath);
        if (existing != null)
        {
            report.Skipped.Add("DefaultNetworkSettings");
            return existing;
        }

        var settings = ScriptableObject.CreateInstance<NetworkSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        report.Created.Add("DefaultNetworkSettings");
        return settings;
    }

    static void AssignSettings(ClientOrchestrator client, NetworkSettings settings, WizardSetupReport report)
    {
        if (client == null || settings == null)
            return;
        var so = new SerializedObject(client);
        var prop = so.FindProperty("settings");
        if (prop != null && prop.objectReferenceValue != settings)
        {
            prop.objectReferenceValue = settings;
            so.ApplyModifiedProperties();
            report.Linked.Add("ClientOrchestrator.settings");
        }
    }

    static void AssignSettings(ServerOrchestrator server, NetworkSettings settings, WizardSetupReport report)
    {
        if (server == null || settings == null)
            return;
        var so = new SerializedObject(server);
        var prop = so.FindProperty("settings");
        if (prop != null && prop.objectReferenceValue != settings)
        {
            prop.objectReferenceValue = settings;
            so.ApplyModifiedProperties();
            report.Linked.Add("ServerOrchestrator.settings");
        }
    }

    static Transform ResolveHubRoot(Component wizard)
    {
        var fac = wizard.GetComponentInParent<SystemDrawerFacilitator>();
        if (fac != null)
            return fac.transform;
        var svc = wizard.GetComponentInParent<SystemDrawerService>();
        if (svc != null)
            return svc.transform;
        return wizard.transform;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static GameObject FindOrCreateChild(Transform parent, string name, WizardSetupReport report)
    {
        var existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
        {
            report.Skipped.Add("GameObject " + name);
            return existing.gameObject;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        if (parent != null)
            Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        report.Created.Add("GameObject " + name);
        return go;
    }

    static T FindOrAddComponent<T>(GameObject go, WizardSetupReport report, string label) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null)
        {
            report.Skipped.Add(label);
            return c;
        }

        c = Undo.AddComponent<T>(go);
        report.Created.Add(label);
        EditorUtility.SetDirty(go);
        return c;
    }
}
