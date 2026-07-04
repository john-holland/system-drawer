using System;
using System.Collections.Generic;
using UnityEngine;
using Weather.Activation;
using Weather.Coarse;
using Weather.Emergence;
using Weather.Lod;
using Weather.Scheduling;

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

        [Header("Emergence")]
        public bool emergenceOnlyMode = true;
        public WeatherEmergenceCollector emergenceCollector;
        public WeatherSimLayerConfig simLayerConfig;

        [Header("Egg Defaults")]
        public Vector3 defaultEggRadii = new Vector3(40f, 60f, 40f);
        public float linearityThreshold = 0.25f;
        public int maxRegressionLayers = 6;

        [Header("Timing")]
        public float clientPushInterval = 0.1f;

        public PlayerWeatherEggRegistry Registry { get; } = new PlayerWeatherEggRegistry();
        public WeatherStoppedSpaceCache StoppedSpace { get; } = new WeatherStoppedSpaceCache();
        public WeatherActivationGate ActivationGate { get; } = new WeatherActivationGate();
        public WeatherSimScheduler SimScheduler { get; } = new WeatherSimScheduler();
        public CoarseMeteorologyGuessField CoarseGuess { get; } = new CoarseMeteorologyGuessField();
        public EmergenceVectorField EmergenceField => emergenceCollector != null ? emergenceCollector.Field : null;

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
            ActivationGate.emergenceOnlyMode = emergenceOnlyMode;
            SimScheduler.config = simLayerConfig;
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
            if (emergenceCollector == null)
                emergenceCollector = GetComponent<WeatherEmergenceCollector>();
            if (emergenceCollector == null)
                emergenceCollector = gameObject.AddComponent<WeatherEmergenceCollector>();
            if (emergenceCollector.playerFocus == null)
                emergenceCollector.playerFocus = focusTransform;
            if (simLayerConfig == null)
                simLayerConfig = Resources.Load<WeatherSimLayerConfig>("WeatherSimLayerConfig");
        }

        public void SetEmergenceOnlyMode(bool enabled)
        {
            emergenceOnlyMode = enabled;
            ActivationGate.SetEmergenceOnlyMode(enabled);
            weatherSystem?.SetEmergenceOnlyMode(enabled);
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
            ActivationGate.emergenceOnlyMode = emergenceOnlyMode;

            if (emergenceCollector != null)
            {
                if (emergenceCollector.playerFocus == null)
                    emergenceCollector.playerFocus = focusTransform;
                emergenceCollector.Tick();
            }

            EmergenceVectorField field = EmergenceField;
            float activationWeight = field != null && focusTransform != null
                ? field.GetActivationWeight(focusTransform.position)
                : 0f;

            PlayerWeatherEggZone egg = GetOrCreateEgg("local");
            Vector3 defaultRadii = defaultEggRadii;
            if (field != null && focusTransform != null)
            {
                EmergenceEggShaper.ShapeEgg(
                    focusTransform.position,
                    defaultEggRadii,
                    field,
                    out Vector3 shapedCenter,
                    out Vector3 shapedRadii);
                egg.transform.position = shapedCenter;
                egg.radii = shapedRadii;
            }
            else if (focusTransform != null)
            {
                egg.transform.position = focusTransform.position;
            }

            foreach (PlayerWeatherEggZone z in Registry.Eggs)
                z?.TickServerBlend();

            if (manifold == null || weatherSystem == null)
                return;

            bool insideEgg = egg.Contains(focusTransform != null ? focusTransform.position : egg.Center);
            float now = Time.time;

            CoarseGuess.SetAnchor(Registry.GetCombinedBounds().center);
            if (SimScheduler.ShouldTick(WeatherSimLayerId.L0_MeteorologyGuess, activationWeight, insideEgg, now))
            {
                ManifoldCellData guess = CoarseGuess.GuessAt(egg.Center);
                StoppedSpace.StoreCoarseGuess(egg.Center, guess);
            }

            if (SimScheduler.ShouldTick(WeatherSimLayerId.L1_CoarseAdvection, activationWeight, insideEgg, now)
                && CoarseGuess.ShouldUpdate(now))
            {
                CoarseGuess.Step(deltaTime, weatherSystem.wind, field);
            }

            Bounds eggBounds = egg.GetBounds();
            bool tickEggManifold = SimScheduler.ShouldTick(WeatherSimLayerId.L2_EggManifold, activationWeight, insideEgg, now)
                || insideEgg;

            if (tickEggManifold && ActivationGate.IsActive(WeatherFeatureMask.LodEggs, activationWeight, insideEgg))
            {
                manifold.SetEggLodActive(true, eggBounds);
                weatherSystem.ServiceUpdateSubsystems(deltaTime, skipManifold: true, activationWeight, insideEgg);
                if (ActivationGate.IsActive(WeatherFeatureMask.FullManifold, activationWeight, insideEgg))
                    manifold.ServiceUpdateInBounds(deltaTime, eggBounds);
            }
            else
            {
                manifold.SetEggLodActive(false, eggBounds);
            }

            if (SimScheduler.ShouldTick(WeatherSimLayerId.L3_NearFieldWind, activationWeight, insideEgg, now)
                && ActivationGate.IsActive(WeatherFeatureMask.NearFieldGraph, activationWeight, insideEgg)
                && manifold.nearFieldGraph != null)
            {
                manifold.nearFieldGraph.enabled = true;
            }
            else if (manifold.nearFieldGraph != null)
            {
                manifold.nearFieldGraph.enabled = false;
            }

            if (tickEggManifold)
                FitRegressionForEgg(egg);

            if (Time.time - _lastClientPushTime >= clientPushInterval)
            {
                _lastClientPushTime = Time.time;
                WeatherEggClientPayload payload = BuildClientPayload(egg, field);
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

        WeatherEggClientPayload BuildClientPayload(PlayerWeatherEggZone egg, EmergenceVectorField field)
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
                timeoutOrder = 0,
                emergenceChecksum = field != null ? EmergenceVectorField.ComputeChecksum(field.Vectors) : 0,
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

            if (CoarseGuess != null)
            {
                data = CoarseGuess.GuessAt(world);
                return true;
            }

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
