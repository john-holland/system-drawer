using UnityEngine;

/// <summary>Runtime CLI verbs for headed / dedicated servers (stdin).</summary>
public static class NetworkRuntimeCli
{
    public static void TryExecute(string line, ServerOrchestrator server)
    {
        if (server == null || string.IsNullOrWhiteSpace(line))
            return;
        var parts = line.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;
        string verb = parts[0].ToLowerInvariant();
        switch (verb)
        {
            case "listen":
                if (parts.Length > 1 && int.TryParse(parts[1], out int port))
                    server.StartListening(port);
                break;
            case "mode":
                if (parts.Length > 1)
                    server.SetMode(ParseMode(parts[1]));
                break;
            case "lobby":
                HandleLobby(parts, server);
                break;
            case "clients":
                Debug.Log("[NetworkRuntimeCli] clients=" + server.ClientCount);
                break;
            case "kick":
                if (parts.Length > 1)
                    server.KickClient(parts[1]);
                break;
            case "impersonate":
                if (parts.Length > 1)
                    server.ImpersonateClient(parts[1]);
                break;
        }
    }

    static void HandleLobby(string[] parts, ServerOrchestrator server)
    {
        if (parts.Length < 2)
            return;
        switch (parts[1].ToLowerInvariant())
        {
            case "start":
                int port = 0;
                if (parts.Length > 2)
                    int.TryParse(parts[2], out port);
                server.StartLobbyHost(port);
                break;
            case "stop":
                server.StopLobbyHost();
                break;
            case "status":
                Debug.Log($"[NetworkRuntimeCli] lobby hosting={server.IsLobbyHosting} addr={server.LobbyAdvertiseAddress}");
                break;
        }
    }

    static NetworkServerMode ParseMode(string raw)
    {
        switch (raw.ToLowerInvariant())
        {
            case "p2p":
                return NetworkServerMode.AuthoritativePeerToPeer;
            case "lockstep":
                return NetworkServerMode.ClassicLockstep;
            default:
                return NetworkServerMode.SinglePlayer;
        }
    }
}
