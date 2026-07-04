#if UNITY_EDITOR
using Locomotion.Drink;
using Locomotion.Drink.Flow;
using Locomotion.Liquid;
using UnityEditor;
using UnityEngine;
using Weather;

namespace Locomotion.Drink.Editor
{
    public sealed class DrinkFlowBakeWindow : EditorWindow
    {
        DrinkFlowModel _model;
        LiquidWeatherManifoldBridge _bridge;
        WeatherPhysicsManifold _manifold;
        float _duration = 1f;
        float _step = 0.05f;
        DrinkFlowBakeAsset _lastBake;

        [MenuItem("Window/Continuum/Drink Flow Bake")]
        public static void Open()
        {
            GetWindow<DrinkFlowBakeWindow>("Drink Flow Bake");
        }

        void OnEnable()
        {
            if (_manifold == null)
                _manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            if (_bridge == null)
                _bridge = FindAnyObjectByType<LiquidWeatherManifoldBridge>();
        }

        void OnGUI()
        {
            _model = (DrinkFlowModel)EditorGUILayout.ObjectField("Flow model", _model, typeof(DrinkFlowModel), true);
            _manifold = (WeatherPhysicsManifold)EditorGUILayout.ObjectField(
                "Weather manifold", _manifold, typeof(WeatherPhysicsManifold), true);
            _bridge = (LiquidWeatherManifoldBridge)EditorGUILayout.ObjectField(
                "Manifold bridge", _bridge, typeof(LiquidWeatherManifoldBridge), true);
            _duration = EditorGUILayout.FloatField("Duration (s)", _duration);
            _step = EditorGUILayout.FloatField("Step (s)", _step);

            if (GUILayout.Button("Find scene weather") && _manifold == null)
                _manifold = FindAnyObjectByType<WeatherPhysicsManifold>();

            if (GUILayout.Button("Bake") && _model != null)
            {
                if (_bridge == null && _manifold != null)
                {
                    var go = new GameObject("DrinkFlowBakeBridge");
                    _bridge = go.AddComponent<LiquidWeatherManifoldBridge>();
                    _bridge.manifold = _manifold;
                }
                if (_bridge != null)
                    _bridge.manifold = _manifold;

                var solver = new DrinkFlowBakeSolver();
                _lastBake = solver.Bake(_model, _duration, _step, _bridge);
                if (_lastBake != null)
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save flow bake", "DrinkFlowBake", "asset", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(_lastBake, path);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            if (_lastBake != null)
                EditorGUILayout.LabelField("Last bake flow keys: " + _lastBake.flowLitersPerSecond.length);
        }
    }
}
#endif
