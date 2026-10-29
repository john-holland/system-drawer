using UnityEngine;

namespace Locomotion.Open
{
    public static class FlowerOpenCloseBt
    {
        public static OpenCloseTopologyAsset FromSteps(FlowerTravelAgent agent)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "flower_bud";
            var root = asset.Root;
            root.nodeId = "flower_bud";
            root.enabledInGameplay = false;
            if (agent?.steps == null)
                return asset;
            for (int i = 0; i < agent.steps.Count; i++)
            {
                var step = agent.steps[i];
                if (step == null || !FlowerDevelopment.IsBudThroughAnthesis(step.kind)) continue;
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
            FlowerTravelAgent agent,
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
