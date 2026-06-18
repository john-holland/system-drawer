using System;
using System.Collections.Generic;

/// <summary>Parse semicolon KV hello payloads from clients.</summary>
public static class NetworkHelloPayload
{
    public static string Build(string clientId, NetworkClientRole role, string passwordHash = null)
    {
        string roleStr = role == NetworkClientRole.Spectator ? "spectator" : "player";
        if (string.IsNullOrEmpty(passwordHash))
            return clientId + ";role=" + roleStr;
        return clientId + ";role=" + roleStr + ";passwordHash=" + passwordHash;
    }

    public static void Parse(string payload, out string clientId, out NetworkClientRole role, out string passwordHash)
    {
        clientId = payload ?? "";
        role = NetworkClientRole.Player;
        passwordHash = null;
        if (string.IsNullOrEmpty(payload))
            return;
        int semi = payload.IndexOf(';');
        if (semi >= 0)
        {
            clientId = payload.Substring(0, semi);
            ParseKv(payload.Substring(semi + 1), out role, out passwordHash);
        }
    }

    static void ParseKv(string kvPart, out NetworkClientRole role, out string passwordHash)
    {
        role = NetworkClientRole.Player;
        passwordHash = null;
        string[] parts = kvPart.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            int eq = parts[i].IndexOf('=');
            if (eq <= 0)
                continue;
            string key = parts[i].Substring(0, eq);
            string val = parts[i].Substring(eq + 1);
            if (key.Equals("role", StringComparison.OrdinalIgnoreCase))
                role = val.Equals("spectator", StringComparison.OrdinalIgnoreCase) ? NetworkClientRole.Spectator : NetworkClientRole.Player;
            else if (key.Equals("passwordHash", StringComparison.OrdinalIgnoreCase))
                passwordHash = val;
        }
    }
}
