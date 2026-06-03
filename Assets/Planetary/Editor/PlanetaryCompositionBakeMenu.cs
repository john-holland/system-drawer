using Planetary.Composition;
using Planetary.Rendering;
using Planetary.Tectonics;
using UnityEditor;
using UnityEngine;

namespace Planetary.Editor
{
    public static class PlanetaryCompositionBakeMenu
    {
        [MenuItem("Window/System Drawer/Planet/Bake Composition")]
        public static void BakeSelectedPlanet()
        {
            var body = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<PlanetBody>()
                : Object.FindFirstObjectByType<PlanetBody>();
            if (body == null)
            {
                Debug.LogWarning("No PlanetBody selected or found.");
                return;
            }
            body.RebakeComposition();
            if (body.sdfLodRenderer != null)
                body.sdfLodRenderer.Rebake();
            EditorUtility.SetDirty(body);
        }
    }

    [CustomEditor(typeof(PlanetBody))]
    public sealed class PlanetBodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var body = (PlanetBody)target;
            if (GUILayout.Button("Rebuild Planet"))
                body.RebuildAll();
            if (GUILayout.Button("Rebake SDF LOD Mesh"))
            {
                if (body.sdfLodRenderer != null)
                    body.sdfLodRenderer.Rebake();
            }
            if (GUILayout.Button("Update Interior Planet Physics"))
            {
                var updater = body.GetComponent<PlanetInteriorPhysicsUpdater>();
                if (updater != null)
                    updater.UpdateInteriorPhysics();
            }
        }
    }

    [CustomEditor(typeof(PlanetarySdfLodProfile))]
    public sealed class PlanetarySdfLodProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Rebake SDF LOD Mesh (selected planet)"))
            {
                var body = Object.FindFirstObjectByType<PlanetBody>();
                if (body != null && body.sdfLodRenderer != null)
                    body.sdfLodRenderer.Rebake();
            }
        }
    }
}
