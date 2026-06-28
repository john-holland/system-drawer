using System;
using UnityEngine;

namespace Weather.CloudBake
{
    [Serializable]
    public sealed class CloudGradientBands
    {
        public Color top = new Color(0.53f, 0.81f, 0.98f);
        public Color mid = new Color(0.75f, 0.85f, 0.95f);
        public Color bottom = new Color(0.55f, 0.55f, 0.6f);

        public static CloudGradientBands Parse(string gradient)
        {
            var bands = new CloudGradientBands();
            if (string.IsNullOrEmpty(gradient))
                return bands;

            foreach (var token in gradient.Split(' '))
            {
                var parts = token.Split('=');
                if (parts.Length != 2)
                    continue;
                if (!ColorUtility.TryParseHtmlString(parts[1], out var c))
                    continue;
                switch (parts[0].ToLowerInvariant())
                {
                    case "top": bands.top = c; break;
                    case "mid": bands.mid = c; break;
                    case "bottom": bands.bottom = c; break;
                }
            }
            return bands;
        }
    }

    [Serializable]
    public sealed class CloudPerspectiveTarget
    {
        public Texture2D referenceTexture;
        public string imagePath;
        public string videoPath;
        public int frameIndex;
        public CloudGradientBands gradientBands = new CloudGradientBands();
        public Color[] perRayColors;
        public int rayWidth;
        public int rayHeight;
        public int sampleStride = 4;
    }
}
