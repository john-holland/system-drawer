using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Creates word2vec-like embeddings for behavior tree class structures.
    /// Incorporates 3D spatial distance between nodes to capture spatial relationships.
    /// </summary>
    public class BehaviorTreeEmbedder
    {
        /// <summary>
        /// Embedding vector dimensions.
        /// </summary>
        public const int EMBEDDING_DIMENSION = 128;

        /// <summary>
        /// Generate embedding vector for a behavior tree.
        /// Uses reflection to avoid direct dependency on BehaviorTree types.
        /// </summary>
        public static float[] EmbedBehaviorTree(object tree)
        {
            if (tree == null)
            {
                return new float[EMBEDDING_DIMENSION];
            }

            // Use reflection to get rootNode
            var rootNodeProp = tree.GetType().GetProperty("rootNode");
            if (rootNodeProp == null)
                return new float[EMBEDDING_DIMENSION];

            object rootNode = rootNodeProp.GetValue(tree);
            if (rootNode == null)
            {
                return new float[EMBEDDING_DIMENSION];
            }

            // Combine class structure and spatial distance features
            float[] classVec = CalculateClassStructureVector(rootNode);
            float[] spatialVec = CalculateSpatialDistanceVector(rootNode);

            // Combine embeddings
            return CombineEmbeddings(classVec, spatialVec);
        }

        /// <summary>
        /// Extract class hierarchy features from a behavior tree node.
        /// Uses reflection to avoid direct dependency on BehaviorTreeNode types.
        /// </summary>
        public static float[] CalculateClassStructureVector(object node)
        {
            float[] embedding = new float[EMBEDDING_DIMENSION];
            if (node == null)
                return embedding;

            // Feature extraction
            List<float> features = new List<float>();

            // Node type encoding (one-hot like)
            var nodeType = GetProperty<object>(node, "nodeType");
            features.AddRange(EncodeNodeType(nodeType));

            // Class hierarchy depth
            features.Add(CalculateDepth(node));

            // Parent-child relationships
            features.AddRange(EncodeParentChildRelationships(node));

            // Sibling relationships
            features.AddRange(EncodeSiblingRelationships(node));

            // Node execution order (if available)
            features.AddRange(EncodeExecutionOrder(node));

            // Class name hash features
            features.AddRange(EncodeClassName(node));

            // Pad or truncate to embedding dimension
            for (int i = 0; i < EMBEDDING_DIMENSION; i++)
            {
                if (i < features.Count)
                {
                    embedding[i] = features[i];
                }
                else
                {
                    // Use hash of node type and depth for remaining dimensions
                    embedding[i] = (float)(Math.Sin(i * 0.1 + node.GetHashCode() * 0.01) * 0.5 + 0.5);
                }
            }

            return embedding;
        }

        /// <summary>
        /// Calculate 3D spatial distance features between nodes.
        /// Uses reflection to avoid direct dependency on BehaviorTreeNode types.
        /// </summary>
        public static float[] CalculateSpatialDistanceVector(object rootNode)
        {
            float[] embedding = new float[EMBEDDING_DIMENSION];
            if (rootNode == null)
                return embedding;

            // Collect all nodes with their 3D positions
            List<NodePosition> nodePositions = new List<NodePosition>();
            CollectNodePositions(rootNode, nodePositions);

            if (nodePositions.Count == 0)
                return embedding;

            // Calculate distance features
            List<float> features = new List<float>();

            // Average position
            Vector3 avgPosition = Vector3.zero;
            foreach (var np in nodePositions)
            {
                avgPosition += np.position;
            }
            avgPosition /= nodePositions.Count;
            features.Add(avgPosition.x);
            features.Add(avgPosition.y);
            features.Add(avgPosition.z);

            // Position variance
            Vector3 variance = Vector3.zero;
            foreach (var np in nodePositions)
            {
                Vector3 diff = np.position - avgPosition;
                variance += new Vector3(diff.x * diff.x, diff.y * diff.y, diff.z * diff.z);
            }
            variance /= nodePositions.Count;
            features.Add(variance.x);
            features.Add(variance.y);
            features.Add(variance.z);

            // Distances between connected nodes
            features.AddRange(CalculateConnectedNodeDistances(rootNode, nodePositions));

            // Minimum and maximum distances
            float minDist = float.MaxValue;
            float maxDist = 0f;
            for (int i = 0; i < nodePositions.Count; i++)
            {
                for (int j = i + 1; j < nodePositions.Count; j++)
                {
                    float dist = Vector3.Distance(nodePositions[i].position, nodePositions[j].position);
                    minDist = Mathf.Min(minDist, dist);
                    maxDist = Mathf.Max(maxDist, dist);
                }
            }
            features.Add(minDist);
            features.Add(maxDist);

            // Distance histogram (bins)
            features.AddRange(CalculateDistanceHistogram(nodePositions, 10));

            // Pad or truncate to embedding dimension
            for (int i = 0; i < EMBEDDING_DIMENSION; i++)
            {
                if (i < features.Count)
                {
                    embedding[i] = features[i];
                }
                else
                {
                    // Use hash-based features for remaining dimensions
                    embedding[i] = (float)(Math.Sin(i * 0.1 + rootNode.GetHashCode() * 0.01) * 0.5 + 0.5);
                }
            }

            return embedding;
        }

        /// <summary>
        /// Combine class structure and spatial distance embeddings.
        /// </summary>
        public static float[] CombineEmbeddings(float[] classVec, float[] spatialVec)
        {
            float[] combined = new float[EMBEDDING_DIMENSION];

            // Weighted combination (can be tuned)
            float classWeight = 0.6f;
            float spatialWeight = 0.4f;

            for (int i = 0; i < EMBEDDING_DIMENSION; i++)
            {
                float classVal = i < classVec.Length ? classVec[i] : 0f;
                float spatialVal = i < spatialVec.Length ? spatialVec[i] : 0f;
                combined[i] = classVal * classWeight + spatialVal * spatialWeight;
            }

            // Normalize
            float magnitude = 0f;
            for (int i = 0; i < combined.Length; i++)
            {
                magnitude += combined[i] * combined[i];
            }
            magnitude = Mathf.Sqrt(magnitude);

            if (magnitude > 0.0001f)
            {
                for (int i = 0; i < combined.Length; i++)
                {
                    combined[i] /= magnitude;
                }
            }

            return combined;
        }

        #region Reflection Helpers

        /// <summary>
        /// Get property value using reflection.
        /// </summary>
        private static T GetProperty<T>(object obj, string propertyName, T defaultValue = default(T))
        {
            if (obj == null) return defaultValue;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanRead)
            {
                var value = prop.GetValue(obj);
                if (value is T)
                    return (T)value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get field value using reflection.
        /// </summary>
        private static T GetField<T>(object obj, string fieldName, T defaultValue = default(T))
        {
            if (obj == null) return defaultValue;
            var field = obj.GetType().GetField(fieldName);
            if (field != null)
            {
                var value = field.GetValue(obj);
                if (value is T)
                    return (T)value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get transform position from MonoBehaviour.
        /// </summary>
        private static Vector3 GetPosition(object obj)
        {
            if (obj == null) return Vector3.zero;
            var transformProp = obj.GetType().GetProperty("transform");
            if (transformProp != null)
            {
                var transform = transformProp.GetValue(obj);
                if (transform != null)
                {
                    var positionProp = transform.GetType().GetProperty("position");
                    if (positionProp != null)
                    {
                        var pos = positionProp.GetValue(transform);
                        if (pos is Vector3)
                            return (Vector3)pos;
                    }
                }
            }
            return Vector3.zero;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Encode node type as feature vector.
        /// </summary>
        private static float[] EncodeNodeType(object nodeTypeObj)
        {
            float[] encoding = new float[5]; // One for each NodeType
            if (nodeTypeObj != null)
            {
                int nodeTypeInt = Convert.ToInt32(nodeTypeObj);
                if (nodeTypeInt >= 0 && nodeTypeInt < encoding.Length)
                {
                    encoding[nodeTypeInt] = 1f;
                }
            }
            return encoding;
        }

        /// <summary>
        /// Calculate depth of node in tree.
        /// </summary>
        private static float CalculateDepth(object node)
        {
            if (node == null)
                return 0f;

            float maxChildDepth = 0f;
            var children = GetProperty<System.Collections.IList>(node, "children");
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        maxChildDepth = Mathf.Max(maxChildDepth, CalculateDepth(child));
                    }
                }
            }

            return 1f + maxChildDepth;
        }

        /// <summary>
        /// Encode parent-child relationships.
        /// </summary>
        private static float[] EncodeParentChildRelationships(object node)
        {
            List<float> features = new List<float>();

            var children = GetProperty<System.Collections.IList>(node, "children");
            if (children != null && children.Count > 0)
            {
                // Number of children
                features.Add(children.Count);

                // Child node types
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        var childNodeType = GetProperty<object>(child, "nodeType");
                        features.Add(Convert.ToInt32(childNodeType));
                    }
                }

                // Average child depth
                float avgChildDepth = 0f;
                int childCount = 0;
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        avgChildDepth += CalculateDepth(child);
                        childCount++;
                    }
                }
                if (childCount > 0)
                {
                    features.Add(avgChildDepth / childCount);
                }
            }
            else
            {
                features.Add(0f); // No children
            }

            return features.ToArray();
        }

        /// <summary>
        /// Encode sibling relationships.
        /// </summary>
        private static float[] EncodeSiblingRelationships(object node)
        {
            List<float> features = new List<float>();

            // Find parent (would need tree traversal, simplified here)
            // For now, encode based on node's position in children list
            features.Add(0f); // Placeholder for sibling index

            return features.ToArray();
        }

        /// <summary>
        /// Encode execution order features.
        /// </summary>
        private static float[] EncodeExecutionOrder(object node)
        {
            List<float> features = new List<float>();

            // Node execution order (if tracked)
            features.Add(0f); // Placeholder

            // Estimated duration
            var estimatedDuration = GetProperty<float>(node, "estimatedDuration", 0f);
            features.Add(estimatedDuration);

            return features.ToArray();
        }

        /// <summary>
        /// Encode class name as features.
        /// </summary>
        private static float[] EncodeClassName(object node)
        {
            List<float> features = new List<float>();

            string className = node.GetType().Name;
            int hash = className.GetHashCode();

            // Use hash to generate features
            features.Add((hash % 1000) / 1000f);
            features.Add((Math.Abs(hash) % 100) / 100f);
            features.Add(className.Length / 100f);

            return features.ToArray();
        }

        /// <summary>
        /// Collect all nodes with their 3D world positions.
        /// </summary>
        private static void CollectNodePositions(object node, List<NodePosition> positions)
        {
            if (node == null)
                return;

            // Get world position from GameObject
            Vector3 position = GetPosition(node);
            positions.Add(new NodePosition { node = node, position = position });

            // Recursively collect children
            var children = GetProperty<System.Collections.IList>(node, "children");
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        CollectNodePositions(child, positions);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate distances between connected nodes (parent-child, siblings).
        /// </summary>
        private static float[] CalculateConnectedNodeDistances(object node, List<NodePosition> nodePositions)
        {
            List<float> distances = new List<float>();

            var children = GetProperty<System.Collections.IList>(node, "children");
            if (children != null && children.Count > 0)
            {
                Vector3 parentPos = GetPosition(node);

                foreach (var child in children)
                {
                    if (child != null)
                    {
                        Vector3 childPos = GetPosition(child);
                        float dist = Vector3.Distance(parentPos, childPos);
                        distances.Add(dist);
                    }
                }

                // Sibling distances
                for (int i = 0; i < children.Count; i++)
                {
                    for (int j = i + 1; j < children.Count; j++)
                    {
                        var child1 = children[i];
                        var child2 = children[j];
                        if (child1 != null && child2 != null)
                        {
                            Vector3 pos1 = GetPosition(child1);
                            Vector3 pos2 = GetPosition(child2);
                            float dist = Vector3.Distance(pos1, pos2);
                            distances.Add(dist);
                        }
                    }
                }
            }

            return distances.ToArray();
        }

        /// <summary>
        /// Calculate distance histogram.
        /// </summary>
        private static float[] CalculateDistanceHistogram(List<NodePosition> nodePositions, int bins)
        {
            float[] histogram = new float[bins];

            if (nodePositions.Count < 2)
                return histogram;

            // Find min and max distances
            float minDist = float.MaxValue;
            float maxDist = 0f;

            for (int i = 0; i < nodePositions.Count; i++)
            {
                for (int j = i + 1; j < nodePositions.Count; j++)
                {
                    float dist = Vector3.Distance(nodePositions[i].position, nodePositions[j].position);
                    minDist = Mathf.Min(minDist, dist);
                    maxDist = Mathf.Max(maxDist, dist);
                }
            }

            if (maxDist - minDist < 0.0001f)
                return histogram;

            // Bin distances
            for (int i = 0; i < nodePositions.Count; i++)
            {
                for (int j = i + 1; j < nodePositions.Count; j++)
                {
                    float dist = Vector3.Distance(nodePositions[i].position, nodePositions[j].position);
                    int bin = Mathf.FloorToInt((dist - minDist) / (maxDist - minDist) * bins);
                    bin = Mathf.Clamp(bin, 0, bins - 1);
                    histogram[bin]++;
                }
            }

            // Normalize
            float total = histogram.Sum();
            if (total > 0f)
            {
                for (int i = 0; i < bins; i++)
                {
                    histogram[i] /= total;
                }
            }

            return histogram;
        }

        /// <summary>
        /// Helper class for node positions.
        /// </summary>
        private class NodePosition
        {
            public object node;
            public Vector3 position;
        }

        #endregion
    }
}
