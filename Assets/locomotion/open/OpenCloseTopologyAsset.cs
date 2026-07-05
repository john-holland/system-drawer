using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Locomotion.Open
{
    [Serializable]
    public sealed class OpenCloseTopologyNode
    {
        public string nodeId = Guid.NewGuid().ToString("N");
        /// <summary>Parent node id; empty for the topology root.</summary>
        public string parentId = "";
        public GameObject target;
        public OpenCloseJointKind jointKind = OpenCloseJointKind.Hinge;
        public EnclosedVolumeRef concaveVolume = new EnclosedVolumeRef();
        public bool enabledInGameplay = true;
        public bool physicsDriven = true;
        public OpenCloseBeatProfile beatProfile;

        [Header("Ambulation")]
        public Vector3 approachAnchorWorld;
        public bool hasApproachAnchor;
        public float arrivalBlendCoefficient = 0f;
        public float reachRadiusMeters = 0.6f;
        public bool requireFacingTarget = true;

        [Header("Auto-close BT")]
        public AutoCloseBtMode autoCloseBt = AutoCloseBtMode.OnStopExit;

        [Header("Scan hints")]
        public Vector3 openingNormal = Vector3.forward;
        public Vector3 cameraHintCenter;
        public Vector3 floorTangentHint = Vector3.right;

        /// <summary>Legacy nested storage; cleared on save. Use <see cref="OpenCloseTopologyAsset.GetChildren"/> instead.</summary>
        [HideInInspector]
        public List<OpenCloseTopologyNode> children = new List<OpenCloseTopologyNode>();
    }

    [Serializable]
    public sealed class EnclosedVolumeRef
    {
        public Vector3 center;
        public Vector3 size;
        public bool hasVolume;

        public Bounds ToBounds()
        {
            return hasVolume ? new Bounds(center, size) : default;
        }
    }

    [CreateAssetMenu(fileName = "OpenCloseTopology", menuName = "Locomotion/Open-Close Topology")]
    public sealed class OpenCloseTopologyAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public string rootId = "root";
        public GameObject rootTarget;
        public List<OpenCloseTopologyNode> nodes = new List<OpenCloseTopologyNode>();
        public AutoCloseBtMode defaultAutoCloseBt = AutoCloseBtMode.OnStopExit;
        public bool compileCloseAmbulation;
        public bool linearOnly;

        [FormerlySerializedAs("root")]
        [SerializeField, HideInInspector]
        OpenCloseTopologyNode _legacyNestedRoot;

        OpenCloseTopologyNode _rootCache;

        public OpenCloseTopologyNode Root
        {
            get
            {
                EnsureRootNode();
                return _rootCache;
            }
            set
            {
                nodes.Clear();
                _rootCache = null;
                if (value == null)
                    return;
                value.parentId = "";
                nodes.Add(value);
                _rootCache = value;
            }
        }

        /// <summary>Compatibility alias for <see cref="Root"/>.</summary>
        public OpenCloseTopologyNode root => Root;

        public IEnumerable<OpenCloseTopologyNode> GetChildren(OpenCloseTopologyNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.nodeId))
                yield break;
            foreach (var n in nodes)
            {
                if (n.parentId == node.nodeId)
                    yield return n;
            }
        }

        public OpenCloseTopologyNode AddChild(OpenCloseTopologyNode parent, OpenCloseTopologyNode child = null)
        {
            EnsureRootNode();
            parent ??= _rootCache;
            child ??= new OpenCloseTopologyNode();
            child.parentId = parent.nodeId;
            nodes.Add(child);
            return child;
        }

        public void SetChildCount(OpenCloseTopologyNode parent, int count)
        {
            var children = new List<OpenCloseTopologyNode>();
            foreach (var c in GetChildren(parent))
                children.Add(c);

            while (children.Count > count)
            {
                var last = children[children.Count - 1];
                RemoveNodeRecursive(last);
                children.RemoveAt(children.Count - 1);
            }

            while (children.Count < count)
                children.Add(AddChild(parent));
        }

        public OpenCloseTopologyNode GetChild(OpenCloseTopologyNode parent, int index)
        {
            int i = 0;
            foreach (var c in GetChildren(parent))
            {
                if (i == index)
                    return c;
                i++;
            }
            return null;
        }

        public void ClearTopology()
        {
            nodes.Clear();
            _rootCache = null;
            _legacyNestedRoot = null;
        }

        public IEnumerable<OpenCloseTopologyNode> EnumerateDepthFirst(OpenCloseTopologyNode node = null)
        {
            EnsureRootNode();
            node ??= _rootCache;
            if (node == null)
                yield break;
            yield return node;
            foreach (var child in GetChildren(node))
            {
                foreach (var n in EnumerateDepthFirst(child))
                    yield return n;
            }
        }

        void EnsureRootNode()
        {
            if (_rootCache != null && nodes.Contains(_rootCache))
                return;

            _rootCache = null;
            foreach (var n in nodes)
            {
                if (string.IsNullOrEmpty(n.parentId))
                {
                    _rootCache = n;
                    return;
                }
            }

            _rootCache = new OpenCloseTopologyNode { nodeId = rootId, parentId = "" };
            nodes.Add(_rootCache);
        }

        void RemoveNodeRecursive(OpenCloseTopologyNode node)
        {
            foreach (var c in new List<OpenCloseTopologyNode>(GetChildren(node)))
                RemoveNodeRecursive(c);
            nodes.Remove(node);
            if (_rootCache == node)
                _rootCache = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_legacyNestedRoot != null && nodes.Count == 0)
            {
                FlattenLegacyNode(_legacyNestedRoot, "");
                _legacyNestedRoot = null;
            }

            foreach (var n in nodes)
            {
                if (n?.children != null && n.children.Count > 0)
                    n.children.Clear();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (nodes == null)
                nodes = new List<OpenCloseTopologyNode>();

            if (nodes.Count == 0 && _legacyNestedRoot != null)
            {
                FlattenLegacyNode(_legacyNestedRoot, "");
                _legacyNestedRoot = null;
            }

            _rootCache = null;
            foreach (var n in nodes)
            {
                if (n?.children != null && n.children.Count > 0)
                    n.children.Clear();
            }
            EnsureRootNode();
        }

        void FlattenLegacyNode(OpenCloseTopologyNode legacy, string parentId)
        {
            legacy.parentId = parentId;
            var nestedChildren = legacy.children != null ? new List<OpenCloseTopologyNode>(legacy.children) : null;
            legacy.children?.Clear();
            nodes.Add(legacy);

            if (nestedChildren == null)
                return;
            foreach (var child in nestedChildren)
                FlattenLegacyNode(child, legacy.nodeId);
        }
    }
}
