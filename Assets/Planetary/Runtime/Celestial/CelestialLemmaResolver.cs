using System;
using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Resolves observer-relative celestial appearance from lemma hints and geometry.</summary>
    public static class CelestialLemmaResolver
    {
        public struct LemmaHint
        {
            public string observerBodyId;
            public string targetBodyId;
            public string tintKeyword;
            public bool mutualGaze;
        }

        public static bool TryParseLemma(string lemma, out LemmaHint hint)
        {
            hint = default;
            if (string.IsNullOrEmpty(lemma) || !lemma.Contains("celestial"))
                return false;
            hint.observerBodyId = ExtractParam(lemma, "observer");
            hint.targetBodyId = ExtractParam(lemma, "target");
            hint.tintKeyword = ExtractParam(lemma, "tint");
            hint.mutualGaze = ExtractParam(lemma, "gaze") == "mutual";
            return !string.IsNullOrEmpty(hint.targetBodyId);
        }

        static string ExtractParam(string lemma, string key)
        {
            string needle = key + "=";
            int i = lemma.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                return null;
            int start = i + needle.Length;
            int end = lemma.IndexOf('|', start);
            if (end < 0)
                end = lemma.IndexOf('}', start);
            if (end < 0)
                end = lemma.Length;
            return lemma.Substring(start, end - start).Trim().Trim('"');
        }

        public static CelestialAppearance Resolve(
            LemmaHint hint,
            Vector3 observerWorld,
            Vector3 observerForward,
            Vector3 targetWorld,
            Vector3 targetForward,
            float targetRadiusM)
        {
            var app = CelestialAppearance.Default;
            Vector3 toTarget = targetWorld - observerWorld;
            float dist = toTarget.magnitude;
            if (dist < 1e-3f)
                return app;
            Vector3 dir = toTarget / dist;
            float angularSize = Mathf.Atan2(targetRadiusM, dist) * Mathf.Rad2Deg;
            app.visible = angularSize > 0.01f;
            app.intensity = Mathf.Clamp01(1f / (1f + dist * 1e-9f));

            if (!string.IsNullOrEmpty(hint.tintKeyword))
            {
                app.tint = hint.tintKeyword.ToLowerInvariant() switch
                {
                    "strange" => new Color(0.85f, 0.7f, 1.1f),
                    "red" => new Color(1.2f, 0.6f, 0.5f),
                    "pale" => new Color(0.9f, 0.95f, 1f),
                    _ => Color.white
                };
            }

            if (hint.mutualGaze)
            {
                float observerLooks = Vector3.Dot(observerForward.normalized, dir);
                float targetLooks = Vector3.Dot(targetForward.normalized, -dir);
                app.stareBackWeight = Mathf.Clamp01(observerLooks * targetLooks);
            }

            return app;
        }
    }
}
