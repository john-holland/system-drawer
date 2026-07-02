using Planetary.AsteroidBelt;
using UnityEditor;
using UnityEngine;

namespace Planetary.Editor
{
    public sealed class AsteroidBeltEditorWindow : EditorWindow
    {
        AsteroidBeltHost _host;
        Vector2 _scroll;

        [MenuItem("Window/System Drawer/Planet/Asteroid Belt")]
        public static void Open() => GetWindow<AsteroidBeltEditorWindow>("Asteroid Belt");

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _host = (AsteroidBeltHost)EditorGUILayout.ObjectField("Belt Host", _host, typeof(AsteroidBeltHost), true);
            if (_host == null)
            {
                if (GUILayout.Button("Create Belt Host In Scene"))
                {
                    var go = new GameObject("AsteroidBeltHost");
                    _host = go.AddComponent<AsteroidBeltHost>();
                    _host.EnsureComponents();
                    Selection.activeGameObject = go;
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            _host.EnsureComponents();
            var manifold = _host.manifold;
            if (manifold != null)
            {
                manifold.innerRadiusM = EditorGUILayout.FloatField("Inner Radius (m)", manifold.innerRadiusM);
                manifold.outerRadiusM = EditorGUILayout.FloatField("Outer Radius (m)", manifold.outerRadiusM);
                manifold.meanDensity = EditorGUILayout.Slider("Mean Density", manifold.meanDensity, 0f, 1f);
                manifold.densityVariance = EditorGUILayout.Slider("Density Variance", manifold.densityVariance, 0f, 1f);
                EditorGUI.BeginChangeCheck();
                manifold.seed = EditorGUILayout.IntField("Belt Seed", manifold.seed);
                if (EditorGUI.EndChangeCheck() && _host.mutationLog != null)
                    _host.mutationLog.beltSeed = manifold.seed;
            }

            if (GUILayout.Button("Rebuild Disc Mesh") && _host.discRenderer != null)
                _host.discRenderer.RebuildMesh();

            if (GUILayout.Button("Regenerate Belt Seed") && manifold != null)
            {
                manifold.seed = Random.Range(1, int.MaxValue);
                if (_host.mutationLog != null)
                    _host.mutationLog.beltSeed = manifold.seed;
            }

            if (_host.mutationLog != null)
            {
                EditorGUILayout.LabelField($"Mutations: {_host.mutationLog.mutations.Count}");
                if (GUILayout.Button("Clear Mutation Log"))
                    _host.mutationLog.Clear();
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI()
        {
            if (_host == null || _host.manifold == null)
                return;
            Vector3 c = _host.manifold.parentPlanet != null
                ? _host.manifold.parentPlanet.position
                : _host.transform.position;
            Handles.color = new Color(0.6f, 0.5f, 0.3f, 0.35f);
            Handles.DrawWireDisc(c, Vector3.up, _host.manifold.innerRadiusM);
            Handles.DrawWireDisc(c, Vector3.up, _host.manifold.outerRadiusM);
        }
    }
}
