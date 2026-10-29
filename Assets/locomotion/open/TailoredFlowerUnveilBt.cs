using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Fruit closed, anthesis open, actor exit.</summary>
    public static class TailoredFlowerUnveilBt
    {
        public static OpenCloseTopologyAsset Topology(Vector3 fruitWorld, Vector3 anthesisWorld, Vector3 exitWorld)
        {
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.linearOnly = true;
            asset.rootId = "tailored_fruit";
            var root = asset.Root;
            root.nodeId = "tailored_fruit";
            root.enabledInGameplay = false;
            asset.AddChild(root, Node("fruit_closed", fruitWorld));
            asset.AddChild(root, Node("anthesis_open", anthesisWorld));
            asset.AddChild(root, Node("actor_exit", exitWorld));
            return asset;
        }

        static OpenCloseTopologyNode Node(string id, Vector3 world)
        {
            return new OpenCloseTopologyNode
            {
                nodeId = id,
                enabledInGameplay = true,
                hasApproachAnchor = true,
                approachAnchorWorld = world,
                autoCloseBt = AutoCloseBtMode.OnStopExit
            };
        }

        public static OpenCloseTopologyBtBuilder.BakeResult Bake(Transform parent, Transform actor = null)
        {
            Vector3 origin = actor != null ? actor.position : parent != null ? parent.position : Vector3.zero;
            var topology = Topology(origin, origin + Vector3.up * 0.2f, origin + Vector3.up * 0.6f);
            try
            {
                return OpenCloseTopologyBtBuilder.Bake(
                    parent,
                    topology,
                    OpenCloseLemmaProperties.Defaults,
                    actor != null ? actor : parent);
            }
            finally
            {
                Object.DestroyImmediate(topology);
            }
        }
    }
}
