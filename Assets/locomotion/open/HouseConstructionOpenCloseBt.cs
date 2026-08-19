using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Linear open/close BT for house construction RTS stops.
    /// Lives in Open.Runtime because Locomotion.Runtime cannot reference this assembly.
    /// </summary>
    public static class HouseConstructionOpenCloseBt
    {
        public static OpenCloseTopologyAsset FromSteps(HouseConstructionTravelAgent agent)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "construction_site";
            var root = asset.Root;
            root.nodeId = "construction_site";
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
            HouseConstructionTravelAgent agent,
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
