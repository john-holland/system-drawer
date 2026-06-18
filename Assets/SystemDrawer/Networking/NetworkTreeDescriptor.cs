using System;

/// <summary>Metadata for a registered spatial / causality tree.</summary>
[Serializable]
public sealed class NetworkTreeDescriptor
{
    public string TreeId = "";
    public TreeDimension Dimension = TreeDimension.Spatial3D;
    public TreeTransmitPolicy TransmitPolicy = TreeTransmitPolicy.LocalOnly;
    public string OwnerClientId = "";
    public string CausalityLeafPrefix = "";
    public bool StreamForOwnership;
    public UnityEngine.Object Source;

    public NetworkTreeDescriptor Clone()
    {
        return new NetworkTreeDescriptor
        {
            TreeId = TreeId,
            Dimension = Dimension,
            TransmitPolicy = TransmitPolicy,
            OwnerClientId = OwnerClientId,
            CausalityLeafPrefix = CausalityLeafPrefix,
            StreamForOwnership = StreamForOwnership,
            Source = Source
        };
    }
}
