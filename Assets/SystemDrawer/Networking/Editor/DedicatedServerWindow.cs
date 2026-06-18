#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Headed dedicated server management window.</summary>
public sealed class DedicatedServerWindow : EditorWindow
{
    ServerOrchestrator _server;
    Vector2 _scroll;
    string _impersonateId = "client-1";
    string _runtimeCli = "lobby status";

    [MenuItem("Window/System Drawer/Networking/Dedicated Server Window")]
    public static void Open()
    {
        GetWindow<DedicatedServerWindow>("Dedicated Server");
    }

    void OnGUI()
    {
        _server = (ServerOrchestrator)EditorGUILayout.ObjectField("Server", _server, typeof(ServerOrchestrator), true);
        if (_server == null)
            _server = UnityEngine.Object.FindAnyObjectByType<ServerOrchestrator>();
        if (_server == null)
        {
            EditorGUILayout.HelpBox("Add a ServerOrchestrator to the scene.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Mode", _server.Mode.ToString());
        EditorGUILayout.LabelField("Clients", _server.ClientCount.ToString());
        EditorGUILayout.LabelField("Lobby hosting", _server.IsLobbyHosting.ToString());
        EditorGUILayout.LabelField("Lobby address", _server.LobbyAdvertiseAddress);
        EditorGUILayout.LabelField("Lobby locked (--no-lobby)", _server.LobbyLockedByLaunchArgs.ToString());

        EditorGUILayout.Space();
        if (GUILayout.Button("Start Listening"))
            _server.StartListening();
        if (GUILayout.Button("Start SP Loopback"))
            _server.StartSinglePlayerLoopback();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lobby", EditorStyles.boldLabel);
        string lobbyPassword = EditorGUILayout.PasswordField("Lobby password", _server.LobbyPasswordPlaintext);
        if (GUILayout.Button("Apply lobby password"))
            _server.SetLobbyPassword(lobbyPassword);
        GUI.enabled = !_server.LobbyLockedByLaunchArgs;
        if (GUILayout.Button("Start Lobby Host"))
            _server.StartLobbyHost();
        GUI.enabled = true;
        if (GUILayout.Button("Stop Lobby Host"))
            _server.StopLobbyHost();

        EditorGUILayout.Space();
        _impersonateId = EditorGUILayout.TextField("Impersonate client id", _impersonateId);
        if (GUILayout.Button("Impersonate"))
            _server.ImpersonateClient(_impersonateId);

        EditorGUILayout.Space();
        _runtimeCli = EditorGUILayout.TextField("Runtime CLI", _runtimeCli);
        if (GUILayout.Button("Execute CLI"))
            NetworkRuntimeCli.TryExecute(_runtimeCli, _server);

        EditorGUILayout.Space();
        var audit = _server.AuditClientTrees();
        EditorGUILayout.LabelField("Causality audit", audit.Ok ? "OK" : audit.Reason);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < audit.Violations.Count; i++)
            EditorGUILayout.LabelField("- " + audit.Violations[i]);
        EditorGUILayout.EndScrollView();
    }
}
#endif
