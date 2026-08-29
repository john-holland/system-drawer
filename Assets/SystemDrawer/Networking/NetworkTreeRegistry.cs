using System.Collections.Generic;

/// <summary>Maps tree id → descriptor with ownership and transmit policy.</summary>
public sealed class NetworkTreeRegistry
{
    readonly Dictionary<string, NetworkTreeDescriptor> _trees = new Dictionary<string, NetworkTreeDescriptor>();

    public IReadOnlyDictionary<string, NetworkTreeDescriptor> Trees => _trees;

    public void Register(NetworkTreeDescriptor descriptor)
    {
        if (descriptor == null || string.IsNullOrEmpty(descriptor.TreeId))
            return;
        _trees[descriptor.TreeId] = descriptor.Clone();
    }

    public bool TryGet(string treeId, out NetworkTreeDescriptor descriptor)
    {
        if (_trees.TryGetValue(treeId, out var d))
        {
            descriptor = d.Clone();
            return true;
        }
        descriptor = null;
        return false;
    }

    public bool TransferOwnership(string treeId, string newOwnerClientId)
    {
        if (!_trees.TryGetValue(treeId, out var d))
            return false;
        if (d.TransmitPolicy != TreeTransmitPolicy.PeerTransferable)
            return false;
        d.OwnerClientId = newOwnerClientId ?? "";
        _trees[treeId] = d;
        return true;
    }

    public void Remove(string treeId)
    {
        if (!string.IsNullOrEmpty(treeId))
            _trees.Remove(treeId);
    }

    public void Clear() => _trees.Clear();

    public int ComputeStateHash()
    {
        unchecked
        {
            int hash = 17;
            foreach (var pair in _trees)
            {
                hash = hash * 31 + pair.Key.GetHashCode();
                hash = hash * 31 + (int)pair.Value.Dimension;
                hash = hash * 31 + (int)pair.Value.TransmitPolicy;
                hash = hash * 31 + (pair.Value.OwnerClientId ?? "").GetHashCode();
                hash = hash * 31 + (pair.Value.CausalityLeafPrefix ?? "").GetHashCode();
                hash = hash * 31 + (pair.Value.GameSessionId ?? "").GetHashCode();
            }
            return hash;
        }
    }
}
