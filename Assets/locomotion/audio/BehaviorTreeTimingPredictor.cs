using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Analyzes behavior trees to predict sound effect timing.
    /// Uses narrative timeline data to estimate when sounds should occur.
    /// Uses reflection to avoid direct dependency on Narrative.Runtime.
    /// </summary>
    public class BehaviorTreeTimingPredictor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to narrative calendar (MonoBehaviour, resolved via reflection)")]
        public MonoBehaviour narrativeCalendar;

        [Header("Prediction Settings")]
        [Tooltip("Use behavior tree duration estimation")]
        public bool useDurationEstimation = true;

        [Tooltip("Use narrative timeline events")]
        public bool useTimelineEvents = true;

        [Tooltip("Default timing if prediction fails (seconds)")]
        public float defaultTiming = 0f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        private void Awake()
        {
            // Auto-find narrative calendar if not assigned (using reflection)
            if (narrativeCalendar == null)
            {
                var narrativeCalendarType = System.Type.GetType("Locomotion.Narrative.NarrativeCalendarAsset, Locomotion.Narrative.Runtime");
                if (narrativeCalendarType != null)
                {
                    var found = FindObjectOfType(narrativeCalendarType);
                    if (found != null)
                    {
                        narrativeCalendar = found as MonoBehaviour;
                    }
                }
            }
        }

        /// <summary>
        /// Predict sound timing for a behavior tree using narrative calendar.
        /// Uses reflection to avoid direct dependency on BehaviorTree and Narrative types.
        /// </summary>
        public float PredictSoundTiming(object tree, object calendar)
        {
            if (tree == null)
                return defaultTiming;

            if (calendar == null)
            {
                calendar = narrativeCalendar;
            }

            // Convert calendar to proper type if needed
            if (calendar != null && !(calendar is MonoBehaviour))
            {
                // Try to get as MonoBehaviour
                calendar = calendar as MonoBehaviour;
            }

            float predictedTime = defaultTiming;

            // Method 1: Use behavior tree duration estimation
            if (useDurationEstimation)
            {
                var rootNodeProp = tree.GetType().GetProperty("rootNode");
                if (rootNodeProp != null)
                {
                    object rootNode = rootNodeProp.GetValue(tree);
                    if (rootNode != null)
                    {
                        var calculateDurationMethod = rootNode.GetType().GetMethod("CalculateDuration");
                        if (calculateDurationMethod != null)
                        {
                            var result = calculateDurationMethod.Invoke(rootNode, null);
                            if (result is float)
                            {
                                predictedTime = (float)result;
                            }
                        }
                        if (enableDebugLogging)
                        {
                            Debug.Log($"[BehaviorTreeTimingPredictor] Duration-based prediction: {predictedTime}s");
                        }
                    }
                }
            }

            // Method 2: Use narrative timeline events
            if (useTimelineEvents && calendar != null)
            {
                float timelinePrediction = PredictFromTimeline(tree, calendar);
                if (timelinePrediction > 0f)
                {
                    predictedTime = timelinePrediction;
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[BehaviorTreeTimingPredictor] Timeline-based prediction: {predictedTime}s");
                    }
                }
            }

            // Method 3: Extract sound nodes and calculate timing
            var soundNodes = ExtractSoundNodes(tree);
            if (soundNodes.Count > 0)
            {
                float soundNodeTiming = CalculateNodeTiming(soundNodes[0]);
                if (soundNodeTiming > 0f)
                {
                    predictedTime = soundNodeTiming;
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[BehaviorTreeTimingPredictor] Sound node-based prediction: {predictedTime}s");
                    }
                }
            }

            return predictedTime;
        }

        /// <summary>
        /// Extract all sound-related nodes from a behavior tree.
        /// Uses reflection to avoid direct dependency on BehaviorTree types.
        /// </summary>
        public List<object> ExtractSoundNodes(object tree)
        {
            List<object> soundNodes = new List<object>();

            if (tree == null)
                return soundNodes;

            var rootNodeProp = tree.GetType().GetProperty("rootNode");
            if (rootNodeProp == null)
                return soundNodes;

            object rootNode = rootNodeProp.GetValue(tree);
            if (rootNode == null)
                return soundNodes;

            // Recursively search for sound nodes
            ExtractSoundNodesRecursive(rootNode, soundNodes);

            return soundNodes;
        }

        /// <summary>
        /// Recursively extract sound nodes.
        /// </summary>
        private void ExtractSoundNodesRecursive(object node, List<object> soundNodes)
        {
            if (node == null)
                return;

            // Check if this is a sound node (SaySoundNode or similar)
            string nodeTypeName = node.GetType().Name;
            if (nodeTypeName.Contains("Sound") || nodeTypeName.Contains("Audio"))
            {
                soundNodes.Add(node);
            }

            // Recursively check children
            var childrenProp = node.GetType().GetProperty("children");
            if (childrenProp != null)
            {
                var children = childrenProp.GetValue(node) as System.Collections.IList;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child != null)
                        {
                            ExtractSoundNodesRecursive(child, soundNodes);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate timing for a specific node.
        /// Uses reflection to avoid direct dependency on BehaviorTreeNode types.
        /// </summary>
        public float CalculateNodeTiming(object node)
        {
            if (node == null)
                return defaultTiming;

            // Use estimated duration if available
            var estimatedDurationProp = node.GetType().GetProperty("estimatedDuration");
            if (estimatedDurationProp != null)
            {
                var estimatedDuration = estimatedDurationProp.GetValue(node);
                if (estimatedDuration is float && (float)estimatedDuration > 0f)
                {
                    return (float)estimatedDuration;
                }
            }

            // Calculate from children (for sequence nodes)
            var nodeTypeProp = node.GetType().GetProperty("nodeType");
            if (nodeTypeProp != null)
            {
                var nodeType = nodeTypeProp.GetValue(node);
                // Check if it's Sequence (typically enum value 0 or 1, but we'll check by name)
                string nodeTypeName = nodeType != null ? nodeType.ToString() : "";
                if (nodeTypeName.Contains("Sequence"))
                {
                    var childrenProp = node.GetType().GetProperty("children");
                    if (childrenProp != null)
                    {
                        var children = childrenProp.GetValue(node) as System.Collections.IList;
                        if (children != null)
                        {
                            float totalTime = 0f;
                            foreach (var child in children)
                            {
                                if (child != null)
                                {
                                    totalTime += CalculateNodeTiming(child);
                                }
                            }
                            return totalTime;
                        }
                    }
                }
            }

            // Use default timing
            return defaultTiming;
        }

        /// <summary>
        /// Predict timing from narrative timeline events.
        /// Uses reflection to avoid direct dependency on Narrative types.
        /// </summary>
        private float PredictFromTimeline(object tree, object calendar)
        {
            if (calendar == null)
                return 0f;

            // Try to find events that match the behavior tree
            // This is a simplified implementation - could be enhanced with more sophisticated matching

            // For now, return 0 to indicate no timeline-based prediction
            // This would require more integration with the narrative system
            return 0f;
        }
    }
}
