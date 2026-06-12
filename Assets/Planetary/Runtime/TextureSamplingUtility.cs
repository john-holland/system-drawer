using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    public static class TextureSamplingUtility
    {
        struct CacheEntry
        {
            public Texture2D Copy;
            public int Width;
            public int Height;
        }

        static readonly Dictionary<int, CacheEntry> ReadableCache = new Dictionary<int, CacheEntry>();

        public static Color SampleBilinear(Texture2D texture, float u, float v)
        {
            if (texture == null)
                return Color.black;
            if (texture.isReadable)
                return texture.GetPixelBilinear(u, v);
            return GetReadableCopy(texture).GetPixelBilinear(u, v);
        }

        public static float SampleRedBilinear(Texture2D texture, float u, float v)
        {
            return SampleBilinear(texture, u, v).r;
        }

        public static float SampleAlphaBilinear(Texture2D texture, float u, float v)
        {
            return SampleBilinear(texture, u, v).a;
        }

        static Texture2D GetReadableCopy(Texture2D source)
        {
            int id = source.GetInstanceID();
            if (ReadableCache.TryGetValue(id, out var entry)
                && entry.Copy != null
                && entry.Width == source.width
                && entry.Height == source.height)
                return entry.Copy;

            if (entry.Copy != null)
                Object.DestroyImmediate(entry.Copy);

            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            ReadableCache[id] = new CacheEntry
            {
                Copy = readable,
                Width = source.width,
                Height = source.height
            };
            return readable;
        }
    }
}
