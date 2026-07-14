using System.Collections.Generic;
using Locomotion.Open.Nodes;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Shared factory that bakes OpenCloseTopologyAsset stops into BT child GameObjects.</summary>
    public static class OpenCloseTopologyBtBuilder
    {
        public sealed class BakeResult
        {
            public readonly List<OpenCloseTopologyNode> flatNodes = new List<OpenCloseTopologyNode>();
            public readonly List<OpenCloseAmbulateToStopNode> stopNodes = new List<OpenCloseAmbulateToStopNode>();
            public readonly Stack<OpenCloseTopologyNode> closeStack = new Stack<OpenCloseTopologyNode>();
        }

        public static BakeResult Bake(
            Transform parent,
            OpenCloseTopologyAsset topology,
            OpenCloseLemmaProperties lemmaOverrides,
            Transform actor,
            bool clearChildren = true)
        {
            var result = new BakeResult();
            if (parent == null)
                return result;

            if (clearChildren)
                ClearChildren(parent);

            if (topology?.root == null)
                return result;

            bool linearOnly = topology.linearOnly || lemmaOverrides.linearOnly;

            foreach (var n in topology.EnumerateDepthFirst())
            {
                if (n == null || !n.enabledInGameplay)
                    continue;
                if (linearOnly && !n.enabledInGameplay)
                    continue;

                var autoClose = ResolveAutoClose(n, lemmaOverrides, topology.defaultAutoCloseBt);
                result.flatNodes.Add(n);
                if (autoClose == AutoCloseBtMode.OnSequenceEnd)
                    result.closeStack.Push(n);

                var amb = CreateAmbulateNode(parent, n, lemmaOverrides, actor, autoClose);
                result.stopNodes.Add(amb);
            }

            return result;
        }

        public static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (Application.isPlaying)
                    Object.Destroy(c.gameObject);
                else
                    Object.DestroyImmediate(c.gameObject);
            }
        }

        /// <summary>Rehydrate bake lists from already-persisted Stop_* children (editor bake).</summary>
        public static BakeResult CollectExisting(Transform parent, OpenCloseTopologyAsset topology)
        {
            var result = new BakeResult();
            if (parent == null || topology?.root == null)
                return result;

            var byId = new Dictionary<string, OpenCloseTopologyNode>();
            foreach (var n in topology.EnumerateDepthFirst())
            {
                if (n == null || !n.enabledInGameplay)
                    continue;
                byId[n.nodeId] = n;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var amb = child.GetComponent<OpenCloseAmbulateToStopNode>();
                if (amb == null)
                    continue;
                string id = child.name.StartsWith("Stop_") ? child.name.Substring(5) : child.name;
                if (!byId.TryGetValue(id, out var node))
                    continue;
                result.flatNodes.Add(node);
                result.stopNodes.Add(amb);
            }

            for (int i = 0; i < result.flatNodes.Count; i++)
            {
                var node = result.flatNodes[i];
                var autoClose = ResolveAutoClose(node, default, topology.defaultAutoCloseBt);
                if (autoClose == AutoCloseBtMode.OnSequenceEnd)
                    result.closeStack.Push(node);
            }

            return result;
        }

        public static AutoCloseBtMode ResolveAutoClose(
            OpenCloseTopologyNode node,
            OpenCloseLemmaProperties lemma,
            AutoCloseBtMode assetDefault)
        {
            if (lemma.autoCloseBt != OpenCloseLemmaAutoCloseBtMode.OnStopExit)
                return OpenCloseLemmaPropertyResolver.ToRuntimeAutoClose(lemma.autoCloseBt);
            if (node != null && node.autoCloseBt != AutoCloseBtMode.OnStopExit)
                return node.autoCloseBt;
            return assetDefault;
        }

        public static OpenCloseAmbulateToStopNode CreateAmbulateNode(
            Transform parent,
            OpenCloseTopologyNode node,
            OpenCloseLemmaProperties lemmaOverrides,
            Transform actor,
            AutoCloseBtMode? autoCloseOverride = null)
        {
            var go = new GameObject($"Stop_{node.nodeId}");
            go.transform.SetParent(parent, false);
            var amb = go.AddComponent<OpenCloseAmbulateToStopNode>();
            amb.approachAnchor = node.hasApproachAnchor
                ? node.approachAnchorWorld
                : (node.target != null ? node.target.transform.position : Vector3.zero);
            amb.arrivalBlendCoefficient = lemmaOverrides.arrivalBlendCoefficient > 0f
                ? lemmaOverrides.arrivalBlendCoefficient
                : node.arrivalBlendCoefficient;
            amb.reachRadiusMeters = lemmaOverrides.reachRadiusMeters > 0f
                ? lemmaOverrides.reachRadiusMeters
                : node.reachRadiusMeters;
            amb.requireFacingTarget = node.requireFacingTarget;
            amb.handlePoint = node.target != null ? node.target.transform : null;

            var driver = node.target != null ? node.target.GetComponent<OpenableJointDriver>() : null;
            if (driver != null)
                ApplyDriverOverrides(driver, node, lemmaOverrides);

            if (node.jointKind == OpenCloseJointKind.LatchOnly)
            {
                var unlock = go.AddComponent<UnlockLatchNode>();
                unlock.latch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
                unlock.toolLemma = lemmaOverrides.requireToolLemma ?? "";
                unlock.topologyNodeId = node.nodeId;
                unlock.profile = node.beatProfile;
                amb.children.Add(unlock);
            }
            else
            {
                var open = go.AddComponent<OpenJointNode>();
                open.driver = driver;
                open.profile = node.beatProfile;
                open.topologyNodeId = node.nodeId;
                amb.children.Add(open);
            }

            var autoClose = autoCloseOverride ?? node.autoCloseBt;
            if (autoClose == AutoCloseBtMode.OnStopExit)
            {
                var exitGo = new GameObject("ExitTrigger");
                exitGo.transform.SetParent(go.transform, false);
                var exit = exitGo.AddComponent<OpenCloseExitTriggerNode>();
                exit.stopCenter = node.target != null ? node.target.transform : parent;
                exit.actor = actor != null ? actor : parent;
                var close = exitGo.AddComponent<CloseJointNode>();
                close.driver = driver;
                close.profile = node.beatProfile;
                close.relatch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
                close.topologyNodeId = node.nodeId;
                amb.children.Add(exit);
                amb.children.Add(close);
            }

            return amb;
        }

        public static CloseJointNode CreateCloseNode(Transform parent, OpenCloseTopologyNode node)
        {
            var closeGo = new GameObject($"Close_{node.nodeId}");
            closeGo.transform.SetParent(parent, false);
            var close = closeGo.AddComponent<CloseJointNode>();
            close.driver = node.target != null ? node.target.GetComponent<OpenableJointDriver>() : null;
            close.profile = node.beatProfile;
            close.relatch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
            close.topologyNodeId = node.nodeId;
            return close;
        }

        public static void ApplyDriverOverrides(
            OpenableJointDriver driver,
            OpenCloseTopologyNode node,
            OpenCloseLemmaProperties lemma)
        {
            if (driver == null)
                return;

            var profile = node?.beatProfile;
            float angle = lemma.openAngleDeg > 0f ? lemma.openAngleDeg : (profile != null ? profile.openAngleDeg : driver.targetOpenAngle);
            if (angle > 0f)
                driver.targetOpenAngle = angle;

            var drive = profile != null ? profile.driveMode : OpenCloseDriveMode.Hybrid;
            if (lemma.driveMode == OpenCloseLemmaDriveMode.Physics)
                drive = OpenCloseDriveMode.Physics;
            else if (lemma.driveMode == OpenCloseLemmaDriveMode.Animation)
                drive = OpenCloseDriveMode.Animation;
            else if (lemma.driveMode == OpenCloseLemmaDriveMode.Hybrid && profile == null)
                drive = OpenCloseDriveMode.Hybrid;

            driver.driveMode = drive;
            if (!string.IsNullOrEmpty(lemma.openAnimationRef))
                driver.openAnimationRef = lemma.openAnimationRef;
            else if (profile != null)
                driver.openAnimationRef = profile.openAnimationRef;
            if (!string.IsNullOrEmpty(lemma.closeAnimationRef))
                driver.closeAnimationRef = lemma.closeAnimationRef;
            else if (profile != null)
                driver.closeAnimationRef = profile.closeAnimationRef;
        }
    }
}
