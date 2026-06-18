using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>UDP decision channel with simple ack handshake.</summary>
public sealed class UdpDecisionChannel : IDisposable
{
    UdpClient _client;
    IPEndPoint _remote;
    Thread _recvThread;
    volatile bool _running;
    readonly HashSet<int> _pendingAcks = new HashSet<int>();
    int _nextSeq;

    public event Action<NetworkMessageEnvelope> MessageReceived;
    public event Action<int> AckReceived;

    public void Bind(string bindAddress, int port)
    {
        Stop();
        _client = new UdpClient(new IPEndPoint(IPAddress.Parse(NormalizeBind(bindAddress)), port));
        _running = true;
        _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "UdpDecisionRecv" };
        _recvThread.Start();
    }

    public void Connect(string host, int port)
    {
        Stop();
        _client = new UdpClient();
        _remote = new IPEndPoint(IPAddress.Parse(host), port);
        _running = true;
        _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "UdpDecisionRecv" };
        _recvThread.Start();
    }

    public int SendDecision(NetworkMessageEnvelope envelope)
    {
        if (_client == null || envelope == null)
            return -1;
        int seq = ++_nextSeq;
        envelope.Type = "decision:" + seq;
        SendRaw(envelope);
        lock (_pendingAcks)
            _pendingAcks.Add(seq);
        return seq;
    }

    public void SendAck(int seq)
    {
        SendRaw(NetworkMessageEnvelope.Create("DecisionChannel", "ack", seq.ToString()));
    }

    void SendRaw(NetworkMessageEnvelope envelope)
    {
        try
        {
            byte[] data = envelope.Serialize();
            if (_remote != null)
                _client.Send(data, data.Length, _remote);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UdpDecisionChannel] Send failed: " + ex.Message);
        }
    }

    void RecvLoop()
    {
        var any = new IPEndPoint(IPAddress.Any, 0);
        while (_running && _client != null)
        {
            try
            {
                byte[] data = _client.Receive(ref any);
                if (_remote == null)
                    _remote = any;
                if (data == null || data.Length < 4)
                    continue;
                int len = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
                if (data.Length < 4 + len)
                    continue;
                if (!NetworkMessageEnvelope.TryDeserialize(data, 4, len, out var env))
                    continue;
                if (env.Type != null && env.Type.StartsWith("ack", StringComparison.Ordinal))
                {
                    if (int.TryParse(env.PayloadJson, out int ackSeq))
                    {
                        lock (_pendingAcks)
                            _pendingAcks.Remove(ackSeq);
                        AckReceived?.Invoke(ackSeq);
                    }
                    continue;
                }
                MessageReceived?.Invoke(env);
                if (env.Type != null && env.Type.StartsWith("decision:", StringComparison.Ordinal))
                {
                    var parts = env.Type.Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int seq))
                        SendAck(seq);
                }
            }
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    public bool IsAckPending(int seq)
    {
        lock (_pendingAcks)
            return _pendingAcks.Contains(seq);
    }

    static string NormalizeBind(string bindAddress)
    {
        if (string.IsNullOrEmpty(bindAddress) || bindAddress == "0.0.0.0")
            return IPAddress.Any.ToString();
        return bindAddress;
    }

    public void Stop()
    {
        _running = false;
        try { _client?.Close(); } catch { }
        _client = null;
        _remote = null;
    }

    public void Dispose() => Stop();
}
