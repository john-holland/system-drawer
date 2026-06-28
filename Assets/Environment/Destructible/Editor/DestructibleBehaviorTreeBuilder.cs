#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DestructibleEnvironment.Editor
{
    public static class DestructibleBehaviorTreeBuilder
    {
        public static GameObject BuildPlaybackHierarchy(
            DestructibleBakeAsset bake,
            IList<int> activePieceIds,
            string rootName = "DestructiblePlayback")
        {
            var root = new GameObject(rootName);
            var playback = root.AddComponent<DestructiblePlaybackController>();
            var bt = root.AddComponent<BehaviorTree>();
            bt.decisionTime = 0f;

            var sequence = root.AddComponent<DestructibleFallSequenceNode>();
            bt.rootNode = sequence;
            playback.behaviorTree = bt;
            sequence.playback = playback;

            var children = new List<BehaviorTreeNode>();
            if (bake != null && bake.fallOrder != null)
            {
                var activeSet = activePieceIds != null ? new HashSet<int>(activePieceIds) : null;
                for (int i = 0; i < bake.fallOrder.Length; i++)
                {
                    int pieceId = bake.fallOrder[i];
                    if (activeSet != null && !activeSet.Contains(pieceId))
                        continue;

                    var childGo = new GameObject($"PieceFall_{pieceId}");
                    childGo.transform.SetParent(root.transform, false);
                    var node = childGo.AddComponent<DestructiblePieceFallNode>();
                    node.pieceId = pieceId;
                    node.playback = playback;
                    children.Add(node);
                }
            }

            sequence.children = children;
            return root;
        }

        public static void AttachToDestructible(
            DestructibleEnvironmentMeshRenderer destructible,
            DestructibleBakeAsset bake)
        {
            if (destructible == null || bake == null)
                return;

            Transform existing = destructible.transform.Find("DestructiblePlayback");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var activeIds = new List<int>();
            for (int i = 0; i < bake.pieces.Count; i++)
                activeIds.Add(bake.pieces[i].pieceId);

            GameObject playbackRoot = BuildPlaybackHierarchy(bake, activeIds);
            playbackRoot.transform.SetParent(destructible.transform, false);

            var playback = playbackRoot.GetComponent<DestructiblePlaybackController>();
            destructible.playbackController = playback;
            playback.owner = destructible;
            playback.enabled = false;

            string prefabPath = AssetDatabase.GetAssetPath(bake);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                string dir = System.IO.Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
                string prefabName = $"{bake.name}_Playback.prefab";
                string fullPath = $"{dir}/{prefabName}";
                PrefabUtility.SaveAsPrefabAsset(playbackRoot, fullPath);
                bake.behaviorTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            }
        }
    }
}
#endif
