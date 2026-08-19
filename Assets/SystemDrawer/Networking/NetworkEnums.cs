/// <summary>Networking model enums for System Drawer tree ownership and transport.</summary>
public enum NetworkServerMode
{
    SinglePlayer,
    AuthoritativePeerToPeer,
    ClassicLockstep
}

public enum NetworkClientRole
{
    Player,
    Spectator
}

public enum TreeDimension
{
    Spatial2D,
    Spatial3D,
    Spatial4D
}

public enum TreeTransmitPolicy
{
    LocalOnly,
    ServerAuthoritative,
    PeerTransferable,
    SpectatorReadOnly
}

public enum NetworkConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Loopback
}

public enum MenuServerModeMask
{
    All = -1,
    SinglePlayer = 1,
    Multiplayer = 2
}

public enum MenuClientRoleMask
{
    All = -1,
    PlayerOnly = 1,
    SpectatorOnly = 2
}
