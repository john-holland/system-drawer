#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DestructibleEnvironment.Editor
{
    public static class DestructiblePreBakeUtility
    {
        public static DestructibleBakeAsset PreBakeDestructible(DestructibleEnvironmentMeshRenderer destructible)
        {
            if (destructible == null)
                return null;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Destructible Bake",
                $"{destructible.gameObject.name}_DestructibleBake",
                "asset",
                "Choose a location for the destructible bake asset.");

            if (string.IsNullOrEmpty(path))
                return null;

            var asset = AssetDatabase.LoadAssetAtPath<DestructibleBakeAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DestructibleBakeAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            ClearEmbeddedMeshes(asset);
            destructible.EditorPreBake(asset);

            for (int i = 0; i < asset.pieces.Count; i++)
            {
                DestructiblePieceRecord piece = asset.pieces[i];
                if (piece.pieceMesh != null)
                {
                    piece.pieceMesh.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(piece.pieceMesh, asset);
                }
                asset.pieces[i] = piece;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            DestructibleBehaviorTreeBuilder.AttachToDestructible(destructible, asset);
            AssetDatabase.SaveAssets();

            destructible.bake = asset;
            EditorUtility.SetDirty(destructible);
            return asset;
        }

        static void ClearEmbeddedMeshes(DestructibleBakeAsset asset)
        {
            if (asset == null || asset.pieces == null)
                return;

            string path = AssetDatabase.GetAssetPath(asset);
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Mesh)
                    Object.DestroyImmediate(subAssets[i], true);
            }

            asset.pieces.Clear();
        }
    }

    [CustomEditor(typeof(DestructibleEnvironmentMeshRenderer))]
    public class DestructibleEnvironmentMeshRendererEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var destructible = (DestructibleEnvironmentMeshRenderer)target;
            EditorGUILayout.Space();

            if (GUILayout.Button("Pre-Bake Destructible"))
                DestructiblePreBakeUtility.PreBakeDestructible(destructible);

            if (destructible.bake != null)
            {
                EditorGUILayout.LabelField("Bake Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Pieces", destructible.bake.PieceCount.ToString());
                EditorGUILayout.LabelField("Pool Slots", destructible.bake.poolSlotCount.ToString());
            }

            Validate(destructible);
        }

        static void Validate(DestructibleEnvironmentMeshRenderer destructible)
        {
            if (destructible.bake == null)
                return;

            if (destructible.bake.PieceCount > 0 &&
                destructible.sourceColliders != null &&
                destructible.sourceColliders.Length > 0 &&
                destructible.sourceColliders[0] != null &&
                destructible.sourceColliders[0].sharedMaterial == null)
            {
                EditorGUILayout.HelpBox("Source collider has no PhysicMaterial; break strength will use profile defaults only.", MessageType.Warning);
            }

            if (destructible.bake.PieceCount > destructible.bake.poolSlotCount)
            {
                EditorGUILayout.HelpBox("Piece count exceeds pool slot count. Re-bake or increase pool slots.", MessageType.Error);
            }
        }

        void OnSceneGUI()
        {
            var destructible = (DestructibleEnvironmentMeshRenderer)target;
            if (destructible.bake == null || destructible.bake.pieces == null)
                return;

            Matrix4x4 ltw = destructible.transform.localToWorldMatrix;
            Handles.color = new Color(1f, 0.5f, 0.1f, 0.35f);
            for (int i = 0; i < destructible.bake.pieces.Count; i++)
            {
                DestructiblePieceRecord piece = destructible.bake.pieces[i];
                Bounds wb = TransformBounds(piece.localBounds, ltw);
                Handles.DrawWireCube(wb.center, wb.size);

                Vector3 centroid = ltw.MultiplyPoint3x4(piece.localCentroid);
                Vector3 down = destructible.gravityDir.sqrMagnitude > 1e-6f
                    ? destructible.gravityDir.normalized
                    : Vector3.down;
                Handles.DrawLine(centroid, centroid + down * piece.groundRayMaxDistance);
            }
        }

        static Bounds TransformBounds(Bounds localBounds, Matrix4x4 ltw)
        {
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            var corners = new Vector3[8];
            int n = 0;
            for (int ix = -1; ix <= 1; ix += 2)
            for (int iy = -1; iy <= 1; iy += 2)
            for (int iz = -1; iz <= 1; iz += 2)
                corners[n++] = ltw.MultiplyPoint3x4(c + Vector3.Scale(e, new Vector3(ix, iy, iz)));

            var world = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                world.Encapsulate(corners[i]);
            return world;
        }
    }

    public static class DestructiblePreBakeMenu
    {
        [MenuItem("Environment/Destructible/Pre-Bake Selected")]
        static void PreBakeSelected()
        {
            GameObject[] selection = Selection.gameObjects;
            for (int i = 0; i < selection.Length; i++)
            {
                var destructible = selection[i].GetComponent<DestructibleEnvironmentMeshRenderer>();
                if (destructible == null)
                    destructible = selection[i].AddComponent<DestructibleEnvironmentMeshRenderer>();
                DestructiblePreBakeUtility.PreBakeDestructible(destructible);
            }
        }

        [MenuItem("Environment/Destructible/Pre-Bake Selected", true)]
        static bool PreBakeSelectedValidate() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }
}
#endif
