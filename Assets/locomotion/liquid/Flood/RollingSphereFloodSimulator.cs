using UnityEngine;
using Weather;

namespace Locomotion.Liquid.Flood
{
    /// <summary>Rolling-sphere flood with weather manifold paint.</summary>
    public sealed class RollingSphereFloodSimulator : MonoBehaviour
    {
        public OpenEdgeLoopSpoutSimulator spout;
        public WaterPhysicsApproximationSphere spherePool;
        public LiquidWeatherManifoldBridge weatherBridge;
        public float spawnRatePerSecond = 12f;
        public bool infiniteDrain;
        public int paintEveryNthSphere = 1;
        public float standingLiters;
        public float lastDrainedLiters;
        public float lastDrainedLitersPerSecond;

        float _spawnAccumulator;
        int _paintCounter;

        void Awake()
        {
            if (spherePool == null)
                spherePool = GetComponent<WaterPhysicsApproximationSphere>()
                             ?? gameObject.AddComponent<WaterPhysicsApproximationSphere>();
        }

        void Update()
        {
            if (spherePool == null)
                return;

            _spawnAccumulator += spawnRatePerSecond * Time.deltaTime;
            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                Vector3 origin = spout != null ? spout.RimWorldPosition : transform.position;
                spherePool.TrySpawn(origin + Vector3.up * 0.01f, Vector3.down * 0.1f);
            }

            spherePool.Step(Time.deltaTime, s =>
            {
                if (spout != null && spout.TryExitLoop(s.position, out Vector3 exitVel))
                {
                    PaintSphere(s, exitVel);
                    return true;
                }
                if (s.position.y < transform.position.y - 0.5f)
                {
                    PaintSphere(s, s.velocity);
                    return true;
                }
                return false;
            });
        }

        void PaintSphere(WaterPhysicsSphereState s, Vector3 velocity)
        {
            if (weatherBridge == null)
                return;
            _paintCounter++;
            if (_paintCounter % paintEveryNthSphere != 0)
                return;
            weatherBridge.PaintWaterSphere(s.position, s.radius * 2f, velocity, 101325f);
        }

        public void EmitFromFlow(float litersPerSecond)
        {
            standingLiters += Mathf.Max(0f, litersPerSecond);
            spawnRatePerSecond = infiniteDrain
                ? Mathf.Max(spawnRatePerSecond, litersPerSecond * 800f)
                : litersPerSecond * 400f;
        }

        public void DrainFromFlow(float litersPerSecond)
        {
            lastDrainedLitersPerSecond = Mathf.Max(0f, litersPerSecond);
            lastDrainedLiters = DrainAmount(lastDrainedLitersPerSecond);
            spawnRatePerSecond = Mathf.Max(0f, spawnRatePerSecond - lastDrainedLitersPerSecond * 400f);
        }

        public float DrainAmount(float liters)
        {
            float taken = FloodDrainageAmounts.ApplyDrain(ref standingLiters, liters);
            lastDrainedLiters = taken;
            int recycle = Mathf.CeilToInt(FloodDrainageAmounts.SpawnRateFromLitersPerSecond(taken) / 60f);
            spherePool?.RecycleOldest(recycle);
            if (weatherBridge != null && taken > 0f)
            {
                Vector3 origin = spout != null ? spout.RimWorldPosition : transform.position;
                weatherBridge.ReduceWaterPaint(origin, Mathf.Max(0.02f, taken * 0.01f), Mathf.Clamp01(taken / 20f));
            }
            return taken;
        }
    }
}
