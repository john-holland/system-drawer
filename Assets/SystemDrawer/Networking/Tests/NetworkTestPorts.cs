#if UNITY_INCLUDE_TESTS
using System.Net;
using System.Net.Sockets;
using UnityEngine;

static class NetworkTestPorts
{
    public static int Allocate(int extraConsecutive = 0)
    {
        for (int attempt = 0; attempt < 32; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            bool ok = true;
            for (int i = 1; i <= extraConsecutive; i++)
            {
                if (!IsFree(port + i))
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
                return port;
        }
        throw new SocketException();
    }

    public static bool IsFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static void DestroyNetworkObjects()
    {
        foreach (var client in Object.FindObjectsByType<ClientOrchestrator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(client.gameObject);
        foreach (var server in Object.FindObjectsByType<ServerOrchestrator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(server.gameObject);
        foreach (var menu in Object.FindObjectsByType<MenuRagdoll>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(menu.gameObject);
    }
}
#endif
