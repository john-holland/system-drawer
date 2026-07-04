using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>Bar-quantized stem crossfade mixer (RogueScroll-style layering).</summary>
    public sealed class MusicPlaybackMixer
    {
        readonly Dictionary<MusicStemRole, AudioSource> _sources = new Dictionary<MusicStemRole, AudioSource>();
        readonly List<MusicStemSlot> _pendingPlay = new List<MusicStemSlot>();
        readonly List<MusicStemRole> _pendingStop = new List<MusicStemRole>();

        float _timeSinceQuantize;
        float _quantizeIntervalSec = 0.5f;
        float _masterVolume = 1f;

        public float QuantizeIntervalSec
        {
            get => _quantizeIntervalSec;
            set => _quantizeIntervalSec = Mathf.Max(0.05f, value);
        }

        public void BindSource(MusicStemRole role, AudioSource source)
        {
            if (source != null)
                _sources[role] = source;
        }

        public void SetMasterVolume(float v) => _masterVolume = Mathf.Clamp01(v);

        public void QueuePlay(MusicStemSlot slot)
        {
            _pendingPlay.Add(slot);
        }

        public void QueueStop(MusicStemRole role)
        {
            if (!_pendingStop.Contains(role))
                _pendingStop.Add(role);
        }

        public void Tick(float deltaTime, float bpm)
        {
            if (bpm > 0f)
                _quantizeIntervalSec = 60f / bpm * 4f;

            _timeSinceQuantize += deltaTime;
            if (_timeSinceQuantize < _quantizeIntervalSec)
                return;

            _timeSinceQuantize -= _quantizeIntervalSec;

            for (int i = 0; i < _pendingStop.Count; i++)
            {
                MusicStemRole role = _pendingStop[i];
                if (_sources.TryGetValue(role, out AudioSource src) && src != null)
                    src.Stop();
            }
            _pendingStop.Clear();

            for (int i = 0; i < _pendingPlay.Count; i++)
            {
                MusicStemSlot slot = _pendingPlay[i];
                if (!_sources.TryGetValue(slot.role, out AudioSource src) || src == null || slot.clip == null)
                    continue;
                src.clip = slot.clip;
                src.volume = slot.volume * _masterVolume;
                src.loop = true;
                src.pitch = Mathf.Pow(2f, slot.transpositionSemitones / 12f);
                src.Play();
            }
            _pendingPlay.Clear();
        }

        public void CrossfadeToSlots(IReadOnlyList<MusicStemSlot> slots, float accentSwell = 1f)
        {
            if (slots == null) return;
            var activeRoles = new HashSet<MusicStemRole>();
            for (int i = 0; i < slots.Count; i++)
            {
                MusicStemSlot s = slots[i];
                activeRoles.Add(s.role);
                var copy = s;
                if (s.role == MusicStemRole.Accent)
                    copy.volume *= accentSwell;
                QueuePlay(copy);
            }
            foreach (var kvp in _sources)
            {
                if (!activeRoles.Contains(kvp.Key))
                    QueueStop(kvp.Key);
            }
        }
    }
}
