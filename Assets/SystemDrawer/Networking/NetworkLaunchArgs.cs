using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Parses Unity command-line args for dedicated server and lobby hosting.</summary>
public static class NetworkLaunchArgs
{
    public const string FlagDedicatedServer = "--dedicated-server";
    public const string FlagDedicatedServerShort = "-ds";
    public const string FlagListenPort = "--listen-port";
    public const string FlagListenPortShort = "-p";
    public const string FlagMode = "--mode";
    public const string FlagModeShort = "-m";
    public const string FlagHostLobby = "--host-lobby";
    public const string FlagLobbyPort = "--lobby-port";
    public const string FlagLobbyName = "--lobby-name";
    public const string FlagNoLobby = "--no-lobby";
    public const string FlagBindAddress = "--bind-address";
    public const string FlagLobbyPassword = "--lobby-password";

    public static bool DedicatedServer { get; private set; }
    public static int ListenPort { get; private set; } = 7777;
    public static NetworkServerMode Mode { get; private set; } = NetworkServerMode.SinglePlayer;
    public static bool HostLobby { get; private set; }
    public static int LobbyPort { get; private set; } = 7780;
    public static string LobbyName { get; private set; } = "Drawer 2";
    public static string LobbyPassword { get; private set; } = "";
    public static bool NoLobby { get; private set; }
    public static string BindAddress { get; private set; } = "0.0.0.0";
    public static bool Parsed { get; private set; }

    public static void Parse(string[] args = null)
    {
        if (Parsed)
            return;
        args ??= Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == FlagDedicatedServer || a == FlagDedicatedServerShort)
                DedicatedServer = true;
            else if ((a == FlagListenPort || a == FlagListenPortShort) && i + 1 < args.Length && int.TryParse(args[++i], out int p))
                ListenPort = p;
            else if ((a == FlagMode || a == FlagModeShort) && i + 1 < args.Length)
                Mode = ParseMode(args[++i]);
            else if (a == FlagHostLobby)
                HostLobby = true;
            else if (a == FlagLobbyPort && i + 1 < args.Length && int.TryParse(args[++i], out int lp))
                LobbyPort = lp;
            else if (a == FlagLobbyName && i + 1 < args.Length)
                LobbyName = args[++i];
            else if (a == FlagNoLobby)
                NoLobby = true;
            else if (a == FlagBindAddress && i + 1 < args.Length)
                BindAddress = args[++i];
            else if (a == FlagLobbyPassword && i + 1 < args.Length)
                LobbyPassword = args[++i];
        }
        Parsed = true;
    }

    public static void ApplyTo(ServerOrchestrator server)
    {
        if (server == null)
            return;
        Parse();
        if (NoLobby)
            server.SetLobbyLockedByLaunchArgs(true);
        if (!DedicatedServer && !HostLobby)
            return;
        server.ConfigureDedicated(BindAddress, ListenPort, Mode, NoLobby);
        if (HostLobby && !NoLobby)
        {
            var opts = new LobbyHostOptions
            {
                sessionName = LobbyName,
                lobbyPort = LobbyPort,
                password = LobbyPassword
            };
            server.StartLobbyHost(opts);
        }
    }

    static NetworkServerMode ParseMode(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return NetworkServerMode.SinglePlayer;
        switch (raw.Trim().ToLowerInvariant())
        {
            case "p2p":
            case "authoritative":
                return NetworkServerMode.AuthoritativePeerToPeer;
            case "lockstep":
                return NetworkServerMode.ClassicLockstep;
            default:
                return NetworkServerMode.SinglePlayer;
        }
    }

    public static void ResetForTests()
    {
        DedicatedServer = false;
        ListenPort = 7777;
        Mode = NetworkServerMode.SinglePlayer;
        HostLobby = false;
        LobbyPort = 7780;
        LobbyName = "Drawer 2";
        LobbyPassword = "";
        NoLobby = false;
        BindAddress = "0.0.0.0";
        Parsed = false;
    }
}
