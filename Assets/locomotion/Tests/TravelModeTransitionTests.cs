#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TravelModeTransitionTests
{
    public static int TransitionExecuteCount;

    sealed class TransitionCounterNode : TravelContextBehaviorTreeNode
    {
        void Awake()
        {
            nodeType = NodeType.Action;
        }

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            TransitionExecuteCount++;
            Assert.IsNotNull(Ctx, "Expected travel execution context during transition");
            return BehaviorTreeStatus.Success;
        }
    }

    [SetUp]
    public void SetUp()
    {
        TransitionExecuteCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        TransitionExecuteCount = 0;
    }

    [Test]
    public void BuildPlan_WalkThenDrive_InsertsTransitionChild_WhenBindingMatches()
    {
        var root = new GameObject("composite_root");
        var treeGo = new GameObject("bt");
        treeGo.transform.SetParent(root.transform);
        var tree = treeGo.AddComponent<BehaviorTree>();

        var travelAgentGo = new GameObject("travel_agent");
        travelAgentGo.transform.SetParent(root.transform);
        var amb = travelAgentGo.AddComponent<BaseAmbulatingActor>();
        var agent = travelAgentGo.AddComponent<TravelAgent>();
        agent.ambulatingActor = amb;

        var compositeGo = new GameObject("composite");
        compositeGo.transform.SetParent(root.transform);
        var composite = compositeGo.AddComponent<CompositeMultiModalPathNode>();

        var templateGo = new GameObject("activation_template");
        templateGo.AddComponent<TransitionCounterNode>();

        composite.modeTransitionBindings = new List<TravelModeTransitionBinding>
        {
            new TravelModeTransitionBinding
            {
                fromMode = TravelLegMode.Walk,
                toMode = TravelLegMode.Drive,
                activationRoot = templateGo
            }
        };

        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3> { Vector3.zero, Vector3.forward }));
        plan.segments.Add(MultiModalSegment.FromDrive(new List<Vector3> { Vector3.forward, Vector3.forward * 2f }, null));

        Assert.IsTrue(composite.BuildChildrenFromPlanForTests(plan, tree));
        Assert.AreEqual(2, composite.children.Count);

        var leg0 = composite.children[0] as TravelLegSequenceNode;
        var leg1 = composite.children[1] as TravelLegSequenceNode;
        Assert.IsNotNull(leg0);
        Assert.IsNotNull(leg1);
        Assert.IsFalse(leg0.children != null && leg0.children.Count > 0 && leg0.children[0] is TravelModeTransitionSequenceNode,
            "First leg has no mode transition");
        Assert.GreaterOrEqual(leg1.children?.Count ?? 0, 2, "Drive leg: transition + waypoints");

        var trans = leg1.children[0] as TravelModeTransitionSequenceNode;
        Assert.IsNotNull(trans, "Expected TravelModeTransitionSequenceNode as first child of drive leg");
        Assert.AreEqual(TravelLegMode.Walk, trans.fromMode);
        Assert.AreEqual(TravelLegMode.Drive, trans.toMode);

        trans.OnEnter(tree);
        BehaviorTreeStatus s = trans.Execute(tree);
        Assert.AreEqual(BehaviorTreeStatus.Success, s);
        Assert.AreEqual(1, TransitionExecuteCount);

        s = trans.Execute(tree);
        Assert.AreEqual(BehaviorTreeStatus.Success, s);
        Assert.AreEqual(1, TransitionExecuteCount, "Transition runs once");

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(templateGo);
    }

    [Test]
    public void BuildPlan_NoBinding_SkipsTransitionChild()
    {
        var root = new GameObject("composite_root2");
        var treeGo = new GameObject("bt");
        treeGo.transform.SetParent(root.transform);
        var tree = treeGo.AddComponent<BehaviorTree>();

        var compositeGo = new GameObject("composite");
        compositeGo.transform.SetParent(root.transform);
        var composite = compositeGo.AddComponent<CompositeMultiModalPathNode>();
        composite.modeTransitionBindings = new List<TravelModeTransitionBinding>();

        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3> { Vector3.zero, Vector3.one }));
        plan.segments.Add(MultiModalSegment.FromDrive(new List<Vector3> { Vector3.one, Vector3.one * 2f }, null));

        Assert.IsTrue(composite.BuildChildrenFromPlanForTests(plan, tree));

        var leg1 = composite.children[1] as TravelLegSequenceNode;
        Assert.IsNotNull(leg1);
        Assert.IsFalse(leg1.children != null && leg1.children.Count > 0 && leg1.children[0] is TravelModeTransitionSequenceNode);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void TryResolve_MatchAnyFrom_MatchesFlyToDrive()
    {
        var bindings = new List<TravelModeTransitionBinding>
        {
            new TravelModeTransitionBinding { matchAnyFrom = true, toMode = TravelLegMode.Drive }
        };

        Assert.IsTrue(TravelModeTransitionBinding.TryResolve(TravelLegMode.Walk, TravelLegMode.Drive, bindings, out _));
        Assert.IsTrue(TravelModeTransitionBinding.TryResolve(TravelLegMode.Fly, TravelLegMode.Drive, bindings, out _));
        Assert.IsFalse(TravelModeTransitionBinding.TryResolve(TravelLegMode.Walk, TravelLegMode.Walk, bindings, out _));
    }

    [Test]
    public void Transition_PublishesContext_WithTravelAgentAndAmbulatingActor()
    {
        var root = new GameObject("ctx_root");
        var treeGo = new GameObject("bt");
        treeGo.transform.SetParent(root.transform);
        var tree = treeGo.AddComponent<BehaviorTree>();

        var travelAgentGo = new GameObject("travel_agent");
        travelAgentGo.transform.SetParent(root.transform);
        var amb = travelAgentGo.AddComponent<BaseAmbulatingActor>();
        var agent = travelAgentGo.AddComponent<TravelAgent>();
        agent.ambulatingActor = amb;

        var compositeGo = new GameObject("composite");
        compositeGo.transform.SetParent(root.transform);
        var composite = compositeGo.AddComponent<CompositeMultiModalPathNode>();
        Assert.IsNotNull(TravelExecutionContextProvider.Ensure(compositeGo, tree, agent));

        TravelExecutionContext captured = null;
        var captureGo = new GameObject("capture");
        var capture = captureGo.AddComponent<ContextCaptureNode>();
        capture.onExecute = ctx => captured = ctx;

        composite.modeTransitionBindings = new List<TravelModeTransitionBinding>
        {
            new TravelModeTransitionBinding
            {
                fromMode = TravelLegMode.Walk,
                toMode = TravelLegMode.Drive,
                activationNodes = new List<BehaviorTreeNode> { capture }
            }
        };

        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3> { Vector3.zero }));
        plan.segments.Add(MultiModalSegment.FromDrive(new List<Vector3> { Vector3.forward }, null));

        composite.BuildChildrenFromPlanForTests(plan, tree);

        var leg1 = composite.children[1] as TravelLegSequenceNode;
        var trans = leg1.children[0] as TravelModeTransitionSequenceNode;
        trans.OnEnter(tree);
        trans.Execute(tree);

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured.isModeTransition);
        Assert.AreEqual(TravelLegMode.Walk, captured.fromMode);
        Assert.AreEqual(TravelLegMode.Drive, captured.toMode);
        Assert.AreSame(agent, captured.travelAgent);
        Assert.AreSame(amb, captured.ambulatingActor);

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(captureGo);
    }

    sealed class ContextCaptureNode : TravelContextBehaviorTreeNode, ITravelExecutionContextConsumer
    {
        public System.Action<TravelExecutionContext> onExecute;
        TravelExecutionContext _injected;

        void Awake()
        {
            nodeType = NodeType.Action;
        }

        public void SetTravelExecutionContext(TravelExecutionContext ctx) => _injected = ctx;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            onExecute?.Invoke(Ctx ?? _injected);
            return BehaviorTreeStatus.Success;
        }
    }
}
#endif
