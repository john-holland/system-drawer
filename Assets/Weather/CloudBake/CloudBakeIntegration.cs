using System;
using System.Collections.Generic;
using UnityEngine;
using Weather.Executor;
using Weather.Lod;

namespace Weather.CloudBake
{
    /// <summary>Manifold cell snapshot at a world position for anchor reset.</summary>
    [Serializable]
    public struct ManifoldAnchorCell
    {
        public Vector3 worldPosition;
        public ManifoldCellData data;
    }

    /// <summary>
    /// Manages bake-time weather executor egg zones and stopped-space cache
    /// according to <see cref="CloudPerspectiveBakeConfig.allowFloatAway"/>.
    /// </summary>
    public sealed class CloudBakeIntegration : IDisposable
    {
        const string BakeEggClientId = "cloud_bake";

        readonly WeatherPhysicsManifold _manifold;
        readonly WeatherExecutorService _executor;
        readonly CloudPerspectiveBakeConfig _config;
        PlayerWeatherEggZone _bakeEgg;

        public CloudBakeIntegration(
            WeatherPhysicsManifold manifold,
            WeatherExecutorService executor,
            CloudPerspectiveBakeConfig config)
        {
            _manifold = manifold;
            _executor = executor;
            _config = config ?? new CloudPerspectiveBakeConfig();
        }

        public void BeginSession(CloudHalfShellStack stack)
        {
            if (stack == null || _manifold == null)
                return;

            if (_config.useExecutorAdvection && _config.allowFloatAway && _executor != null)
            {
                _bakeEgg = _executor.GetOrCreateEgg(BakeEggClientId);
                _bakeEgg.transform.position = stack.shellBounds.center;
                _bakeEgg.radii = stack.shellBounds.extents;
                _executor.Registry.Register(_bakeEgg);
                _manifold.SetEggLodActive(true, stack.shellBounds);
            }
            else if (!_config.allowFloatAway)
            {
                _manifold.SetEggLodActive(true, stack.shellBounds);
            }
        }

        public void AfterIteration(CloudHalfShellStack stack, CloudBakeAnchor anchor, SphericalHyperplaneRegression regression)
        {
            if (stack == null)
                return;

            if (!_config.allowFloatAway && _executor != null && regression != null)
                _executor.StoppedSpace.StoreRegression(stack.shellBounds.center, regression);

            if (_config.allowFloatAway && _config.useExecutorAdvection && _executor != null && _manifold != null)
            {
                _manifold.SetEggLodActive(true, stack.shellBounds);
                _executor.TickClient(_config.advectionDeltaTime);
            }
        }

        public void EndSession()
        {
            if (_manifold != null)
                _manifold.SetEggLodActive(false, default);

            if (_bakeEgg != null && _executor != null)
                _executor.Registry.Unregister(_bakeEgg);

            _bakeEgg = null;
        }

        public void Dispose() => EndSession();
    }
}
