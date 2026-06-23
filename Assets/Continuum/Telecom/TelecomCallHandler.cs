using UnityEngine;

namespace Continuum.Telecom
{
    /// <summary>Maps ring events to AudioSource ring playback.</summary>
    public class TelecomCallHandler : MonoBehaviour
    {
        public AudioSource ringAudioSource;
        public AudioClip defaultRingClip;

        public void HandleRing(string payloadJson)
        {
            if (ringAudioSource != null && defaultRingClip != null)
            {
                ringAudioSource.clip = defaultRingClip;
                ringAudioSource.Play();
            }
            Debug.Log($"[Telecom] Ring: {payloadJson}");
        }
    }
}
