#if UNITY_EDITOR
using System;
using Locomotion.Audio;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Audio.EditorTools
{
    public enum MusicTimelineOverlayTab
    {
        Harmonics,
        Modes,
        Pentameters,
        Scales,
        ResonantFilters,
        Spectra
    }

    /// <summary>Timeline overlay tab: harmonics, modes, pentameters, scales, resonant filters, spectra.</summary>
    public sealed class MusicTimelineOverlayWindow : EditorWindow
    {
        MusicTimelineOverlayTab _tab = MusicTimelineOverlayTab.Spectra;
        AudioClip _clip;
        InstrumentProfileCurves _curves;
        Texture2D _spectraTex;
        Vector2 _scroll;
        float _playhead01;

        [MenuItem("Window/System Drawer/Music/Timeline Overlays", false, 352)]
        public static void Open()
        {
            var w = GetWindow<MusicTimelineOverlayWindow>("Music Timeline Overlays");
            w.minSize = new Vector2(640, 420);
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Music Timeline Overlays", EditorStyles.boldLabel);
            _tab = (MusicTimelineOverlayTab)GUILayout.Toolbar((int)_tab, Enum.GetNames(typeof(MusicTimelineOverlayTab)));
            _clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", _clip, typeof(AudioClip), false);
            _curves = (InstrumentProfileCurves)EditorGUILayout.ObjectField("Profile Curves", _curves, typeof(InstrumentProfileCurves), false);
            _playhead01 = EditorGUILayout.Slider("Playhead", _playhead01, 0f, 1f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case MusicTimelineOverlayTab.Harmonics:
                    DrawHarmonics();
                    break;
                case MusicTimelineOverlayTab.Modes:
                    DrawModes();
                    break;
                case MusicTimelineOverlayTab.Pentameters:
                    DrawPentameters();
                    break;
                case MusicTimelineOverlayTab.Scales:
                    DrawScales();
                    break;
                case MusicTimelineOverlayTab.ResonantFilters:
                    DrawResonantFilters();
                    break;
                case MusicTimelineOverlayTab.Spectra:
                    DrawSpectra();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawHarmonics()
        {
            EditorGUILayout.HelpBox("Harmonic alignment markers at integer partials of base pitch.", MessageType.None);
            float baseHz = _curves != null ? Mathf.Lerp(110f, 880f, _curves.pitch.Evaluate(_playhead01)) : 440f;
            for (int h = 1; h <= 8; h++)
            {
                float x = (h - 1) / 7f;
                Rect r = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(r, x, $"H{h} {baseHz * h:F1} Hz");
            }
        }

        void DrawModes()
        {
            var mode = _curves != null ? _curves.scaleMode : MusicScaleMode.Ionian;
            EditorGUILayout.LabelField("Active mode", mode.ToString());
            foreach (MusicScaleMode m in Enum.GetValues(typeof(MusicScaleMode)))
            {
                float align = m == mode ? 1f : 0.15f;
                Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(r, align, m.ToString());
            }
        }

        void DrawPentameters()
        {
            string[] feet = { "Iambic", "Trochaic", "Anapestic", "Dactylic", "Spondaic" };
            for (int i = 0; i < feet.Length; i++)
            {
                float stress = (Mathf.Sin((_playhead01 + i * 0.17f) * Mathf.PI * 2f) + 1f) * 0.5f;
                Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(r, stress, feet[i]);
            }
        }

        void DrawScales()
        {
            int tonic = _curves != null ? _curves.keyTonic : 0;
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            for (int i = 0; i < 12; i++)
            {
                float v = i == tonic ? 1f : ScaleDegreeWeight(i, tonic);
                Rect r = GUILayoutUtility.GetRect(16, 16, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(r, v, names[i]);
            }
        }

        void DrawResonantFilters()
        {
            float cut = _curves != null ? Mathf.Lerp(400f, 12000f, _curves.treble.Evaluate(_playhead01)) : 4000f;
            float res = _curves != null ? _curves.resonance.Evaluate(_playhead01) : 0.4f;
            EditorGUILayout.LabelField("Cutoff", $"{cut:F0} Hz");
            EditorGUILayout.LabelField("Resonance", res.ToString("F2"));
            Rect r = GUILayoutUtility.GetRect(40, 40, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(r, Mathf.InverseLerp(200f, 12000f, cut), "Resonant filter");
        }

        void DrawSpectra()
        {
            if (GUILayout.Button("Rebuild spectra from clip") && _clip != null)
                RebuildSpectra();
            if (_spectraTex != null)
            {
                Rect r = GUILayoutUtility.GetRect(256, 128, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(r, _spectraTex, null, ScaleMode.StretchToFill);
                EditorGUILayout.LabelField("Spectra: amplitude vs frequency (lightweight FFT bins)");
            }
            else
                EditorGUILayout.HelpBox("Assign an AudioClip and rebuild spectra.", MessageType.Info);
        }

        void RebuildSpectra()
        {
            if (_clip == null) return;
            int bins = 128;
            var samples = new float[_clip.samples * _clip.channels];
            _clip.GetData(samples, 0);
            var mags = new float[bins];
            int win = Mathf.Min(samples.Length, 4096);
            for (int b = 0; b < bins; b++)
            {
                float re = 0f, im = 0f;
                float freq = (b + 1f) / bins;
                for (int n = 0; n < win; n++)
                {
                    float ang = 2f * Mathf.PI * freq * n;
                    re += samples[n] * Mathf.Cos(ang);
                    im -= samples[n] * Mathf.Sin(ang);
                }
                mags[b] = Mathf.Sqrt(re * re + im * im) / win;
            }
            float max = 0.0001f;
            for (int i = 0; i < bins; i++) max = Mathf.Max(max, mags[i]);

            if (_spectraTex != null)
                DestroyImmediate(_spectraTex);
            _spectraTex = new Texture2D(bins, 64, TextureFormat.RGBA32, false);
            for (int x = 0; x < bins; x++)
            {
                int h = Mathf.Clamp(Mathf.RoundToInt((mags[x] / max) * 63f), 0, 63);
                for (int y = 0; y < 64; y++)
                {
                    Color c = y <= h ? new Color(0.2f, 0.85f, 0.55f) : new Color(0.08f, 0.08f, 0.1f);
                    _spectraTex.SetPixel(x, y, c);
                }
            }
            _spectraTex.Apply();
            Repaint();
        }

        static float ScaleDegreeWeight(int pc, int tonic)
        {
            int d = (pc - tonic + 12) % 12;
            return d switch
            {
                0 => 0.9f,
                2 => 0.55f,
                4 => 0.7f,
                5 => 0.5f,
                7 => 0.8f,
                9 => 0.45f,
                11 => 0.35f,
                _ => 0.1f
            };
        }
    }
}
#endif
