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
            path = new List<Vector3> { from, from + Vector3.forward * 2f };
            _pathCache[instanceId] = path;
            return true;
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
