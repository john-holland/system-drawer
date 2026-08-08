#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode tests for ragdoll ground/fallen probes and get-up BT bootstrap merge.</summary>
public class RagdollGetUpTests
{
    [Test]
    public void RagdollGroundCheck_IsFallen_UsesUprightDotThreshold()
    {
        var go = new GameObject("RagdollFallenProbe");
        var system = go.AddComponent<RagdollSystem>();
        system.ragdollRoot = go.transform;

        go.transform.rotation = Quaternion.identity;
        Assert.IsFalse(RagdollGroundCheck.IsFallen(system, uprightDotThreshold: 0.5f));

        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Assert.IsTrue(RagdollGroundCheck.IsFallen(system, uprightDotThreshold: 0.5f));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void RagdollGroundCheck_IsOnGround_RaycastsDownFromRoot()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);

        var go = new GameObject("RagdollGroundProbe");
        go.transform.position = new Vector3(0f, 0.2f, 0f);
        var system = go.AddComponent<RagdollSystem>();
        system.ragdollRoot = go.transform;

        Physics.SyncTransforms();

        Assert.IsTrue(RagdollGroundCheck.IsOnGround(system, ~0, distance: 0.5f));

        go.transform.position = new Vector3(0f, 5f, 0f);
        Physics.SyncTransforms();
        Assert.IsFalse(RagdollGroundCheck.IsOnGround(system, ~0, distance: 0.5f));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(floor);
    }

    [Test]
    public void Bootstrap_EnableGetUpFalse_SkipsMerge()
    {
        var actorGo = new GameObject("ActorNoGetUp");
        actorGo.SetActive(false);
        var brain = actorGo.AddComponent<Brain>();
        var actor = actorGo.AddComponent<RagdollActor>();
        actor.enableGetUp = false;

        Assert.IsFalse(RagdollGetUpBootstrap.TryMerge(actor));
        Assert.IsNull(brain.behaviorTree);
        Assert.IsFalse(actor.GetUpMerged);

        Object.DestroyImmediate(actorGo);
    }

    [Test]
    public void Bootstrap_MergeOnce_WrapsExistingRoot_WithoutDoubleWrap()
    {
        // Keep inactive so Awake/OnEnable do not auto-merge before the assert setup.
        var actorGo = new GameObject("ActorGetUp");
        actorGo.SetActive(false);
        var brain = actorGo.AddComponent<Brain>();

        var existingTreeGo = new GameObject("ExistingBT");
        existingTreeGo.transform.SetParent(actorGo.transform, false);
        var existingBt = existingTreeGo.AddComponent<BehaviorTree>();
        var existingRoot = existingTreeGo.AddComponent<RagdollIdleSuccessNode>();
        existingBt.rootNode = existingRoot;
        brain.behaviorTree = existingBt;

        var actor = actorGo.AddComponent<RagdollActor>();
        actor.enableGetUp = true;
        actor.getUpBehaviorTreePrefab = null;

        Assert.IsTrue(RagdollGetUpBootstrap.TryMerge(actor));
        Assert.IsNotNull(brain.behaviorTree);
        Assert.IsInstanceOf<RagdollGetUpSelectorNode>(brain.behaviorTree.rootNode);

        var selector = (RagdollGetUpSelectorNode)brain.behaviorTree.rootNode;
        Assert.AreSame(existingRoot, selector.passthroughChild);
        Assert.IsTrue(actor.GetUpMerged);

        BehaviorTree afterFirst = brain.behaviorTree;
        int selectorsAfterFirst = actorGo.GetComponentsInChildren<RagdollGetUpSelectorNode>(true).Length;
        Assert.AreEqual(1, selectorsAfterFirst);
        Assert.IsFalse(RagdollGetUpBootstrap.TryMerge(actor));
        Assert.AreSame(afterFirst, brain.behaviorTree);
        Assert.AreEqual(1, actorGo.GetComponentsInChildren<RagdollGetUpSelectorNode>(true).Length);

        Object.DestroyImmediate(actorGo);
    }

    [Test]
    public void Bootstrap_NullBrainTree_AssignsGetUpTree()
    {
        var actorGo = new GameObject("ActorFreshGetUp");
        actorGo.SetActive(false);
        var brain = actorGo.AddComponent<Brain>();
        var actor = actorGo.AddComponent<RagdollActor>();
        actor.enableGetUp = true;
        actor.getUpBehaviorTreePrefab = null;

        Assert.IsTrue(RagdollGetUpBootstrap.TryMerge(actor));
        Assert.IsNotNull(brain.behaviorTree);
        Assert.IsInstanceOf<RagdollGetUpSelectorNode>(brain.behaviorTree.rootNode);

        var selector = (RagdollGetUpSelectorNode)brain.behaviorTree.rootNode;
        Assert.IsNotNull(selector.passthroughChild);
        Assert.IsInstanceOf<RagdollIdleSuccessNode>(selector.passthroughChild);

        Object.DestroyImmediate(actorGo);
    }

    [Test]
    public void Factory_Build_WiresConditionToGetUpAction()
    {
        BehaviorTree bt = RagdollGetUpTreeFactory.Build();
        Assert.IsNotNull(bt);
        Assert.IsInstanceOf<RagdollGetUpSelectorNode>(bt.rootNode);

        var condition = bt.GetComponentInChildren<RagdollOnGroundConditionNode>(true);
        var action = bt.GetComponentInChildren<RagdollGetUpActionNode>(true);
        Assert.IsNotNull(condition);
        Assert.IsNotNull(action);
        Assert.AreSame(action, condition.getUpAction);

        Object.DestroyImmediate(bt.gameObject);
    }
}
#endif
