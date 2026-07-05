using UnityEngine;

namespace Continuuuum.Telecom
{
    /// <summary>Maps notifyVisual to locomotion Sensor stimulus / screen material flash.</summary>
    public class TelecomVisualNotifier : MonoBehaviour
    {
        public MonoBehaviour sensorSource;
        public Renderer screenRenderer;
        public Color flashColor = Color.cyan;
        public float flashDuration = 0.3f;

        float _flashUntil;

        void Update()
        {
            if (screenRenderer == null || Time.time > _flashUntil) return;
            screenRenderer.material.color = Color.Lerp(flashColor, Color.white, (Time.time - (_flashUntil - flashDuration)) / flashDuration);
        }

        public void Notify(string payloadJson)
        {
            _flashUntil = Time.time + flashDuration;
            if (sensorSource != null)
                Debug.Log($"[Telecom] Visual notify on {sensorSource.name}: {payloadJson}");
        }
    }
}
