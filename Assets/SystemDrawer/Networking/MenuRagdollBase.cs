using UnityEngine;

/// <summary>Abstract main-menu host: event routing + optional Tomba-style 2D hanging physics.</summary>
[AddComponentMenu("System Drawer/Networking/Menu Ragdoll Base")]
public abstract class MenuRagdollBase : MonoBehaviour
{
    public const string ServiceKey = SystemDrawerServiceKeys.MenuRagdoll;

    [Header("Optional 2D hanging menu physics (Tomba-style)")]
    public bool enableHangingPhysics;
    public Transform ropeAnchor;
    public float ropeLength = 1.2f;
    public float plankMass = 0.8f;
    public float swayDamping = 0.4f;
    public float selectionImpulse = 0.5f;

    Rigidbody2D _plankBody;

    protected ClientOrchestrator Client => ClientOrchestrator.Instance;
    protected ServerOrchestrator Server => FindAnyObjectByType<ServerOrchestrator>();
    protected MenuRagdoll Menu => this as MenuRagdoll;

    protected virtual void Awake()
    {
        if (enableHangingPhysics)
            EnsureHangingPhysics();
        RegisterService();
    }

    protected virtual void OnDestroy() => UnregisterService();

    void RegisterService()
    {
        SystemDrawerService.FindInScene()?.Register(ServiceKey, this);
    }

    void UnregisterService()
    {
        SystemDrawerService.FindInScene()?.Unregister(ServiceKey);
    }

    public void EnsureHangingPhysics()
    {
        if (ropeAnchor == null)
        {
            var anchorGo = new GameObject("RopeAnchor");
            anchorGo.transform.SetParent(transform, false);
            ropeAnchor = anchorGo.transform;
            ropeAnchor.localPosition = new Vector3(0f, ropeLength, 0f);
        }

        var anchorRb = ropeAnchor.GetComponent<Rigidbody2D>();
        if (anchorRb == null)
            anchorRb = ropeAnchor.gameObject.AddComponent<Rigidbody2D>();
        anchorRb.bodyType = RigidbodyType2D.Static;

        Transform plank = transform;
        _plankBody = plank.GetComponent<Rigidbody2D>();
        if (_plankBody == null)
            _plankBody = plank.gameObject.AddComponent<Rigidbody2D>();
        _plankBody.bodyType = RigidbodyType2D.Dynamic;
        _plankBody.mass = plankMass;
        _plankBody.gravityScale = 1f;
        _plankBody.linearDamping = swayDamping;
        _plankBody.angularDamping = swayDamping;

        if (plank.GetComponent<BoxCollider2D>() == null)
            plank.gameObject.AddComponent<BoxCollider2D>();

        var joint = plank.GetComponent<DistanceJoint2D>();
        if (joint == null)
            joint = plank.gameObject.AddComponent<DistanceJoint2D>();
        joint.connectedBody = anchorRb;
        joint.autoConfigureConnectedAnchor = true;
        joint.maxDistanceOnly = true;
        joint.distance = ropeLength;
    }

    public void ApplySelectionImpulse()
    {
        if (_plankBody != null)
            _plankBody.AddForce(Vector2.right * selectionImpulse, ForceMode2D.Impulse);
    }

    public virtual bool HandleBubble(MenuRagdollEvent e)
    {
        if (e.Name == null)
            return false;
        switch (e.Name)
        {
            case "start":
                Client?.SetServerMode(NetworkServerMode.SinglePlayer);
                return true;
            case "multiplayer":
                Client?.SetServerMode(NetworkServerMode.AuthoritativePeerToPeer);
                BroadcastMenuRefresh();
                return true;
            case "settings":
                return true;
            case "save":
                Server?.HandleSavePublic(e.Payload as string ?? "default");
                return true;
            case "load":
                Server?.HandleLoadPublic(e.Payload as string ?? "default");
                return true;
            case "lobby.connect":
                if (e.Payload is string hostPort)
                    TryConnect(hostPort, NetworkClientRole.Player);
                return true;
            case "lobby.host.options":
                if (e.Payload is LobbyHostOptions opts)
                    Server?.ApplyHostOptions(opts);
                else if (Menu != null)
                    Server?.ApplyHostOptions(Menu.BuildHostOptions());
                return true;
            case "lobby.host.password":
                if (Menu != null && e.Payload is string hostPw)
                    Menu.hostLobbyPassword = hostPw;
                return true;
            case "lobby.join.password":
                if (Menu != null && e.Payload is string joinPw)
                    Menu.joinLobbyPassword = joinPw;
                return true;
            case "lobby.host.start":
                if (Menu != null)
                    Server?.StartLobbyHost(Menu.BuildHostOptions());
                else
                    Server?.StartLobbyHost();
                return true;
            case "lobby.host.stop":
                Server?.StopLobbyHost();
                return true;
            case "lobby.join":
                TryJoinLobby(e.Payload, NetworkClientRole.Player);
                return true;
            case "lobby.spectate.join":
                TryJoinLobby(e.Payload, NetworkClientRole.Spectator);
                return true;
            case "lobby.game.start":
                TryConnect(e.Payload as string ?? "127.0.0.1:7777", NetworkClientRole.Player);
                return true;
            case "lobby.game.end":
                Client?.Disconnect();
                return true;
        }
        return false;
    }

    void BroadcastMenuRefresh()
    {
        var nodes = GetComponentsInChildren<MenuRagdollNode>(true);
        var refresh = new MenuRagdollEvent("menu.refresh", null);
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].BroadcastDescend(refresh, "menu.refresh");
    }

    void TryConnect(string hostPort, NetworkClientRole role)
    {
        if (string.IsNullOrEmpty(hostPort) || Client == null)
            return;
        var parts = hostPort.Split(':');
        string host = parts[0];
        int port = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : NetworkSettings.Default.gamePort;
        if (role == NetworkClientRole.Spectator)
            Client.ConnectAsSpectator(host, port);
        else
            Client.Connect(host, port, NetworkClientRole.Player);
    }

    void TryJoinLobby(object payload, NetworkClientRole role)
    {
        LobbyJoinPayload join = payload as LobbyJoinPayload;
        if (join == null && Menu != null)
            join = Menu.BuildJoinPayload(role, payload as string);
        if (join == null || string.IsNullOrEmpty(join.hostPort))
            return;

        var parts = join.hostPort.Split(':');
        string host = parts[0];
        int lobbyPort = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : NetworkSettings.Default.lobbyPort;

        var info = LobbyClientQuery.Query(host, lobbyPort);
        if (!info.Ok)
        {
            Debug.LogWarning("[MenuRagdollBase] lobby query failed: " + info.Error);
            return;
        }
        if (info.passwordRequired && string.IsNullOrEmpty(join.password))
        {
            BubbleJoinPasswordRequired();
            return;
        }
        if (role == NetworkClientRole.Spectator && !info.allowSpectators)
        {
            Debug.LogWarning("[MenuRagdollBase] spectate not allowed");
            return;
        }
        if (!LobbyClientQuery.Register(host, lobbyPort, role, Client?.ClientId ?? "client", join.password, out var regInfo))
        {
            if (regInfo != null && regInfo.Error == "password")
                BubbleJoinPasswordDenied();
            return;
        }
        int gamePort = regInfo != null && regInfo.gamePort > 0 ? regInfo.gamePort : info.gamePort;
        if (!string.IsNullOrEmpty(join.password))
            Client?.SetLobbyPasswordHash(LobbyPasswordHash.Hash(join.password, info.sessionName));
        TryConnect(host + ":" + gamePort, role);
    }

    protected virtual void BubbleJoinPasswordRequired()
    {
        var nodes = GetComponentsInChildren<MenuRagdollNode>(true);
        var evt = new MenuRagdollEvent("lobby.join.password.required", null);
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].BubbleUp(evt);
    }

    protected virtual void BubbleJoinPasswordDenied()
    {
        var nodes = GetComponentsInChildren<MenuRagdollNode>(true);
        var evt = new MenuRagdollEvent("lobby.join.password.denied", null);
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].BubbleUp(evt);
    }
}
