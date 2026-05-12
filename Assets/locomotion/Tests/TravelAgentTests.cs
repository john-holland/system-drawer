#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TravelAgentTests
{
    [Test]
    public void RefreshDiscoveredNodes_FindsPathfindingChild()
    {
        var root = new GameObject("travel_root");
        var agent = root.AddComponent<TravelAgent>();
        var btParent = new GameObject("bt_branch");
        btParent.transform.SetParent(root.transform, false);
        var pfGo = new GameObject("pathfind");
        pfGo.transform.SetParent(btParent.transform, false);
        pfGo.AddComponent<PathfindingNode>();

        agent.RefreshDiscoveredNodes();

        Assert.GreaterOrEqual(agent.DiscoveredNodes.Count, 1);
        bool foundPathfinding = false;
        foreach (TravelDiscoveredNodeInfo info in agent.DiscoveredNodes)
        {
            if (info.nodeTypeName == nameof(PathfindingNode))
                foundPathfinding = true;
        }

        Assert.IsTrue(foundPathfinding);

        Object.DestroyImmediate(root);
    }
}
#endif
