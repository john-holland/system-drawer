using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum SpeechVisMode
    {
        Auto,
        Jaw,
        ScaleWobble,
        HeadBobble,
        None
    }

    /// <summary>
    /// Handle for in-flight actor speech playback.
    /// </summary>
    public sealed class SpeechPlaybackHandle
    {
        public bool IsPlaying =>
            (_jawIsPlaying != null && _jawIsPlaying()) ||
            (_audioSource != null && _audioSource.isPlaying);

        readonly Func<bool> _jawIsPlaying;
        readonly AudioSource _audioSource;

        internal SpeechPlaybackHandle(Func<bool> jawIsPlaying, AudioSource audioSource)
        {
            _jawIsPlaying = jawIsPlaying;
            _audioSource = audioSource;
        }

        public void Stop()
        {
            if (_audioSource != null)
                _audioSource.Stop();
        }
    }

    /// <summary>
    /// Routes dialogue/TTS clips to RagdollJaw DSP or ModulatingSoundComponent scale wobble via actor binding.
    /// </summary>
    public static class ActorSpeechPlayback
    {
        static Type _ragdollSystemType;
        static Type _ragdollJawType;
        static Type _modulatingSoundType;

        public static SpeechPlaybackHandle Play(
            NarrativeExecutionContext ctx,
            string speakerKey,
            AudioClip clip,
            SpeechVisMode visMode = SpeechVisMode.Auto,
            float volume = 1f)
        {
            if (clip == null)
                return new SpeechPlaybackHandle(() => false, null);

            if (ctx == null || string.IsNullOrWhiteSpace(speakerKey) ||
                !ctx.TryResolveGameObject(speakerKey, out GameObject actorGo) || actorGo == null)
            {
                Debug.LogWarning("[ActorSpeechPlayback] Could not resolve speaker binding: " + speakerKey);
                return PlayOnFallbackSource(clip, volume);
            }

            EnsureTypes();

            if (visMode == SpeechVisMode.Auto || visMode == SpeechVisMode.Jaw || visMode == SpeechVisMode.HeadBobble)
            {
                var jaw = ResolveJaw(actorGo);
                if (jaw != null)
                {
                    if (visMode == SpeechVisMode.HeadBobble)
                        SetBoolField(jaw, "enableHeadBobble", true);
                    PlayOnJaw(jaw, clip, volume);
                    return new SpeechPlaybackHandle(() => IsJawPlaying(jaw), GetJawAudioSource(jaw));
                }
            }

            if (visMode == SpeechVisMode.Auto || visMode == SpeechVisMode.ScaleWobble)
            {
                var mod = ResolveModulatingSound(actorGo);
                if (mod != null)
                {
                    PlayOnModulatingSound(mod, clip, volume);
                    return new SpeechPlaybackHandle(() => false, GetComponent<AudioSource>(mod));
                }
            }

            if (visMode == SpeechVisMode.None)
                return PlayOnFallbackSource(clip, volume);

            var added = actorGo.AddComponent(GetModulatingSoundType());
            PlayOnModulatingSound(added, clip, volume);
            return new SpeechPlaybackHandle(() => false, GetComponent<AudioSource>(added));
        }

        static SpeechPlaybackHandle PlayOnFallbackSource(AudioClip clip, float volume)
        {
            var go = new GameObject("DialogueSpeechFallback");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.Play();
            return new SpeechPlaybackHandle(() => false, src);
        }

        static void EnsureTypes()
        {
            if (_ragdollSystemType == null)
                _ragdollSystemType = Type.GetType("RagdollSystem, Locomotion.Runtime")
                    ?? Type.GetType("RagdollSystem, Assembly-CSharp");
            if (_ragdollJawType == null)
                _ragdollJawType = Type.GetType("Locomotion.Musculature.RagdollJaw, Locomotion.Runtime")
                    ?? Type.GetType("Locomotion.Musculature.RagdollJaw, Assembly-CSharp");
            if (_modulatingSoundType == null)
                _modulatingSoundType = Type.GetType("ModulatingSoundComponent, Assembly-CSharp");
        }

        static Type GetModulatingSoundType() => _modulatingSoundType;

        static Component ResolveJaw(GameObject actorGo)
        {
            if (_ragdollJawType == null)
                return null;

            if (_ragdollSystemType != null)
            {
                var rs = actorGo.GetComponent(_ragdollSystemType);
                if (rs != null)
                {
                    var find = _ragdollSystemType.GetMethod("FindOrAddJaw");
                    if (find != null)
                        return find.Invoke(rs, null) as Component;
                }
            }

            return actorGo.GetComponentInChildren(_ragdollJawType);
        }

        static Component ResolveModulatingSound(GameObject actorGo)
        {
            if (_modulatingSoundType == null)
                return null;
            return actorGo.GetComponent(_modulatingSoundType)
                ?? actorGo.GetComponentInChildren(_modulatingSoundType);
        }

        static void PlayOnJaw(Component jaw, AudioClip clip, float volume)
        {
            var play = _ragdollJawType.GetMethod("PlaySound", new[] { typeof(AudioClip) });
            if (play != null)
            {
                play.Invoke(jaw, new object[] { clip });
                var src = GetJawAudioSource(jaw);
                if (src != null)
                    src.volume = volume;
            }
        }

        static AudioSource GetJawAudioSource(Component jaw) => jaw.GetComponent<AudioSource>();

        static bool IsJawPlaying(Component jaw)
        {
            var m = _ragdollJawType.GetMethod("IsPlaying");
            return m != null && (bool)m.Invoke(jaw, null);
        }

        static void PlayOnModulatingSound(Component mod, AudioClip clip, float volume)
        {
            SetBoolField(mod, "updateGameObjectDimension", true);
            var src = GetComponent<AudioSource>(mod) ?? mod.gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.Play();
        }

        static void SetBoolField(Component c, string fieldName, bool value)
        {
            var f = c.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
                f.SetValue(c, value);
        }

        static T GetComponent<T>(Component c) where T : Component => c.GetComponent<T>();

        public static SpeechVisMode ParseVisMode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return SpeechVisMode.Auto;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "jaw": return SpeechVisMode.Jaw;
                case "wobble":
                case "scale":
                case "scalewobble": return SpeechVisMode.ScaleWobble;
                case "bobble":
                case "headbobble": return SpeechVisMode.HeadBobble;
                case "none": return SpeechVisMode.None;
                default: return SpeechVisMode.Auto;
            }
        }
    }
}
