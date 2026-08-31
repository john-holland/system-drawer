using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Linear open/close BT for basement utility installation stops.
    /// Lives in Open.Runtime because Locomotion.Runtime cannot reference this assembly.
    /// </summary>
    public static class UtilityInstallationOpenCloseBt
    {
        public static OpenCloseTopologyAsset FromSteps(UtilityRoomBootstrap room)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "utility_install";
            var root = asset.Root;
            root.nodeId = "utility_install";
            root.enabledInGameplay = false;
            if (room?.installSteps == null)
                return asset;
            for (int i = 0; i < room.installSteps.Count; i++)
            {
                var step = room.installSteps[i];
                if (step == null) continue;
                asset.AddChild(root, new OpenCloseTopologyNode
                {
                    nodeId = string.IsNullOrEmpty(step.id) ? "step_" + i : step.id,
                    enabledInGameplay = true,
                    hasApproachAnchor = true,
                    approachAnchorWorld = step.world,
                    autoCloseBt = AutoCloseBtMode.OnStopExit
                });
            }
            return asset;
        }

        public static OpenCloseTopologyBtBuilder.BakeResult Bake(
            UtilityRoomBootstrap room,
            Transform parent,
            Transform actor = null)
        {
            var topology = FromSteps(room);
            try
            {
                return OpenCloseTopologyBtBuilder.Bake(
                    parent,
                    topology,
                    OpenCloseLemmaProperties.Defaults,
                    actor != null ? actor : room != null ? room.transform : parent);
            }
            finally
            {
                Object.DestroyImmediate(topology);
            }
        }
    }
}
