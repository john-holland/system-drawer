using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

namespace Locomotion.Audio
{
    /// <summary>
    /// Analyzes behavior trees to predict sound effect timing.
    /// Uses narrative timeline data to estimate when sounds should occur.
    /// </summary>
    public class BehaviorTreeTimingPredictor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to narrative calendar")]
        public NarrativeCalendarAsset narrativeCalendar;

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
            // Auto-find narrative calendar if not assigned
            if (narrativeCalendar == null)
            {
                narrativeCalendar = FindObjectOfType<NarrativeCalendarAsset>();
            }
        }

        /// <summary>
        /// Predict sound timing for a behavior tree using narrative calendar.
        /// </summary>
        public float PredictSoundTiming(BehaviorTree tree, NarrativeCalendarAsset calendar)
        {
            if (tree == null)
                return defaultTiming;

            if (calendar == null)
            {
                calendar = narrativeCalendar;
            }

            float predictedTime = defaultTiming;

            // Method 1: Use behavior tree duration estimation
            if (useDurationEstimation && tree.rootNode != null)
            {
                predictedTime = tree.rootNode.CalculateDuration();
                if (enableDebugLogging)
                {
                    Debug.Log($"[BehaviorTreeTimingPredictor] Duration-based prediction: {predictedTime}s");
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
            List<BehaviorTreeNode> soundNodes = ExtractSoundNodes(tree);
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
        /// </summary>
        public List<BehaviorTreeNode> ExtractSoundNodes(BehaviorTree tree)
        {
            List<BehaviorTreeNode> soundNodes = new List<BehaviorTreeNode>();

            if (tree == null || tree.rootNode == null)
                return soundNodes;

            // Recursively search for sound nodes
            ExtractSoundNodesRecursive(tree.rootNode, soundNodes);

            return soundNodes;
        }

        /// <summary>
        /// Recursively extract sound nodes.
        /// </summary>
        private void ExtractSoundNodesRecursive(BehaviorTreeNode node, List<BehaviorTreeNode> soundNodes)
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
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child != null)
                    {
                        ExtractSoundNodesRecursive(child, soundNodes);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate timing for a specific node.
        /// </summary>
        public float CalculateNodeTiming(BehaviorTreeNode node)
        {
            if (node == null)
                return defaultTiming;

            // Use estimated duration if available
            if (node.estimatedDuration > 0f)
            {
                return node.estimatedDuration;
            }

            // Calculate from children (for sequence nodes)
            if (node.nodeType == NodeType.Sequence && node.children != null)
            {
                float totalTime = 0f;
                foreach (var child in node.children)
                {
                    if (child != null)
                    {
                        totalTime += CalculateNodeTiming(child);
                    }
                }
                return totalTime;
            }

            // Use default timing
            return defaultTiming;
        }

        /// <summary>
        /// Predict timing from narrative timeline events.
        /// </summary>
        private float PredictFromTimeline(BehaviorTree tree, NarrativeCalendarAsset calendar)
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
