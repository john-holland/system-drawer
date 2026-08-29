using UnityEditor;
using UnityEngine;

/// <summary>Edit lobby prefab params, sync with Continuuuum, and save sessions to the local client.</summary>
public sealed class LobbyPrefabSyncWindow : EditorWindow
{
    ServerOrchestrator _server;
    Vector2 _scroll;
    string _status = "";

    [MenuItem("Window/System Drawer/Networking/Lobby Prefab Sync")]
    public static void Open()
    {
        var w = GetWindow<LobbyPrefabSyncWindow>("Lobby Prefab Sync");
        w.minSize = new Vector2(420, 360);
    }

    [MenuItem("Window/System Drawer/Networking/Game Lobbies")]
    public static void OpenGameLobbiesPage()
    {
        string url = ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/') + "/game-lobbies";
        Application.OpenURL(url);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _server = (ServerOrchestrator)EditorGUILayout.ObjectField("Server", _server, typeof(ServerOrchestrator), true);
        if (_server == null)
            _server = Object.FindAnyObjectByType<ServerOrchestrator>();
        if (_server == null)
        {
            EditorGUILayout.HelpBox("Assign a ServerOrchestrator.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        _server.EnsureReady();
        var settings = _server.Settings != null ? _server.Settings : NetworkSettings.Default;
        if (settings.prefab == null)
            settings.prefab = new LobbyPrefabParameters();
        var p = settings.prefab;
        EditorGUILayout.LabelField("Lobby instance", settings.lobbySessionName);
        p.configId = EditorGUILayout.TextField("Config id", p.configId ?? "");
        p.configName = EditorGUILayout.TextField("Config name", p.configName ?? "");
        p.gameSize = EditorGUILayout.IntField("Game size", p.gameSize);
        p.minPlayersToStart = EditorGUILayout.IntField("Min players to start", p.minPlayersToStart);
        p.mode = (NetworkServerMode)EditorGUILayout.EnumPopup("Mode", p.mode);
        p.requirePassword = EditorGUILayout.Toggle("Require password", p.requirePassword);
        p.allowSpectators = EditorGUILayout.Toggle("Allow spectators", p.allowSpectators);
        p.maxSpectators = EditorGUILayout.IntField("Max spectators", p.maxSpectators);
        p.lobbyTypeId = EditorGUILayout.TextField("Lobby type id", p.lobbyTypeId ?? "");
        p.contentKind = (LobbyContentKind)EditorGUILayout.EnumPopup("Content kind", p.contentKind);
        p.contentId = EditorGUILayout.TextField("Content id", p.contentId ?? "");
        EditorGUILayout.LabelField("Properties JSON");
        p.propertiesJson = EditorGUILayout.TextArea(p.propertiesJson ?? "{}", GUILayout.MinHeight(60));
        if (!p.TryValidateProperties(out var err))
            EditorGUILayout.HelpBox(err, MessageType.Error);

        if (GUILayout.Button("Sync from Continuuuum"))
        {
            if (!string.IsNullOrEmpty(p.configId))
            {
                string cfgJson = GameLobbyContinuuuumClient.GetConfig(p.configId);
                _status = GameLobbyContinuuuumClient.TryApplyConfigJson(cfgJson, settings, _server)
                    ? "Applied config from Continuuuum (instance name unchanged)"
                    : "Get config failed";
            }
            else
            {
                string json = GameLobbyContinuuuumClient.GetLobby(settings.lobbySessionName);
                _status = GameLobbyContinuuuumClient.TryApplyLobbyJson(json, settings, _server)
                    ? "Applied lobby instance from Continuuuum"
                    : "Get lobby failed";
            }
        }
        if (GUILayout.Button("Sync to Continuuuum"))
        {
            _server.ApplyLobbyPrefab(p);
            GameLobbyContinuuuumClient.PutPrefab(settings.lobbySessionName, p);
            GameLobbyContinuuuumClient.Heartbeat(_server);
            _status = "Pushed prefab + heartbeat";
        }
        if (GUILayout.Button("Save to Local Client"))
        {
            _server.GameSessions?.SaveAllToLocalClient();
            _status = "Saved all sessions";
        }
        if (GUILayout.Button("Master Rebake"))
            MasterRebakeRunner.Run();
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }
}
