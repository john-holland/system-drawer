using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>Optional lobby registration/list TCP service for session discovery (protocol v2).</summary>
public sealed class LobbyServerHost : IDisposable
{
    TcpListener _listener;
    Thread _thread;
    volatile bool _running;
    readonly List<string> _pendingPlayers = new List<string>();
    readonly List<string> _pendingSpectators = new List<string>();
    string _sessionName = "Drawer 2";
    int _gamePort = 7777;
    int _maxPlayers = 8;
    int _maxSpectators = 4;
    bool _allowSpectators = true;
    string _passwordHash = "";

    public bool IsRunning => _running;
    public string SessionName => _sessionName;
    public int GamePort => _gamePort;
    public int PendingPlayerCount => _pendingPlayers.Count;
    public int PendingSpectatorCount => _pendingSpectators.Count;
    public int PendingClientCount => PendingPlayerCount + PendingSpectatorCount;
    public bool PasswordRequired => !string.IsNullOrEmpty(_passwordHash);

    public void CopyPendingPlayerIds(List<string> dest)
    {
        if (dest == null) return;
        dest.Clear();
        lock (_pendingPlayers)
        {
            for (int i = 0; i < _pendingPlayers.Count; i++)
                dest.Add(_pendingPlayers[i]);
        }
    }
    public string AdvertiseAddress { get; private set; } = "127.0.0.1";

    public void Start(string bindAddress, int lobbyPort, int gamePort, string sessionName,
        int maxPlayers = 8, int maxSpectators = 4, bool allowSpectators = true, string passwordHash = null)
    {
        Stop();
        _gamePort = gamePort;
        _sessionName = string.IsNullOrEmpty(sessionName) ? "Drawer 2" : sessionName;
        _maxPlayers = Math.Max(1, maxPlayers);
        _maxSpectators = Math.Max(0, maxSpectators);
        _allowSpectators = allowSpectators;
        _passwordHash = passwordHash ?? "";
        _running = true;
        var ip = bindAddress == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(bindAddress);
        _listener = new TcpListener(ip, lobbyPort);
        _listener.Start();
        AdvertiseAddress = bindAddress == "0.0.0.0" ? "127.0.0.1" : bindAddress;
        _thread = new Thread(ListenLoop) { IsBackground = true, Name = "LobbyServerHost" };
        _thread.Start();
    }

    void ListenLoop()
    {
        while (_running && _listener != null)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    void HandleClient(object state)
    {
        if (state is not TcpClient client)
            return;
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var req = ReadLine(stream);
                string response;
                if (req != null && req.StartsWith("REGISTER", StringComparison.OrdinalIgnoreCase))
                    response = HandleRegister(req, client);
                else if (req != null && req.StartsWith("QUERY", StringComparison.OrdinalIgnoreCase))
                    response = BuildQueryResponse();
                else
                    response = "ERR unknown";
                byte[] bytes = Encoding.UTF8.GetBytes(response + "\n");
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LobbyServerHost] Client error: " + ex.Message);
            }
        }
    }

    string HandleRegister(string req, TcpClient client)
    {
        var kv = ParseRequestArgs(req);
        string role = kv.TryGetValue("role", out string r) ? r.ToLowerInvariant() : "player";
        bool isSpectator = role == "spectator";
        if (isSpectator && !_allowSpectators)
            return "ERR role";
        kv.TryGetValue("password", out string password);
        if (PasswordRequired && !LobbyPasswordHash.Verify(password, _sessionName, _passwordHash))
            return "ERR password";
        string endpoint = client.Client.RemoteEndPoint?.ToString() ?? "client";
        lock (_pendingPlayers)
        {
            if (isSpectator)
            {
                if (_pendingSpectators.Count >= _maxSpectators)
                    return "ERR full";
                _pendingSpectators.Add(endpoint);
            }
            else
            {
                if (_pendingPlayers.Count >= _maxPlayers)
                    return "ERR full";
                _pendingPlayers.Add(endpoint);
            }
        }
        return BuildQueryResponse();
    }

    string BuildQueryResponse()
    {
        lock (_pendingPlayers)
        {
            return "OK session=" + _sessionName +
                   ";port=" + _gamePort +
                   ";players=" + _pendingPlayers.Count + "/" + _maxPlayers +
                   ";spectators=" + _pendingSpectators.Count + "/" + _maxSpectators +
                   ";allowSpectators=" + (_allowSpectators ? "1" : "0") +
                   ";passwordRequired=" + (PasswordRequired ? "1" : "0");
        }
    }

    static Dictionary<string, string> ParseRequestArgs(string req)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] parts = req.Split(' ');
        for (int i = 1; i < parts.Length; i++)
        {
            int eq = parts[i].IndexOf('=');
            if (eq <= 0)
                continue;
            dict[parts[i].Substring(0, eq)] = parts[i].Substring(eq + 1);
        }
        return dict;
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

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        _listener = null;
        lock (_pendingPlayers)
        {
            _pendingPlayers.Clear();
            _pendingSpectators.Clear();
        }
        var worker = _thread;
        _thread = null;
        if (worker != null && worker.IsAlive && worker != Thread.CurrentThread)
            worker.Join(250);
    }

    public void Dispose() => Stop();
}
