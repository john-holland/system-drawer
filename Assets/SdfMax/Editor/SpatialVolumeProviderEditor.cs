#if UNITY_EDITOR
using Planetary;
using SpatialVolumes;
using SdfMax;
using UnityEditor;
using UnityEngine;

namespace SdfMax.Editor
{
    [CustomEditor(typeof(SpatialVolumeProvider))]
    public sealed class SpatialVolumeProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var provider = (SpatialVolumeProvider)target;
            EditorGUILayout.Space(6);

            if (provider.renderMode != SdfMaxRenderMode.None)
            {
                EditorGUILayout.LabelField("Surface mesh", EditorStyles.boldLabel);
                if (provider.renderMode == SdfMaxRenderMode.StaticMesh)
                {
                    var meshSurface = provider.GetComponent<SdfMaxMeshSurface>();
                    if (meshSurface != null && GUILayout.Button("Rebuild Surface Mesh", GUILayout.Height(22)))
                    {
                        Undo.RecordObject(meshSurface, "Rebuild Surface Mesh");
                        meshSurface.RebuildSurfaceMesh();
                    }
                }
                else if (provider.renderMode == SdfMaxRenderMode.SkinnedMesh)
                {
                    var skinned = provider.GetComponent<SdfMaxSkinnedMeshSurface>();
                    if (skinned != null)
                    {
                        if (GUILayout.Button("Rebuild Surface Mesh", GUILayout.Height(22)))
                        {
                            Undo.RecordObject(skinned, "Rebuild Surface Mesh");
                            skinned.RebuildSurfaceMesh();
                        }
                        if (GUILayout.Button("Regenerate Skin Weights", GUILayout.Height(22)))
                        {
                            Undo.RecordObject(skinned, "Regenerate Skin Weights");
                            skinned.RegenerateSkinWeights();
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (provider.backend == VolumeBackend.SdfMaxComposition)
            {
                if (GUILayout.Button("Open SDF Max Editor", GUILayout.Height(22)))
                    SdfMaxCompositionEditorWindow.ShowWindow(provider, provider.composition);
            }
            else if (GUILayout.Button("Rebuild Mesh Cache", GUILayout.Height(22)))
                SdfMaxEditorUndo.ApplyAutoCalculate(provider, provider.transform);

            if (GUILayout.Button("Rebuild Now", GUILayout.Height(22)))
            {
                var planet = provider.GetComponentInParent<PlanetBody>();
                if (planet != null)
                    planet.RebuildTectonicPlates(stepPhysics: true);
                else
                    provider.RebuildIfDirty(force: true);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
