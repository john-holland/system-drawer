using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxGridCache
    {
        Bounds _worldBounds;
        int _rx, _ry, _rz;
        float[] _occupancy;
        bool _built;

        public bool IsBuilt => _built;
        public Bounds WorldBounds => _worldBounds;

        public void Build(SdfMaxEvaluator evaluator, Bounds worldBounds, int resX, int resY, int resZ)
        {
            _worldBounds = worldBounds;
            _rx = Mathf.Max(1, resX);
            _ry = Mathf.Max(1, resY);
            _rz = Mathf.Max(1, resZ);
            int count = _rx * _ry * _rz;
            if (_occupancy == null || _occupancy.Length != count)
                _occupancy = new float[count];

            Vector3 origin = worldBounds.min;
            Vector3 step = new Vector3(
                worldBounds.size.x / _rx,
                worldBounds.size.y / _ry,
                worldBounds.size.z / _rz);

            int idx = 0;
            for (int z = 0; z < _rz; z++)
            for (int y = 0; y < _ry; y++)
            for (int x = 0; x < _rx; x++, idx++)
            {
                Vector3 p = origin + new Vector3((x + 0.5f) * step.x, (y + 0.5f) * step.y, (z + 0.5f) * step.z);
                float phi = evaluator != null ? evaluator.Sample(p, 0f) : 1000f;
                _occupancy[idx] = phi < 0f ? 1f : 0f;
            }

            _built = true;
        }

        public float SampleOccupancy(Vector3 worldPos)
        {
            if (!_built || _occupancy == null)
                return 0f;

            Vector3 rel = worldPos - _worldBounds.min;
            Vector3 step = new Vector3(
                _worldBounds.size.x / _rx,
                _worldBounds.size.y / _ry,
                _worldBounds.size.z / _rz);
            if (step.x < 1e-6f || step.y < 1e-6f || step.z < 1e-6f)
                return 0f;

            int x = Mathf.Clamp(Mathf.FloorToInt(rel.x / step.x), 0, _rx - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(rel.y / step.y), 0, _ry - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(rel.z / step.z), 0, _rz - 1);
            return _occupancy[x + y * _rx + z * _rx * _ry];
        }
    }
}
