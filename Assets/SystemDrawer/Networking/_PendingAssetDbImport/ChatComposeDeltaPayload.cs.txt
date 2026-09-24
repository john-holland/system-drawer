using System;

[Serializable]
public sealed class ChatComposeDeltaPayload
{
    public string treeId;
    public string[] tokens;
    public string text;
    public bool committed;
}
