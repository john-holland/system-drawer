#if UNITY_EDITOR
using Planetary;
using Planetary.Bridges;
using UnityEditor;
using UnityEngine;
using Weather;

namespace Planetary.Editor
{
    /// <summary>Draws planet shell + manifold AABB when enabled on <see cref="PlanetBody"/>.</summary>
    public static class PhysicsManifoldGizmoOverlay
    {
        public static void Draw(PlanetBody planet)
        {
            if (planet == null)
                return;

            Vector3 center = planet.PlanetCenter;
            float r = planet.PlanetRadius;
            Gizmos.color = new Color(0.5f, 0.75f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, r);

            if (planet.horizonLodSettings != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.5f);
                float mid = r * 1.15f;
                Gizmos.DrawWireSphere(center, mid);
            }

            WeatherPhysicsManifold manifold = Object.FindAnyObjectByType<WeatherPhysicsManifold>();
            if (manifold != null)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
                Gizmos.DrawWireCube(manifold.worldBounds.center, manifold.worldBounds.size);
            }

            PlanetPhysicsManifoldBridge bridge = planet.GetComponentInChildren<PlanetPhysicsManifoldBridge>();
            if (bridge != null && bridge.manifold != null)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
                Gizmos.DrawWireCube(bridge.manifold.worldBounds.center, bridge.manifold.worldBounds.size);
            }
        }
    }

    [CustomEditor(typeof(PlanetBody))]
    sealed class PlanetBodyPhysicsBridgeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var body = (PlanetBody)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Physics Bridge Editor"))
                EditorApplication.ExecuteMenuItem("Window/System Drawer/Physics/Physics Bridge Editor");
            if (GUILayout.Button("Stamp Planet Physics Bridge"))
            {
                var bridge = body.GetComponentInChildren<PlanetPhysicsManifoldBridge>();
                if (bridge == null)
                    bridge = body.gameObject.AddComponent<PlanetPhysicsManifoldBridge>();
                bridge.planet = body;
                bridge.StampFromCompositionBake();
            }
        }

        void OnSceneGUI()
        {
            var body = (PlanetBody)target;
            var so = new SerializedObject(body);
            var prop = so.FindProperty("drawPhysicsBridgeGizmos");
            if (prop != null && prop.boolValue)
                PhysicsManifoldGizmoOverlay.Draw(body);
        }
    }
}
#endif
