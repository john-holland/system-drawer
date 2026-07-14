using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Routes score parts into InstrumentProxyBank voices.</summary>
    public sealed class OrchestralAssembly : MonoBehaviour
    {
        public InstrumentProxyBank bank;
        public float bpm = 120f;

        readonly Dictionary<string, string> _partToVoice = new Dictionary<string, string>();

        public void MapPart(string partName, string proxyVoiceId)
        {
            if (string.IsNullOrEmpty(partName) || string.IsNullOrEmpty(proxyVoiceId)) return;
            _partToVoice[partName] = proxyVoiceId;
        }

        public int AssembleAndArticulate(ScoreDocument doc)
        {
            if (doc?.events == null || bank == null) return 0;
            int count = 0;
            for (int i = 0; i < doc.events.Length; i++)
            {
                var e = doc.events[i];
                string voice = e.proxyVoiceId;
                if (!string.IsNullOrEmpty(e.partName) && _partToVoice.TryGetValue(e.partName, out var mapped))
                    voice = mapped;
                if (!bank.TryGet(voice, out var proxy) || proxy == null)
                    continue;
                float control = e.velocity01;
                // Map MIDI into pitch curve domain via proxy profile
                proxy.EnsureProfile();
                if (proxy.profileCurves != null)
                    proxy.profileCurves.keyTonic = ((e.midiNote % 12) + 12) % 12;
                float t = proxy.QuantizeEventTime(e.timeSec, doc.bpm > 0 ? doc.bpm : bpm);
                _ = t;
                if (proxy.TryArticulate("key", control, e.timeSec, doc.bpm > 0 ? doc.bpm : bpm, out _))
                    count++;
            }
            return count;
        }
    }
}
