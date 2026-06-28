#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Weather.Executor;

namespace Weather.CloudBake.Editor
{
    public sealed class CloudPerspectiveBakeWindow : EditorWindow
    {
        CloudViewerSpec _viewer = new CloudViewerSpec();
        CloudPerspectiveTarget _target = new CloudPerspectiveTarget();
        CloudPerspectiveBakeConfig _config = new CloudPerspectiveBakeConfig();
        WeatherPhysicsManifold _manifold;
        Wind _wind;
        Cloud _cloud;
        Water _water;
        WeatherExecutorService _executor;

        Texture2D _previewTexture;
        string _gradientText = "top=#87ceeb mid=#bfd9f2 bottom=#8c9099";
        string _videoPath;
        int _videoFrameIndex;
        int _videoFrameCount = 10;
        CloudPerspectiveBakeSolver.BakeResult _lastResult;
        List<CloudColumnSample> _lastColumns;
        Vector2 _scroll;

        [MenuItem("Window/Weather/Cloud Perspective Bake")]
        public static void Open()
        {
            GetWindow<CloudPerspectiveBakeWindow>("Cloud Bake");
        }

        void OnEnable()
        {
            _viewer.kind = CloudViewerKind.Camera;
            _viewer.camera = Camera.main;
            AutoResolveScene();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void AutoResolveScene()
        {
            _manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            _wind = FindAnyObjectByType<Wind>();
            _cloud = FindAnyObjectByType<Cloud>();
            _water = FindAnyObjectByType<Water>();
            _executor = FindAnyObjectByType<WeatherExecutorService>();
            if (_viewer.camera == null)
                _viewer.camera = Camera.main;
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Cloud Perspective Advection Bake", EditorStyles.boldLabel);
            AutoResolveScene();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Viewer", EditorStyles.boldLabel);
            _viewer.kind = (CloudViewerKind)EditorGUILayout.EnumPopup("Kind", _viewer.kind);
            _viewer.camera = (Camera)EditorGUILayout.ObjectField("Camera", _viewer.camera, typeof(Camera), true);
            _target.sampleStride = EditorGUILayout.IntField("Sample Stride", _target.sampleStride);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reference", EditorStyles.boldLabel);
            _previewTexture = (Texture2D)EditorGUILayout.ObjectField("Photo Texture", _previewTexture, typeof(Texture2D), false);
            _gradientText = EditorGUILayout.TextField("Gradient", _gradientText);
            _videoPath = EditorGUILayout.TextField("Video Path", _videoPath);
            _videoFrameIndex = EditorGUILayout.IntField("Video Frame Index", _videoFrameIndex);
            _videoFrameCount = EditorGUILayout.IntField("Video Frame Count", _videoFrameCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bake Config", EditorStyles.boldLabel);
            _config.maxIterations = EditorGUILayout.IntSlider("Max Iterations", _config.maxIterations, 1, 128);
            _config.spheresPerColumn = EditorGUILayout.IntSlider("Spheres / Column", _config.spheresPerColumn, 1, 8);
            _config.allowFloatAway = EditorGUILayout.Toggle("Allow Float Away (weather advection)", _config.allowFloatAway);
            _config.useExecutorAdvection = EditorGUILayout.Toggle("Use Executor Advection", _config.useExecutorAdvection);
            _config.advectionDeltaTime = EditorGUILayout.FloatField("Advection Delta Time", _config.advectionDeltaTime);
            _config.warmStartScalarsOnly = EditorGUILayout.Toggle("Warm-start scalars only (video)", _config.warmStartScalarsOnly);
            _config.sigmaMax = EditorGUILayout.Slider("Sigma Max", _config.sigmaMax, 0f, 2f);
            _config.noiseGamma = EditorGUILayout.Slider("Noise Gamma", _config.noiseGamma, 0.5f, 4f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Half-Shell Convexion", EditorStyles.boldLabel);
            _config.convexion.bias = EditorGUILayout.Slider("Convexion Bias (Forward ← → Back)", _config.convexion.bias, -1f, 1f);
            _config.convexion.size = EditorGUILayout.Slider("Convexion Size", _config.convexion.size, 0f, 1f);
            if (GUILayout.Button("Preview Convexion"))
                PreviewConvexion();

            EditorGUILayout.Space();
            _manifold = (WeatherPhysicsManifold)EditorGUILayout.ObjectField("Manifold", _manifold, typeof(WeatherPhysicsManifold), true);
            _wind = (Wind)EditorGUILayout.ObjectField("Wind", _wind, typeof(Wind), true);
            _cloud = (Cloud)EditorGUILayout.ObjectField("Cloud", _cloud, typeof(Cloud), true);
            _water = (Water)EditorGUILayout.ObjectField("Water", _water, typeof(Water), true);

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake Photo"))
                RunBakePhoto();
            if (GUILayout.Button("Bake Video Sequence"))
                RunBakeVideo();
            if (GUILayout.Button("Apply Last Result To Scene"))
                ApplyLastToScene();

            if (_lastResult.lossHistory != null && _lastResult.lossHistory.Count > 0)
            {
                EditorGUILayout.LabelField($"Final loss: {_lastResult.finalLoss:F4}");
                EditorGUILayout.LabelField($"Iterations: {_lastResult.lossHistory.Count}");
            }

            EditorGUILayout.EndScrollView();
        }

        void RunBakePhoto()
        {
            PrepareTarget();
            float baseAlt = _cloud != null ? _cloud.altitude.x : 1000f;
            float topAlt = _cloud != null ? _cloud.altitude.y : 2000f;
            var columns = CloudPerspectiveRaycaster.SampleColumns(_viewer, _target, baseAlt, topAlt);
            _lastColumns = columns;
            var solver = new CloudPerspectiveBakeSolver();
            _lastResult = solver.Bake(_viewer, _target, columns, _manifold, _wind, _cloud, _water, _config, null, _executor);
            Debug.Log($"[CloudBake] Photo bake complete. Loss={_lastResult.finalLoss:F4} spheres={_lastResult.stack?.spheres.Count ?? 0}");
        }

        void RunBakeVideo()
        {
            if (string.IsNullOrEmpty(_videoPath) || !File.Exists(_videoPath))
            {
                Debug.LogWarning("[CloudBake] Video path invalid.");
                return;
            }

            CloudHalfShellStack warm = null;
            var timeline = new List<string>();
            for (int f = 0; f < _videoFrameCount; f++)
            {
                _videoFrameIndex = f;
                PrepareTarget();
                float baseAlt = _cloud != null ? _cloud.altitude.x : 1000f;
                float topAlt = _cloud != null ? _cloud.altitude.y : 2000f;
                var columns = CloudPerspectiveRaycaster.SampleColumns(_viewer, _target, baseAlt, topAlt);
                _lastColumns = columns;
                var solver = new CloudPerspectiveBakeSolver();
                _lastResult = solver.Bake(_viewer, _target, columns, _manifold, _wind, _cloud, _water, _config, warm, _executor, f);
                if (_config.allowFloatAway || _config.warmStartScalarsOnly)
                    warm = _lastResult.stack;
                int anchorHash = _lastResult.anchor?.ComputeHash() ?? 0;
                float cb = _config.convexion.bias;
                float cs = _config.convexion.size;
                timeline.Add($"{{\"frame\":{f},\"loss\":{_lastResult.finalLoss},\"allowFloatAway\":{(_config.allowFloatAway ? "true" : "false")},\"anchorHash\":{anchorHash},\"convexion\":{{\"bias\":{cb},\"size\":{cs}}}}}");
            }

            var outPath = Path.Combine(Application.dataPath, "../cloud_bake_timeline.json");
            File.WriteAllText(outPath, "[\n" + string.Join(",\n", timeline) + "\n]");
            Debug.Log($"[CloudBake] Video bake wrote {outPath}");
        }

        void PrepareTarget()
        {
            _target.referenceTexture = _previewTexture;
            _target.videoPath = _videoPath;
            _target.frameIndex = _videoFrameIndex;
            _target.gradientBands = CloudGradientBands.Parse(_gradientText);
            _target.sampleStride = Mathf.Max(1, _target.sampleStride);
        }

        void ApplyLastToScene()
        {
            if (_lastResult.stack == null || _manifold == null)
                return;
            CloudHalfShellBuilder.PaintIntoManifold(_lastResult.stack, _manifold, _water);
            if (_cloud != null)
            {
                _cloud.coverage = Mathf.Clamp(_cloud.coverage + 5f, 0f, 100f);
            }
            Debug.Log("[CloudBake] Applied manifold paint from last bake.");
        }

        void PreviewConvexion()
        {
            if (_lastColumns == null || _lastColumns.Count == 0)
            {
                PrepareTarget();
                float baseAlt = _cloud != null ? _cloud.altitude.x : 1000f;
                float topAlt = _cloud != null ? _cloud.altitude.y : 2000f;
                _lastColumns = CloudPerspectiveRaycaster.SampleColumns(_viewer, _target, baseAlt, topAlt);
            }
            if (_lastColumns.Count == 0)
            {
                Debug.LogWarning("[CloudBake] No columns available for convexion preview.");
                return;
            }

            float cloudBase = _cloud != null ? _cloud.altitude.x : 1000f;
            float cloudTop = _cloud != null ? _cloud.altitude.y : 2000f;
            var stack = CloudHalfShellBuilder.Build(
                _lastColumns, _manifold, cloudBase, cloudTop,
                _config.spheresPerColumn, _viewer, _config.convexion);
            _lastResult = new CloudPerspectiveBakeSolver.BakeResult { stack = stack };
            SceneView.RepaintAll();
            Debug.Log($"[CloudBake] Convexion preview: bias={_config.convexion.bias:F2} size={_config.convexion.size:F2} spheres={stack.spheres.Count}");
        }

        void OnSceneGUI(SceneView view)
        {
            if (_lastResult.stack == null || _lastResult.stack.spheres.Count == 0)
                return;
            var stack = _lastResult.stack;
            Vector3 viewDir = _viewer.ResolveForward();
            float horiz = _config.convexion.size * Mathf.Max(stack.shellBounds.extents.x, stack.shellBounds.extents.z, 1f);
            CloudHalfShellConvexionUtility.DrawGizmo(
                stack.shellCentroid != Vector3.zero ? stack.shellCentroid : stack.shellBounds.center,
                viewDir,
                _config.convexion,
                stack.cloudBaseM,
                stack.cloudTopM,
                horiz);
            Handles.color = new Color(0.6f, 0.85f, 1f, 0.35f);
            foreach (var sphere in stack.spheres)
                Handles.SphereHandleCap(0, sphere.center, Quaternion.identity, sphere.radius * 2f, EventType.Repaint);
        }
    }
}
#endif
