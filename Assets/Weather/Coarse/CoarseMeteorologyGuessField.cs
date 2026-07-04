using UnityEngine;
using Weather.Emergence;

namespace Weather.Coarse
{
    /// <summary>Lightweight coarse advection grid for meteorological guesses between eggs.</summary>
    public sealed class CoarseMeteorologyGuessField
    {
        public Vector3Int cellCount = new Vector3Int(16, 8, 16);
        public float cellSizeM = 16f;
        public int advectionStride = 1;
        public float updateHz = 4f;

        Vector3 _worldAnchor;
        float[] _temperature;
        float[] _pressure;
        Vector3[] _velocity;
        float _lastUpdate = -999f;

        public Vector3 WorldAnchor => _worldAnchor;

        public void SetAnchor(Vector3 anchor) => _worldAnchor = anchor;

        int FlatIndex(int x, int y, int z)
        {
            return x + cellCount.x * (y + cellCount.y * z);
        }

        void EnsureBuffers()
        {
            int n = cellCount.x * cellCount.y * cellCount.z;
            if (_temperature == null || _temperature.Length != n)
            {
                _temperature = new float[n];
                _pressure = new float[n];
                _velocity = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    _temperature[i] = 15f;
                    _pressure[i] = 1013f;
                    _velocity[i] = Vector3.zero;
                }
            }
        }

        Vector3 CellCenter(int x, int y, int z)
        {
            return _worldAnchor + new Vector3(
                (x + 0.5f) * cellSizeM,
                (y + 0.5f) * cellSizeM,
                (z + 0.5f) * cellSizeM);
        }

        void WorldToCell(Vector3 world, out int x, out int y, out int z)
        {
            Vector3 local = world - _worldAnchor;
            x = Mathf.Clamp(Mathf.FloorToInt(local.x / cellSizeM), 0, cellCount.x - 1);
            y = Mathf.Clamp(Mathf.FloorToInt(local.y / cellSizeM), 0, cellCount.y - 1);
            z = Mathf.Clamp(Mathf.FloorToInt(local.z / cellSizeM), 0, cellCount.z - 1);
        }

        public bool ShouldUpdate(float now)
        {
            if (updateHz <= 0f)
                return false;
            float interval = 1f / updateHz;
            if (now - _lastUpdate >= interval)
            {
                _lastUpdate = now;
                return true;
            }
            return false;
        }

        public void Step(float deltaTime, Weather.Wind wind, EmergenceVectorField emergence)
        {
            EnsureBuffers();
            float dt = Mathf.Max(deltaTime, 1e-4f);

            for (int z = 0; z < cellCount.z; z += advectionStride)
            for (int y = 0; y < cellCount.y; y += advectionStride)
            for (int x = 0; x < cellCount.x; x += advectionStride)
            {
                int idx = FlatIndex(x, y, z);
                Vector3 center = CellCenter(x, y, z);

                Vector3 windVel = Vector3.zero;
                if (wind != null)
                {
                    float rad = wind.direction * Mathf.Deg2Rad;
                    windVel = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * wind.speed;
                }

                if (emergence != null)
                {
                    float w = emergence.GetActivationWeight(center);
                    if (w > 0.01f)
                    {
                        for (int i = 0; i < emergence.Vectors.Count; i++)
                        {
                            EmergenceVector v = emergence.Vectors[i];
                            windVel += v.direction.normalized * (v.weight * w * 2f);
                        }
                    }
                }

                Vector3 backtrace = center - windVel * dt;
                WorldToCell(backtrace, out int sx, out int sy, out int sz);
                int sidx = FlatIndex(sx, sy, sz);

                _velocity[idx] = Vector3.Lerp(_velocity[idx], windVel, 0.35f);
                _temperature[idx] = Mathf.Lerp(_temperature[idx], _temperature[sidx], 0.85f);
                _pressure[idx] = Mathf.Lerp(_pressure[idx], _pressure[sidx], 0.85f);
            }
        }

        public ManifoldCellData GuessAt(Vector3 world)
        {
            EnsureBuffers();
            WorldToCell(world, out int x, out int y, out int z);
            int idx = FlatIndex(x, y, z);
            return new ManifoldCellData
            {
                velocity = _velocity[idx],
                temperature = _temperature[idx],
                pressure = _pressure[idx],
                density = 1.2f,
            };
        }

        public void StoreStoppedGuess(Vector3 world, ManifoldCellData data)
        {
            EnsureBuffers();
            WorldToCell(world, out int x, out int y, out int z);
            int idx = FlatIndex(x, y, z);
            _velocity[idx] = data.velocity;
            _temperature[idx] = data.temperature;
            _pressure[idx] = data.pressure;
        }
    }
}
