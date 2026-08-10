using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    public enum AudioEquipmentLaneKind
    {
        Physical,
        DigitalTiming,
        AnalogueTiming
    }

    /// <summary>Nested equipment node (PerfTrace-style) for physical/digital audio gear.</summary>
    [Serializable]
    public sealed class AudioEquipmentTraceNode
    {
        public string id;
        public string label;
        public AudioEquipmentLaneKind kind = AudioEquipmentLaneKind.Physical;
        public string machineComponentId;
        [SerializeReference]
        public List<AudioEquipmentTraceNode> children = new List<AudioEquipmentTraceNode>();

        public AudioEquipmentTraceNode Find(string nodeId)
        {
            if (id == nodeId) return this;
            if (children == null) return null;
            for (int i = 0; i < children.Count; i++)
            {
                var hit = children[i]?.Find(nodeId);
                if (hit != null) return hit;
            }
            return null;
        }

        public AudioEquipmentTraceNode ParentOf(string nodeId, AudioEquipmentTraceNode parent = null)
        {
            if (id == nodeId) return parent;
            if (children == null) return null;
            for (int i = 0; i < children.Count; i++)
            {
                var hit = children[i]?.ParentOf(nodeId, this);
                if (hit != null) return hit;
            }
            return null;
        }
    }

    [Serializable]
    public sealed class AudioEquipmentTrace
    {
        [SerializeReference]
        public AudioEquipmentTraceNode root = new AudioEquipmentTraceNode
        {
            id = "root",
            label = "Audio Equipment",
            kind = AudioEquipmentLaneKind.Physical
        };

        public void InsertBefore(string selectedId, AudioEquipmentTraceNode node)
        {
            if (node == null || root == null) return;
            var parent = root.ParentOf(selectedId) ?? root;
            int idx = IndexOfChild(parent, selectedId);
            if (idx < 0) parent.children.Add(node);
            else parent.children.Insert(idx, node);
        }

        public void InsertAfter(string selectedId, AudioEquipmentTraceNode node)
        {
            if (node == null || root == null) return;
            var parent = root.ParentOf(selectedId) ?? root;
            int idx = IndexOfChild(parent, selectedId);
            if (idx < 0) parent.children.Add(node);
            else parent.children.Insert(idx + 1, node);
        }

        static int IndexOfChild(AudioEquipmentTraceNode parent, string childId)
        {
            if (parent?.children == null) return -1;
            for (int i = 0; i < parent.children.Count; i++)
            {
                if (parent.children[i] != null && parent.children[i].id == childId)
                    return i;
            }
            return -1;
        }
    }
}
