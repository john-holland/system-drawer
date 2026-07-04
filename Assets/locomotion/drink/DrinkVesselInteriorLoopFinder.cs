using UnityEngine;
using Weather;

namespace Locomotion.Drink
{
    /// <summary>Interior loop search for shake-free pour angles (wraps MeshTerrainSampler).</summary>
    public sealed class DrinkVesselInteriorLoopFinder : MonoBehaviour
    {
        public DrinkVesselComponent vessel;
        public MeshTerrainSampler sampler;
        public int yawSteps = 8;
        public int pitchSteps = 5;

        public float SearchBestAngleEfficiency(Vector3 nozzleForward, out Vector3 bestForward)
        {
            bestForward = nozzleForward;
            if (vessel == null || vessel.interiorMeshCollider == null)
                return 1f;

            if (sampler == null)
                sampler = GetComponent<MeshTerrainSampler>() ?? gameObject.AddComponent<MeshTerrainSampler>();

            float best = 0f;
            Vector3 bestDir = nozzleForward.normalized;
            for (int y = 0; y < yawSteps; y++)
            {
                float yaw = (y / (float)yawSteps) * 360f;
                for (int p = 0; p < pitchSteps; p++)
                {
                    float pitch = -30f + (p / (float)Mathf.Max(1, pitchSteps - 1)) * 60f;
                    var rot = Quaternion.Euler(pitch, yaw, 0f);
                    Vector3 dir = rot * Vector3.forward;
                    float score = ScoreDirection(dir);
                    if (score > best)
                    {
                        best = score;
                        bestDir = dir;
                    }
                }
            }
            bestForward = bestDir;
            return Mathf.Clamp01(best);
        }

        float ScoreDirection(Vector3 dir)
        {
            if (vessel.interiorMeshCollider == null)
                return 0.5f;
            Vector3 origin = vessel.transform.position + Vector3.up * 0.05f;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 0.2f) &&
                hit.collider == vessel.interiorMeshCollider)
                return 1f;
            return 0.3f;
        }
    }
}
