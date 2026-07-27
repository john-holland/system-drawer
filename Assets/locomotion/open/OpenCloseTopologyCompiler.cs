using System.Collections.Generic;
using Locomotion.Open.Nodes;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Compiles OpenCloseTopologyAsset into BehaviorTreeNode hierarchy.</summary>
    public static class OpenCloseTopologyCompiler
    {
        public sealed class CompileResult
        {
            public List<string> previewLines = new List<string>();
            public int closeNodeCount;
            public int openNodeCount;
        }

        public static CompileResult CompilePreview(OpenCloseTopologyAsset asset, OpenCloseLemmaProperties? lemmaOverrides = null)
        {
            var result = new CompileResult();
            if (asset?.root == null)
                return result;
            var closeStack = new Stack<OpenCloseTopologyNode>();
            CompileNode(asset.root, asset, lemmaOverrides, result, closeStack, 0);
            while (closeStack.Count > 0)
            {
                var n = closeStack.Pop();
                result.previewLines.Add($"{Indent(0)}[OnSequenceEnd] Close {n.nodeId}");
                result.closeNodeCount++;
            }
            return result;
        }

        public static OpenCloseSequenceNode BakeToScene(OpenCloseTopologyAsset asset, GameObject host, OpenCloseLemmaProperties? lemmaOverrides = null)
        {
            if (host == null || asset == null)
                return null;

            var rootSeq = host.GetComponent<OpenCloseSequenceNode>();
            if (rootSeq == null)
                rootSeq = host.AddComponent<OpenCloseSequenceNode>();
            rootSeq.topology = asset;
            rootSeq.lemmaOverrides = lemmaOverrides ?? default;
            rootSeq.RebuildFromTopology();
            return rootSeq;
        }

        /// <summary>Bake topology steps onto an <see cref="ObjectOpenCloseTopologyPlanNode"/> host.</summary>
        public static ObjectOpenCloseTopologyPlanNode BakePlanToScene(
            OpenCloseTopologyAsset asset,
            GameObject host,
            OpenCloseLemmaProperties? lemmaOverrides = null)
        {
            if (host == null || asset == null)
                return null;

            var plan = host.GetComponent<ObjectOpenCloseTopologyPlanNode>();
            if (plan == null)
                plan = host.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            plan.topology = asset;
            plan.lemmaOverrides = lemmaOverrides ?? default;
            plan.persistBakedSteps = true;
            plan.BakeFromTopology();
            return plan;
        }

        static void CompileNode(
            OpenCloseTopologyNode node,
            OpenCloseTopologyAsset asset,
            OpenCloseLemmaProperties? lemmaOverrides,
            CompileResult result,
            Stack<OpenCloseTopologyNode> closeStack,
            int depth)
        {
            if (node == null || !node.enabledInGameplay)
                return;
            if (asset.linearOnly && !node.enabledInGameplay)
                return;

            var mode = OpenCloseTopologyBtBuilder.ResolveAutoClose(
                node,
                lemmaOverrides ?? OpenCloseLemmaProperties.Defaults,
                asset.defaultAutoCloseBt);
            result.previewLines.Add($"{Indent(depth)}Ambulate → {(node.jointKind == OpenCloseJointKind.LatchOnly ? "Unlock" : "Open")} [{node.nodeId}] blend={node.arrivalBlendCoefficient:F2} autoClose={mode}");
            result.openNodeCount++;

            foreach (var child in asset.GetChildren(node))
                CompileNode(child, asset, lemmaOverrides, result, closeStack, depth + 1);

            switch (mode)
            {
                case AutoCloseBtMode.AfterChildren:
                    result.previewLines.Add($"{Indent(depth)}Close [{node.nodeId}]");
                    result.closeNodeCount++;
                    break;
                case AutoCloseBtMode.OnStopExit:
                    result.previewLines.Add($"{Indent(depth)}OnStopExit → Close [{node.nodeId}]");
                    result.closeNodeCount++;
                    break;
                case AutoCloseBtMode.OnSequenceEnd:
                    closeStack.Push(node);
                    break;
            }
        }

        static string Indent(int depth) => new string(' ', depth * 2);
    }
}
