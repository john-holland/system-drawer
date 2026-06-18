using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

/// <summary>TCP client for lobby QUERY / REGISTER protocol v2.</summary>
public static class LobbyClientQuery
{
    public static LobbySessionInfo Query(string host, int lobbyPort, int timeoutMs = 3000)
    {
        var info = new LobbySessionInfo();
        try
        {
            using var client = new TcpClient();
            client.ReceiveTimeout = timeoutMs;
            client.SendTimeout = timeoutMs;
            client.Connect(host, lobbyPort);
            var stream = client.GetStream();
            WriteLine(stream, "QUERY");
            string line = ReadLine(stream);
            ParseOkLine(line, info);
        }
        catch (Exception ex)
        {
            info.Ok = false;
            info.Error = ex.Message;
        }
        return info;
    }

    public static bool Register(string host, int lobbyPort, NetworkClientRole role, string name, string password, out LobbySessionInfo info, int timeoutMs = 3000)
    {
        info = new LobbySessionInfo();
        try
        {
            using var client = new TcpClient();
            client.ReceiveTimeout = timeoutMs;
            client.SendTimeout = timeoutMs;
            client.Connect(host, lobbyPort);
            var stream = client.GetStream();
            string roleStr = role == NetworkClientRole.Spectator ? "spectator" : "player";
            var parts = new List<string> { "REGISTER", "role=" + roleStr };
            if (!string.IsNullOrEmpty(name))
                parts.Add("name=" + name);
            if (!string.IsNullOrEmpty(password))
                parts.Add("password=" + password);
            WriteLine(stream, string.Join(" ", parts));
            string line = ReadLine(stream);
            if (line != null && line.StartsWith("ERR password", StringComparison.OrdinalIgnoreCase))
            {
                info.Ok = false;
                info.Error = "password";
                return false;
            }
            ParseOkLine(line, info);
            return info.Ok;
        }
        catch (Exception ex)
        {
            info.Ok = false;
            info.Error = ex.Message;
            return false;
        }
    }

    static void ParseOkLine(string line, LobbySessionInfo info)
    {
        if (string.IsNullOrEmpty(line) || !line.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
        {
            info.Ok = false;
            info.Error = line ?? "empty response";
            return;
        }
        info.Ok = true;
        var kv = ParseKeyValues(line);
        if (kv.TryGetValue("session", out string session))
            info.sessionName = session;
        if (kv.TryGetValue("port", out string portStr) && int.TryParse(portStr, out int port))
            info.gamePort = port;
        if (kv.TryGetValue("players", out string players))
            ParseCountPair(players, out info.playerCount, out info.maxPlayers);
        if (kv.TryGetValue("spectators", out string spectators))
            ParseCountPair(spectators, out info.spectatorCount, out info.maxSpectators);
        if (kv.TryGetValue("allowSpectators", out string allowSpec))
            info.allowSpectators = allowSpec == "1";
        if (kv.TryGetValue("passwordRequired", out string pwReq))
            info.passwordRequired = pwReq == "1";
    }

    static Dictionary<string, string> ParseKeyValues(string line)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int idx = line.IndexOf(' ');
        if (idx < 0)
            return dict;
        string rest = line.Substring(idx + 1);
        string[] segments = rest.Split(';');
        for (int i = 0; i < segments.Length; i++)
        {
            int eq = segments[i].IndexOf('=');
            if (eq <= 0)
                continue;
            dict[segments[i].Substring(0, eq).Trim()] = segments[i].Substring(eq + 1).Trim();
        }
        return dict;
    }

    static void ParseCountPair(string value, out int count, out int max)
    {
        count = 0;
        max = 0;
        if (string.IsNullOrEmpty(value))
            return;
        int slash = value.IndexOf('/');
        if (slash >= 0)
        {
            int.TryParse(value.Substring(0, slash), out count);
            int.TryParse(value.Substring(slash + 1), out max);
        }
        else
            int.TryParse(value, out count);
    }

    static void WriteLine(NetworkStream stream, string line)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
        stream.Write(bytes, 0, bytes.Length);
    }

    static string ReadLine(NetworkStream stream)
    {
        var sb = new StringBuilder();
        int b;
        while ((b = stream.ReadByte()) >= 0)
        {
            if (b == '\n')
                break;
            sb.Append((char)b);
        }
        return sb.ToString().Trim();
    }
}
