using UnityEngine;

namespace Locomotion.Open
{
    public static class TayloringOpenCloseBt
    {
        public static OpenCloseTopologyAsset FromSteps(TayloringTravelAgent agent)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "tayloring_line";
            var root = asset.Root;
            root.nodeId = "tayloring_line";
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
            TayloringTravelAgent agent,
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
                UnityEngine.Object.DestroyImmediate(topology);
            }
        }
    }
}
