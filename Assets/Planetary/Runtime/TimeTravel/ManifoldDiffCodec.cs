using System;
using UnityEngine;
using Weather;

namespace Planetary.TimeTravel
{
    [Serializable]
    public sealed class ManifoldDiffEntry
    {
        public Vector3 position;
        public ManifoldCellData data;
    }

    [Serializable]
    public sealed class ManifoldDiffBundle
    {
        public ManifoldDiffEntry[] entries;
    }

    /// <summary>Encode/decode sparse manifold cell restores for time-travel frames.</summary>
    public static class ManifoldDiffCodec
    {
        public static byte[] Encode(ManifoldDiffBundle bundle) =>
            bundle != null ? System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(bundle)) : null;

        public static ManifoldDiffBundle Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<ManifoldDiffBundle>(json);
        }

        public static void ApplyToManifold(ManifoldDiffBundle bundle, WeatherPhysicsManifold manifold)
        {
            if (bundle?.entries == null || manifold == null)
                return;
            for (int i = 0; i < bundle.entries.Length; i++)
            {
                ManifoldDiffEntry e = bundle.entries[i];
                if (e == null)
                    continue;
                manifold.SetDataAtPosition(e.position, e.data);
            }
        }
    }
}
