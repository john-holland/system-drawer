using System;
using UnityEngine;

namespace SystemDrawer.DreamCycle
{
    /// <summary>Renders sleep wave IO as 2D texture (electrical sheep storm → smooth REM/deep).</summary>
    public sealed class SleepWaveStatRenderer : MonoBehaviour
    {
        public int textureWidth = 256;
        public int textureHeight = 64;
        public Renderer targetRenderer;
        public float[] waveSamples = Array.Empty<float>();
        public Color stormColor = new Color(1f, 0.4f, 0.9f);
        public Color smoothColor = new Color(0.3f, 0.6f, 1f);

        Texture2D _tex;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            EnsureTexture();
        }

        public void SetWaveSamples(float[] samples)
        {
            waveSamples = samples ?? Array.Empty<float>();
            RenderWave();
        }

        public void EnsureTexture()
        {
            int w = Mathf.Max(1, textureWidth);
            int h = Mathf.Max(1, textureHeight);
            if (_tex != null && (_tex.width != w || _tex.height != h))
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_tex);
                else
                    UnityEngine.Object.DestroyImmediate(_tex);
                _tex = null;
            }
            if (_tex == null)
            {
                _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }
        }

        public void RenderWave()
        {
            EnsureTexture();
            if (waveSamples.Length == 0 || _tex == null)
                return;
            int w = _tex.width;
            int h = _tex.height;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float rowT = h <= 1 ? 0f : y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float t = w <= 1 ? 0f : x / (float)(w - 1);
                    int idx = Mathf.Clamp(Mathf.RoundToInt(t * (waveSamples.Length - 1)), 0, waveSamples.Length - 1);
                    float v = waveSamples[idx];
                    float stormWeight = 1f - rowT;
                    Color c = Color.Lerp(smoothColor, stormColor, stormWeight);
                    float alpha = Mathf.Clamp01(Mathf.Abs(v));
                    pixels[y * w + x] = new Color(c.r * alpha, c.g * alpha, c.b * alpha, alpha);
                }
            }
            _tex.SetPixels32(pixels);
            _tex.Apply();
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_mpb);
                _mpb.SetTexture("_MainTex", _tex);
                targetRenderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
