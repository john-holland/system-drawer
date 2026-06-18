#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Editor helpers for dedicated server standalone builds.</summary>
public static class DedicatedServerLauncher
{
    public const string MenuPath = "Window/System Drawer/Networking/Copy Dedicated Server Launch Args";

    [MenuItem(MenuPath)]
    public static void CopyLaunchArgsExample()
    {
        string example =
            "-batchmode -nographics -ds -m p2p -p 7777 --host-lobby --lobby-port 7780 --lobby-name \"Campaign Co-op\"";
        EditorGUIUtility.systemCopyBuffer = example;
        Debug.Log("[DedicatedServerLauncher] Copied launch args: " + example);
    }

    [MenuItem("Window/System Drawer/Networking/Apply Launch Args To Scene Server")]
    public static void ApplyLaunchArgsToSceneServer()
    {
        var server = Object.FindAnyObjectByType<ServerOrchestrator>();
        if (server == null)
        {
            Debug.LogWarning("[DedicatedServerLauncher] No ServerOrchestrator in scene.");
            return;
        }
        NetworkLaunchArgs.Parse();
        NetworkLaunchArgs.ApplyTo(server);
        EditorUtility.SetDirty(server);
    }
}
#endif
