using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Linear open/close BT for canal lock stops. Lives in Open.Runtime.</summary>
    public static class CanalLockOpenCloseBt
    {
        public static OpenCloseTopologyAsset FromSteps(CannalTravelAgent agent)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "canal_lock";
            var root = asset.Root;
            root.nodeId = "canal_lock";
            root.enabledInGameplay = false;
            if (agent?.steps == null)
                return asset;
            for (int i = 0; i < agent.steps.Count; i++)
            {
                var step = agent.steps[i];
                if (step == null) continue;
                asset.AddChild(root, new OpenCloseTopologyNode
                {
                    nodeId = string.IsNullOrEmpty(step.sgInstanceId) ? step.kind.ToString() : step.sgInstanceId,
                    enabledInGameplay = true,
                    hasApproachAnchor = true,
                    approachAnchorWorld = step.predictedWorld,
                    autoCloseBt = AutoCloseBtMode.OnStopExit
                });
            }
            return asset;
        }

        public static OpenCloseTopologyBtBuilder.BakeResult Bake(
            CannalTravelAgent agent,
            Transform parent,
            Transform actor = null)
        {
            var topology = FromSteps(agent);
            try
            {
                return OpenCloseTopologyBtBuilder.Bake(
                    parent,
                    topology,
                    OpenCloseLemmaProperties.Defaults,
                    actor != null ? actor : agent != null ? agent.transform : parent);
            }
            finally
            {
                Object.DestroyImmediate(topology);
            }
        }
    }
}
