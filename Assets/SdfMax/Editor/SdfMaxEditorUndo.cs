#if UNITY_EDITOR
using SpatialVolumes;
using SdfMax;
using UnityEditor;
using UnityEngine;

namespace SdfMax.Editor
{
    public static class SdfMaxEditorUndo
    {
        public static void ApplyAutoCalculate(SpatialVolumeProvider provider, Transform previewRoot)
        {
            if (provider == null)
                return;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(
                provider.backend == VolumeBackend.SdfMaxComposition
                    ? "SDF Max Auto Calculate"
                    : "Spatial Volume Rebuild");

            if (provider.composition != null)
                Undo.RecordObject(provider.composition, "SDF Max Auto Calculate");
            if (provider.profile != null)
                Undo.RecordObject(provider.profile, "SDF Max Auto Calculate");
            Undo.RecordObject(provider, "SDF Max Auto Calculate");

            Transform root = previewRoot != null ? previewRoot : provider.transform;

            if (provider.backend == VolumeBackend.SdfMaxComposition)
            {
                if (provider.composition == null)
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Create SDF Max Composition",
                        "SdfMaxComposition",
                        "asset",
                        "Choose location for new composition asset.");
                    if (string.IsNullOrEmpty(path))
                    {
                        Undo.CollapseUndoOperations(group);
                        return;
                    }
                    provider.composition = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
                    AssetDatabase.CreateAsset(provider.composition, path);
                }

                SdfMaxMeshAutoSetup.ApplyToComposition(provider.composition, root, provider.profile);
            }

            SpatialVolumeCacheRegistry.EnsureBuilt(provider, force: true);

            if (provider.profile != null && provider.profile.generateSurfaceMesh)
            {
                var meshSurface = provider.GetComponent<SdfMaxMeshSurface>();
                if (meshSurface != null)
                    meshSurface.RebuildSurfaceMesh();
                var skinned = provider.GetComponent<SdfMaxSkinnedMeshSurface>();
                if (skinned != null)
                    skinned.RebuildSurfaceMesh();
            }

            if (provider.composition != null)
                EditorUtility.SetDirty(provider.composition);
            EditorUtility.SetDirty(provider);

            Undo.CollapseUndoOperations(group);
        }
    }
}
#endif
