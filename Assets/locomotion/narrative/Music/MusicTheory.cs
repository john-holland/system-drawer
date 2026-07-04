using System;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>Krumhansl-style major key profile helpers for transition scoring.</summary>
    public static class MusicTheory
    {
        static readonly float[] MajorProfile =
        {
            6.35f, 2.23f, 3.48f, 2.33f, 4.38f, 4.09f, 2.52f, 5.19f, 2.39f, 3.66f, 2.29f, 2.88f
        };

        public static int NormalizeTonic(int tonic) => ((tonic % 12) + 12) % 12;

        public static bool IsFifthStep(int fromTonic, int toTonic)
        {
            int a = NormalizeTonic(fromTonic);
            int b = NormalizeTonic(toTonic);
            int diff = (b - a + 12) % 12;
            return diff == 7 || diff == 5;
        }

        public static float TonalDistance(int tonicA, int tonicB)
        {
            int a = NormalizeTonic(tonicA);
            int b = NormalizeTonic(tonicB);
            if (a == b) return 0f;

            float[] profileA = RotateProfile(MajorProfile, a);
            float[] profileB = RotateProfile(MajorProfile, b);
            float corr = PearsonCorrelation(profileA, profileB);
            return Mathf.Clamp01(1f - corr);
        }

        public static int CommonToneCount(int tonicA, int tonicB, bool major = true)
        {
            int a = NormalizeTonic(tonicA);
            int b = NormalizeTonic(tonicB);
            int[] scaleA = major ? MajorScaleOffsets : MinorScaleOffsets;
            int[] scaleB = major ? MajorScaleOffsets : MinorScaleOffsets;
            int count = 0;
            for (int i = 0; i < scaleA.Length; i++)
            {
                int pcA = NormalizeTonic(a + scaleA[i]);
                for (int j = 0; j < scaleB.Length; j++)
                {
                    if (pcA == NormalizeTonic(b + scaleB[j]))
                        count++;
                }
            }
            return count;
        }

        public static float VoiceLeadingPenalty(int rootA, int rootB)
        {
            int a = NormalizeTonic(rootA);
            int b = NormalizeTonic(rootB);
            int diff = Mathf.Abs(b - a);
            diff = Mathf.Min(diff, 12 - diff);
            return diff / 6f;
        }

        public static float RhythmMismatch(RhythmMeterTemplate a, RhythmMeterTemplate b)
        {
            if (a == null || b == null) return 0.5f;
            float beatDiff = Mathf.Abs(a.beatsPerBar - b.beatsPerBar) / 8f;
            float stressDiff = Mathf.Abs(a.stressPattern - b.stressPattern);
            float swingDiff = Mathf.Abs(a.swingAmount - b.swingAmount);
            return Mathf.Clamp01(beatDiff * 0.4f + stressDiff * 0.35f + swingDiff * 0.25f);
        }

        public static int TonicFromKeyName(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return 0;
            string k = keyName.Trim().ToUpperInvariant();
            if (k.StartsWith("C")) return 0;
            if (k.StartsWith("D")) return k.Contains("B") ? 1 : 2;
            if (k.StartsWith("E")) return 4;
            if (k.StartsWith("F")) return k.Contains("B") ? 4 : 5;
            if (k.StartsWith("G")) return k.Contains("B") ? 6 : 7;
            if (k.StartsWith("A")) return k.Contains("B") ? 8 : 9;
            if (k.StartsWith("B")) return k.Contains("B") ? 10 : 11;
            return 0;
        }

        static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly int[] MinorScaleOffsets = { 0, 2, 3, 5, 7, 8, 10 };

        static float[] RotateProfile(float[] profile, int tonic)
        {
            var rotated = new float[12];
            for (int i = 0; i < 12; i++)
                rotated[i] = profile[(i - tonic + 12) % 12];
            return rotated;
        }

        static float PearsonCorrelation(float[] a, float[] b)
        {
            float meanA = 0f, meanB = 0f;
            for (int i = 0; i < 12; i++)
            {
                meanA += a[i];
                meanB += b[i];
            }
            meanA /= 12f;
            meanB /= 12f;

            float num = 0f, denA = 0f, denB = 0f;
            for (int i = 0; i < 12; i++)
            {
                float da = a[i] - meanA;
                float db = b[i] - meanB;
                num += da * db;
                denA += da * da;
                denB += db * db;
            }
            if (denA < 1e-6f || denB < 1e-6f) return 0f;
            return num / Mathf.Sqrt(denA * denB);
        }
    }
}
