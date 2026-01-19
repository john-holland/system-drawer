using UnityEngine;

namespace Locomotion.Senses
{
    /// <summary>
    /// Convenience component for configuring a pair of Visual sensors as "eyes".
    /// This does not replace the core sensing pipeline (Sensor -> WorldInteraction -> NervousSystem);
    /// it just helps keep left/right eye sensors organized and optionally visualizes FOV.
    /// </summary>
    public class Eyes : MonoBehaviour
    {
        [Header("Eye Sensors (optional)")]
        public Sensor leftEye;
        public Sensor rightEye;

        [Tooltip("If true, auto-find Visual sensors in children on Awake.")]
        public bool autoFindEyes = true;

        [Header("Debug Gizmos")]
        public bool drawGizmos = true;
        public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.25f);
        public float gizmoRangeOverride = 0f;

        private void Awake()
        {
            if (!autoFindEyes)
                return;

            if (leftEye == null || rightEye == null)
            {
                var sensors = GetComponentsInChildren<Sensor>(includeInactive: true);
                for (int i = 0; i < sensors.Length; i++)
                {
                    var s = sensors[i];
                    if (s == null || s.sensorType != SensorType.Visual)
                        continue;

                    if (leftEye == null && s != rightEye)
                        leftEye = s;
                    else if (rightEye == null && s != leftEye)
                        rightEye = s;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            DrawEyeGizmo(leftEye);
            DrawEyeGizmo(rightEye);
        }

        private void DrawEyeGizmo(Sensor eye)
        {
            if (eye == null || eye.sensorType != SensorType.Visual)
                return;

            float range = gizmoRangeOverride > 0f ? gizmoRangeOverride : eye.range;
            float halfFov = eye.fieldOfViewDegrees * 0.5f;

            Gizmos.color = gizmoColor;
            Vector3 origin = eye.transform.position;
            Vector3 forward = eye.transform.forward;

            // Draw a simple arc boundary (approximate) and a forward line.
            Gizmos.DrawLine(origin, origin + forward * range);

            Vector3 left = Quaternion.AngleAxis(-halfFov, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(halfFov, Vector3.up) * forward;
            Gizmos.DrawLine(origin, origin + left.normalized * range);
            Gizmos.DrawLine(origin, origin + right.normalized * range);
        }
    }
}

