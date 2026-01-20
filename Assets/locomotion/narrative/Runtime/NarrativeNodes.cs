using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum NarrativeNodeType
    {
        Sequence,
        Selector,
        Action
    }

    [Serializable]
    public abstract class NarrativeNode
    {
        public string id = Guid.NewGuid().ToString("N");
        public string title = "Node";
        public NarrativeContingency contingency = new NarrativeContingency();
        public abstract NarrativeNodeType NodeType { get; }
    }

    [Serializable]
    public class NarrativeSequenceNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Sequence;

        [SerializeReference]
        public List<NarrativeNode> children = new List<NarrativeNode>();
    }

    [Serializable]
    public class NarrativeSelectorNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Selector;

        [SerializeReference]
        public List<NarrativeNode> children = new List<NarrativeNode>();
    }

    [Serializable]
    public class NarrativeActionNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Action;

        [SerializeReference]
        public NarrativeActionSpec action;
    }
}

