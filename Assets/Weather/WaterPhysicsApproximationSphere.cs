using UnityEngine;

namespace Weather
{
    /// <summary>Pooled water sphere for cheap flow/flood approximation.</summary>
    [System.Serializable]
    public struct WaterPhysicsSphereState
    {
        public Vector3 position;
        public Vector3 velocity;
        public float radius;
        public bool active;
    }

    /// <summary>Rolling sphere pool for liquid flood (manifold paint driven externally).</summary>
    public sealed class WaterPhysicsApproximationSphere : MonoBehaviour
    {
        public int maxSpheres = 64;
        public float sphereRadiusM = 0.003f;
        public float gravity = 9.81f;

        WaterPhysicsSphereState[] _pool;
        int _activeCount;

        void Awake()
        {
            _pool = new WaterPhysicsSphereState[Mathf.Max(8, maxSpheres)];
        }

        public int ActiveCount => _activeCount;

        public void Clear()
        {
            _activeCount = 0;
            for (int i = 0; i < _pool.Length; i++)
                _pool[i].active = false;
        }

        public bool TrySpawn(Vector3 position, Vector3 velocity)
        {
            if (_pool == null)
                Awake();
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].active)
                    continue;
                _pool[i] = new WaterPhysicsSphereState
                {
                    position = position,
                    velocity = velocity,
                    radius = sphereRadiusM,
                    active = true,
                };
                _activeCount++;
                return true;
            }
            return false;
        }

        public void Step(float dt, System.Func<WaterPhysicsSphereState, bool> onExitLoop)
        {
            if (_pool == null || dt <= 0f)
                return;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].active)
                    continue;
                var s = _pool[i];
                s.velocity += Vector3.down * gravity * dt;
                s.position += s.velocity * dt;
                if (onExitLoop != null && onExitLoop(s))
                {
                    s.active = false;
                    _activeCount = Mathf.Max(0, _activeCount - 1);
                }
                _pool[i] = s;
            }
        }

        public void ForEachActive(System.Action<WaterPhysicsSphereState> visitor)
        {
            if (_pool == null || visitor == null)
                return;
            foreach (var s in _pool)
            {
                if (s.active)
                    visitor(s);
            }
        }
    }
}
