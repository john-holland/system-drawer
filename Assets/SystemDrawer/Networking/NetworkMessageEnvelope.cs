using System;
using System.Text;

/// <summary>Length-prefixed JSON envelope for TCP/UDP channels.</summary>
[Serializable]
public sealed class NetworkMessageEnvelope
{
    public string Channel = "";
    public string Type = "";
    public string PayloadJson = "";

    public static NetworkMessageEnvelope Create(string channel, string type, string payloadJson = "")
    {
        return new NetworkMessageEnvelope
        {
            Channel = channel ?? "",
            Type = type ?? "",
            PayloadJson = payloadJson ?? ""
        };
    }

    public byte[] Serialize()
    {
        string json = UnityEngine.JsonUtility.ToJson(this);
        byte[] body = Encoding.UTF8.GetBytes(json);
        byte[] frame = new byte[4 + body.Length];
        frame[0] = (byte)((body.Length >> 24) & 0xFF);
        frame[1] = (byte)((body.Length >> 16) & 0xFF);
        frame[2] = (byte)((body.Length >> 8) & 0xFF);
        frame[3] = (byte)(body.Length & 0xFF);
        Buffer.BlockCopy(body, 0, frame, 4, body.Length);
        return frame;
    }

    public static bool TryDeserialize(byte[] buffer, int offset, int count, out NetworkMessageEnvelope envelope)
    {
        envelope = null;
        if (buffer == null || count <= 0)
            return false;
        try
        {
            string json = Encoding.UTF8.GetString(buffer, offset, count);
            envelope = UnityEngine.JsonUtility.FromJson<NetworkMessageEnvelope>(json);
            return envelope != null;
        }
        catch
        {
            return false;
        }
    }
}
