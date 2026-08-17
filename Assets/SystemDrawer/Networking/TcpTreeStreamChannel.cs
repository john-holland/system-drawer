using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>Reliable ordered tree stream channel (TCP).</summary>
public sealed class TcpTreeStreamChannel : IDisposable
{
    readonly List<byte> _rxBuffer = new List<byte>(4096);
    TcpListener _listener;
    TcpClient _client;
    NetworkStream _stream;
    Thread _acceptThread;
    volatile bool _running;

    public event Action<NetworkMessageEnvelope> MessageReceived;
    public bool IsConnected => _stream != null && _client != null && _client.Connected;

    public void Listen(string bindAddress, int port)
    {
        Stop();
        _running = true;
        _listener = new TcpListener(IPAddress.Parse(NormalizeBind(bindAddress)), port);
        _listener.Start();
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "TcpTreeStreamAccept" };
        _acceptThread.Start();
    }

    public void Connect(string host, int port)
    {
        StopClientOnly();
        _client = new TcpClient();
        _client.Connect(host, port);
        _stream = _client.GetStream();
        _running = true;
        var t = new Thread(ReadLoop) { IsBackground = true, Name = "TcpTreeStreamRead" };
        t.Start();
    }

    public void Send(NetworkMessageEnvelope envelope)
    {
        if (_stream == null || envelope == null)
            return;
        try
        {
            byte[] frame = envelope.Serialize();
            _stream.Write(frame, 0, frame.Length);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[TcpTreeStreamChannel] Send failed: " + ex.Message);
        }
    }

    void AcceptLoop()
    {
        while (_running && _listener != null)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                StopClientOnly();
                _client = client;
                _stream = client.GetStream();
                ReadLoop();
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
            catch (Exception ex)
            {
                if (_running)
                    Debug.LogWarning("[TcpTreeStreamChannel] Accept failed: " + ex.Message);
            }
        }
    }

    void ReadLoop()
    {
        var buf = new byte[8192];
        while (_running && _stream != null)
        {
            int read;
            try
            {
                read = _stream.Read(buf, 0, buf.Length);
            }
            catch
            {
                break;
            }
            if (read <= 0)
                break;
            lock (_rxBuffer)
            {
                for (int i = 0; i < read; i++)
                    _rxBuffer.Add(buf[i]);
                DrainFramesLocked();
            }
        }
    }

    void DrainFramesLocked()
    {
        while (_rxBuffer.Count >= 4)
        {
            int len = (_rxBuffer[0] << 24) | (_rxBuffer[1] << 16) | (_rxBuffer[2] << 8) | _rxBuffer[3];
            if (len <= 0 || len > 1024 * 1024)
            {
                _rxBuffer.Clear();
                return;
            }
            if (_rxBuffer.Count < 4 + len)
                return;
            var body = new byte[len];
            _rxBuffer.CopyTo(4, body, 0, len);
            _rxBuffer.RemoveRange(0, 4 + len);
            if (NetworkMessageEnvelope.TryDeserialize(body, 0, len, out var env))
                MessageReceived?.Invoke(env);
        }
    }

    static string NormalizeBind(string bindAddress)
    {
        if (string.IsNullOrEmpty(bindAddress) || bindAddress == "0.0.0.0")
            return IPAddress.Any.ToString();
        return bindAddress;
    }

    void StopClientOnly()
    {
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _stream = null;
        _client = null;
        lock (_rxBuffer)
            _rxBuffer.Clear();
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        _listener = null;
        StopClientOnly();
        var accept = _acceptThread;
        _acceptThread = null;
        if (accept != null && accept.IsAlive && accept != Thread.CurrentThread)
            accept.Join(250);
    }

    public void Dispose() => Stop();
}

static class ListByteExtensions
{
    public static void CopyTo(this List<byte> src, int srcOffset, byte[] dst, int dstOffset, int count)
    {
        for (int i = 0; i < count; i++)
            dst[dstOffset + i] = src[srcOffset + i];
    }
}
