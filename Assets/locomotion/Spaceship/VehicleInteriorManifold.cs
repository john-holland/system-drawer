using System.Collections.Generic;
using UnityEngine;
using Weather;

namespace Locomotion.Spaceship
{
    public sealed class VehicleInteriorManifold : MonoBehaviour
    {
        public VehicleActor vehicle;
        public MeshTerrainSampler interiorSampler;
        public WeatherPhysicsManifold interiorWeatherManifold;
        public float describeDebounceSeconds = 0.5f;

        readonly Dictionary<int, List<Vector3>> _pathCache = new Dictionary<int, List<Vector3>>();
        float _lastDescribeTime;

        public bool TryGetPathToInstrument(int instanceId, Vector3 from, out List<Vector3> path)
        {
            if (_pathCache.TryGetValue(instanceId, out path))
                return path != null && path.Count > 0;

            // Discover bound / tagged instrument transforms under the vehicle (straight-segment v1).
            Transform instrument = FindInstrumentTransform(instanceId);
            Vector3 goal = instrument != null
                ? instrument.position
                : from + (vehicle != null ? vehicle.transform.forward : Vector3.forward) * 2f;
            path = new List<Vector3> { from, goal };
            _pathCache[instanceId] = path;
            return true;
        }

        /// <summary>Resolve an instrument by instance id, name hash, or first VehicleInstrumentPhysicsProxy binding origin.</summary>
        public Transform FindInstrumentTransform(int instanceId)
        {
            if (vehicle == null)
                return null;
            var transforms = vehicle.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (t == null) continue;
                if (t.GetInstanceID() == instanceId)
                    return t;
            }

            var proxy = vehicle.GetComponentInChildren<VehicleInstrumentPhysicsProxy>();
            if (proxy != null && proxy.bindings != null)
            {
                for (int i = 0; i < proxy.bindings.Count; i++)
                {
                    var b = proxy.bindings[i];
                    if (b?.remoteForceOrigin != null && b.remoteForceOrigin.GetInstanceID() == instanceId)
                        return b.remoteForceOrigin;
                    if (b != null && !string.IsNullOrEmpty(b.localSurfaceId) &&
                        b.localSurfaceId.GetHashCode() == instanceId)
                    {
                        return b.remoteForceOrigin != null ? b.remoteForceOrigin : vehicle.transform;
                    }
                }
            }
            return null;
        }

        public void NotifyInteriorChanged()
        {
            if (Time.time - _lastDescribeTime < describeDebounceSeconds)
                return;
            _lastDescribeTime = Time.time;
            _pathCache.Clear();
            if (interiorSampler != null)
                interiorSampler.DetectEnclosedSpaces();
        }
    }
}
