using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    [Serializable]
    public struct AudioPowerRequirement
    {
        public string componentId;
        public float watts;
        public int channels;
        public float dspLoad01;
    }

    /// <summary>
    /// Gathers power/DSP requirements rather than hard-limiting audio.
    /// Exceeding upper limits raises an unrealistic output quality warning.
    /// </summary>
    public sealed class AudioPowerBudget : MonoBehaviour
    {
        public float maxWatts = 500f;
        public int maxChannels = 32;
        [Range(0f, 1f)] public float maxDspLoad01 = 0.95f;

        readonly List<AudioPowerRequirement> _requirements = new List<AudioPowerRequirement>();

        public float TotalWatts { get; private set; }
        public int TotalChannels { get; private set; }
        public float TotalDspLoad01 { get; private set; }
        public bool UnrealisticOutputQualityWarning { get; private set; }
        public string WarningMessage { get; private set; } = "";

        public IReadOnlyList<AudioPowerRequirement> Requirements => _requirements;

        public void Clear()
        {
            _requirements.Clear();
            Recompute();
        }

        public void Register(AudioPowerRequirement req)
        {
            for (int i = 0; i < _requirements.Count; i++)
            {
                if (_requirements[i].componentId == req.componentId)
                {
                    _requirements[i] = req;
                    Recompute();
                    return;
                }
            }
            _requirements.Add(req);
            Recompute();
        }

        public void Unregister(string componentId)
        {
            _requirements.RemoveAll(r => r.componentId == componentId);
            Recompute();
        }

        void Recompute()
        {
            float watts = 0f;
            int channels = 0;
            float dsp = 0f;
            for (int i = 0; i < _requirements.Count; i++)
            {
                watts += Mathf.Max(0f, _requirements[i].watts);
                channels += Mathf.Max(0, _requirements[i].channels);
                dsp += Mathf.Clamp01(_requirements[i].dspLoad01);
            }
            TotalWatts = watts;
            TotalChannels = channels;
            TotalDspLoad01 = Mathf.Clamp01(dsp);

            UnrealisticOutputQualityWarning =
                TotalWatts > maxWatts ||
                TotalChannels > maxChannels ||
                TotalDspLoad01 > maxDspLoad01;

            WarningMessage = UnrealisticOutputQualityWarning
                ? $"Unrealistic output quality warning: watts={TotalWatts:F1}/{maxWatts}, channels={TotalChannels}/{maxChannels}, dsp={TotalDspLoad01:F2}/{maxDspLoad01:F2}"
                : "";
        }
    }
}
