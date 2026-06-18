using System;
using System.Collections.Generic;
using UnityEngine;
using Weather.Lod;

namespace Weather.Executor
{
    public sealed class WeatherExecutorService : MonoBehaviour
    {
        public const string ServiceKey = "weather.executor";

        public static WeatherExecutorService Instance { get; private set; }

        [Header("References")]
        public WeatherSystem weatherSystem;
        public WeatherPhysicsManifold manifold;
        public Transform focusTransform;

        [Header("Egg Defaults")]
        public Vector3 defaultEggRadii = new Vector3(40f, 60f, 40f);
        public float linearityThreshold = 0.25f;
        public int maxRegressionLayers = 6;

        [Header("Timing")]
        public float clientPushInterval = 0.1f;

        public PlayerWeatherEggRegistry Registry { get; } = new PlayerWeatherEggRegistry();
        public WeatherStoppedSpaceCache StoppedSpace { get; } = new WeatherStoppedSpaceCache();

        readonly WeatherWorkQueue _workQueue = new WeatherWorkQueue();
        readonly WeatherGradientEggMerger _merger = new WeatherGradientEggMerger();
        readonly Dictionary<string, PlayerWeatherEggZone> _clientEggs = new Dictionary<string, PlayerWeatherEggZone>();

        float _lastClientPushTime = -999f;
        int _frameIndex;
        bool _isServer;

        public event Action<string> OnBroadcastApply;
        public event Action<string> OnBroadcastBootstrap;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResolveReferences();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        public void SetServerMode(bool isServer) => _isServer = isServer;

        void ResolveReferences()
        {
            if (weatherSystem == null)
                weatherSystem = GetComponent<WeatherSystem>() ?? FindAnyObjectByType<WeatherSystem>();
            if (manifold == null)
            {
                SceneServiceLookup.TryResolve("weather.physicsManifold", out manifold);
                if (manifold == null)
                    manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            }
            if (focusTransform == null && weatherSystem != null)
                focusTransform = weatherSystem.transform;
        }

        public PlayerWeatherEggZone GetOrCreateEgg(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                clientId = "local";
            if (_clientEggs.TryGetValue(clientId, out PlayerWeatherEggZone existing) && existing != null)
                return existing;

            var go = new GameObject($"WeatherEgg_{clientId}");
            go.transform.SetParent(transform, false);
            var zone = go.AddComponent<PlayerWeatherEggZone>();
            zone.clientId = clientId;
            zone.radii = defaultEggRadii;
            if (focusTransform != null)
                go.transform.position = focusTransform.position;
            _clientEggs[clientId] = zone;
            return zone;
        }

        public void TickClient(float deltaTime)
        {
            _frameIndex++;
            PlayerWeatherEggZone egg = GetOrCreateEgg("local");
            if (focusTransform != null)
                egg.transform.position = focusTransform.position;

            foreach (PlayerWeatherEggZone z in Registry.Eggs)
                z?.TickServerBlend();

            if (manifold == null || weatherSystem == null)
                return;

            Bounds eggBounds = egg.GetBounds();
            manifold.SetEggLodActive(true, eggBounds);
            weatherSystem.ServiceUpdateSubsystems(deltaTime, skipManifold: true);
            manifold.ServiceUpdateInBounds(deltaTime, eggBounds);

            FitRegressionForEgg(egg);

            if (Time.time - _lastClientPushTime >= clientPushInterval)
            {
                _lastClientPushTime = Time.time;
                WeatherEggClientPayload payload = BuildClientPayload(egg);
                if (_isServer)
                    _workQueue.Enqueue(payload);
                else
                    WeatherNetworkSink.SendPush?.Invoke(payload);
            }
        }

        public void TickServer(float deltaTime)
        {
            List<WeatherAdvectionWorkOrder> orders = _workQueue.DequeueDue(_frameIndex);
            var payloads = new List<WeatherEggClientPayload>(orders.Count);
            for (int i = 0; i < orders.Count; i++)
            {
                WeatherAdvectionWorkOrder order = orders[i];
                if (order.payload != null)
                    payloads.Add(order.payload);
            }

            if (payloads.Count == 0)
                return;

            _merger.MergeClientPayloads(payloads, manifold, 1f);
            _merger.MergeOverlappingEggs(Registry, manifold);

            var apply = new WeatherEggApplyPayload
            {
                frameIndex = _frameIndex,
                authorityClientId = "server",
                eggCenter = Registry.Eggs.Count > 0 ? Registry.Eggs[0].Center : Vector3.zero,
                eggRadii = Registry.Eggs.Count > 0 ? Registry.Eggs[0].Radii : defaultEggRadii,
                regressionPayload = payloads[0].regressionPayload,
                sparseDiffPayload = payloads[0].sparseDiffPayload,
                definitionLevel = 1f
            };
            string json = WeatherEggPayloadSerializer.ToJson(apply);
            OnBroadcastApply?.Invoke(json);
            ApplyServerPayloadToClients(apply);
        }

        public void HandleClientPush(WeatherEggClientPayload payload)
        {
            if (payload == null)
                return;
            _workQueue.Enqueue(payload);
        }

        public void ApplyServerPayload(WeatherEggApplyPayload payload)
        {
            if (payload == null || manifold == null)
                return;

            PlayerWeatherEggZone egg = GetOrCreateEgg("local");
            egg.transform.position = payload.eggCenter;
            egg.radii = payload.eggRadii;
            egg.BeginServerBlend(payload.definitionLevel);

            SphericalHyperplaneRegression regression = HyperplaneWeatherDiffCodec.DecodeRegression(payload.regressionPayload);
            if (regression != null)
            {
                egg.Regression.center = regression.center;
                egg.Regression.effectiveRadius = regression.effectiveRadius;
                egg.Regression.residualVariance = regression.residualVariance;
                egg.Regression.layers = regression.layers;
                StoppedSpace.StoreRegression(payload.eggCenter, egg.Regression);
            }

            if (payload.sparseDiffPayload != null && payload.sparseDiffPayload.Length > 0)
                HyperplaneWeatherDiffCodec.ApplySparseDiff(payload.sparseDiffPayload, manifold);
        }

        void ApplyServerPayloadToClients(WeatherEggApplyPayload payload)
        {
            ApplyServerPayload(payload);
            WeatherNetworkSink.BroadcastApply?.Invoke(payload);
        }

        void FitRegressionForEgg(PlayerWeatherEggZone egg)
        {
            List<ManifoldSample> samples = HyperplaneWeatherDiffCodec.CollectSamples(
                manifold, egg.Center, egg.Radii, manifold.advectionStride);
            egg.Regression.FitFromSamples(egg.Center, samples, linearityThreshold, maxRegressionLayers);
            StoppedSpace.StoreRegression(egg.Center, egg.Regression);
        }

        WeatherEggClientPayload BuildClientPayload(PlayerWeatherEggZone egg)
        {
            byte[] sparse = null;
            byte[] regressionBytes = HyperplaneWeatherDiffCodec.EncodeRegression(egg.Regression);
            int sparseBytes = 0;

            if (!egg.CircuitBreaker.ShouldFoldToRegression(sparseBytes, egg.Regression.residualVariance))
            {
                sparse = HyperplaneWeatherDiffCodec.EncodeSparseDiff(manifold, egg.GetBounds(), manifold.advectionStride);
                sparseBytes = sparse?.Length ?? 0;
                if (egg.CircuitBreaker.ShouldFoldToRegression(sparseBytes, egg.Regression.residualVariance))
                    sparse = null;
            }

            return new WeatherEggClientPayload
            {
                clientId = egg.clientId,
                frameIndex = _frameIndex,
                eggCenter = egg.Center,
                eggRadii = egg.Radii,
                confidence = egg.confidence,
                regressionPayload = regressionBytes,
                sparseDiffPayload = sparse,
                residualVariance = egg.Regression.residualVariance,
                timeoutOrder = 0
            };
        }

        public bool TryQueryWeatherAt(Vector3 world, out ManifoldCellData data)
        {
            data = default;
            for (int i = 0; i < Registry.Eggs.Count; i++)
            {
                PlayerWeatherEggZone egg = Registry.Eggs[i];
                if (egg == null)
                    continue;
                if (egg.Contains(world))
                {
                    data = egg.QueryLocal(world, manifold);
                    return true;
                }
            }

            if (StoppedSpace.TryEvaluate(world, out data))
                return true;

            if (manifold != null)
            {
                data = manifold.GetDataAtPosition(world);
                return true;
            }

            return false;
        }

        public void ClearDiagnosticCaches()
        {
            StoppedSpace.Clear();
            _workQueue.Clear();
        }

        public void BootstrapEgg(WeatherEggBootstrapPayload payload)
        {
            if (payload == null)
                return;
            PlayerWeatherEggZone egg = GetOrCreateEgg("local");
            if (payload.suggestedRadii.sqrMagnitude > 0.01f)
                egg.radii = payload.suggestedRadii;
            if (payload.suggestedCenter.sqrMagnitude > 0.01f || payload.latDeg != 0f || payload.lonDeg != 0f)
                egg.transform.position = payload.suggestedCenter;

            OnBroadcastBootstrap?.Invoke(WeatherEggPayloadSerializer.ToJson(payload));
            WeatherNetworkSink.BroadcastBootstrap?.Invoke(payload);
        }

        public void Update()
        {
            if (_isServer)
                TickServer(Time.deltaTime);
        }
    }
}
