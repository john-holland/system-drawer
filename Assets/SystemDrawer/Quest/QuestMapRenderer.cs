using Locomotion.Narrative;
using UnityEngine;

namespace SystemDrawer.Quest
{
    /// <summary>
    /// Orthographic 2D map from SpatialGenerator4D slice occupancy + causal + emergence layers.
    /// </summary>
    public class QuestMapRenderer : MonoBehaviour
    {
        public QuestSpatialSliceSource sliceSource;
        public QuestMapProfile profile;
        public QuestRunner questRunner;
        public NarrativeClock narrativeClock;
        public RenderTexture outputTexture;

        Texture2D _cpuTexture;
        float _nextRefresh;

        void Awake()
        {
            if (profile == null)
                profile = ScriptableObject.CreateInstance<QuestMapProfile>();
            if (questRunner == null)
                questRunner = FindAnyObjectByType<QuestRunner>();
            if (narrativeClock == null)
                narrativeClock = FindAnyObjectByType<NarrativeClock>();
            EnsureTextures();
        }

        void OnEnable()
        {
            if (questRunner != null)
                questRunner.OnMapDirty += MarkDirty;
        }

        void OnDisable()
        {
            if (questRunner != null)
                questRunner.OnMapDirty -= MarkDirty;
        }

        void MarkDirty() => _nextRefresh = 0f;

        void Update()
        {
            if (profile == null || sliceSource == null)
                return;
            if (Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 1f / Mathf.Max(0.1f, profile.refreshHz);
            RenderSlice();
        }

        void EnsureTextures()
        {
            if (_cpuTexture == null || _cpuTexture.width != profile.textureWidth || _cpuTexture.height != profile.textureHeight)
            {
                _cpuTexture = new Texture2D(profile.textureWidth, profile.textureHeight, TextureFormat.RGBA32, false);
                _cpuTexture.filterMode = FilterMode.Point;
            }
            if (outputTexture == null ||
                outputTexture.width != profile.textureWidth ||
                outputTexture.height != profile.textureHeight)
            {
                outputTexture = new RenderTexture(profile.textureWidth, profile.textureHeight, 0, RenderTextureFormat.ARGB32);
                outputTexture.filterMode = FilterMode.Point;
            }
        }

        public void RenderSlice(float? narrativeTOverride = null)
        {
            if (sliceSource == null || profile == null)
                return;
            EnsureTextures();

            float t = narrativeTOverride ?? (narrativeClock != null ? narrativeClock.SimulationSeconds : 0f);
            if (!sliceSource.TryGetSliceAtT(t, out Bounds bounds, out int rx, out int ry, out int rz, out float[] occ, out float[] causal))
                return;

            int w = profile.textureWidth;
            int h = profile.textureHeight;
            var pixels = new Color32[w * h];
            var clear = new Color32(16, 20, 28, 255);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            ProjectOccupancy(bounds, rx, ry, rz, occ, causal, pixels, w, h);
            _cpuTexture.SetPixels32(pixels);
            _cpuTexture.Apply(false);
            Graphics.Blit(_cpuTexture, outputTexture);
        }

        void ProjectOccupancy(
            Bounds bounds,
            int rx, int ry, int rz,
            float[] occ,
            float[] causal,
            Color32[] pixels,
            int w,
            int h)
        {
            if (occ == null || rx <= 0 || ry <= 0 || rz <= 0)
                return;

            for (int iz = 0; iz < rz; iz++)
            {
                for (int iy = 0; iy < ry; iy++)
                {
                    for (int ix = 0; ix < rx; ix++)
                    {
                        int idx = ix + rx * (iy + ry * iz);
                        float o = occ[idx];
                        float c = causal[idx];
                        if (o <= 0f && c <= 0f)
                            continue;

                        int px, py;
                        switch (profile.projectionAxis)
                        {
                            case QuestMapProjectionAxis.XY:
                                px = Mathf.RoundToInt((ix / (float)rx) * (w - 1));
                                py = Mathf.RoundToInt((iy / (float)ry) * (h - 1));
                                break;
                            case QuestMapProjectionAxis.YZ:
                                px = Mathf.RoundToInt((iy / (float)ry) * (w - 1));
                                py = Mathf.RoundToInt((iz / (float)rz) * (h - 1));
                                break;
                            default:
                                px = Mathf.RoundToInt((ix / (float)rx) * (w - 1));
                                py = Mathf.RoundToInt((iz / (float)rz) * (h - 1));
                                break;
                        }

                        int pi = py * w + px;
                        if (pi < 0 || pi >= pixels.Length)
                            continue;

                        Color col = profile.occupancyColor * Mathf.Clamp01(o);
                        if (c > 0f)
                            col = Color.Lerp(col, profile.causalColor, Mathf.Clamp01(c));
                        pixels[pi] = col;
                    }
                }
            }
        }

        public Texture GetDisplayTexture() => outputTexture != null ? outputTexture : _cpuTexture;
    }
}
