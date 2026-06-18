using System;
using UnityEngine;

/// <summary>Loopback impersonation token for headed server debugging.</summary>
public sealed class ImpersonationSession
{
    public string Token { get; }
    public string ClientId { get; }
    public string Host { get; }
    public int Port { get; }

    public ImpersonationSession(string clientId, string host, int port)
    {
        ClientId = clientId ?? "";
        Host = host ?? "127.0.0.1";
        Port = port;
        Token = Guid.NewGuid().ToString("N");
    }
}
