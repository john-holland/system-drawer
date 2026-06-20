using System.Reflection;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public class BrainThoughtPipelineTests
{
    private static void FlushThoughtQueue(Brain brain)
    {
        var mi = typeof(Brain).GetMethod("ProcessThoughts", BindingFlags.NonPublic | BindingFlags.Instance);
        mi?.Invoke(brain, null);
    }

    [Test]
    public void SendThought_Decision_SetsGoal_OnReceiver()
    {
        var sendGo = new GameObject("sender_br");
        var recvGo = new GameObject("recv_br");
        var sender = sendGo.AddComponent<Brain>();
        var recv = recvGo.AddComponent<Brain>();
        sender.enabled = false;
        recv.enabled = false;
        var bt = recvGo.AddComponent<BehaviorTree>();
        recv.behaviorTree = bt;

        var payload = new DecisionThoughtPayload { proposedGoalName = "Walk", conviction = 1f };
        var td = new ThoughtData(sender, recv, ThoughtType.Decision, payload);
        sender.SendThought(recv, td);

        FlushThoughtQueue(recv);
        Assert.IsNotNull(bt.currentGoal);
        Assert.AreEqual("Walk", bt.currentGoal.goalName);
    }

    [Test]
    public void ThoughtSimilarityMvp_ReturnsValue_ForOverlappingTags()
    {
        var go = new GameObject("t");
        var brain = go.AddComponent<Brain>();
        var bt = go.AddComponent<BehaviorTree>();
        brain.behaviorTree = bt;
        bt.currentGoal = new BehaviorTreeGoal { goalName = "WalkForward" };

        var incoming = new ThoughtData(null, brain, ThoughtType.Decision,
            new DecisionThoughtPayload { proposedGoalName = "Walk", semanticTags = new[] { "Walk", "Forward" } });

        float s = ThoughtSimilarityMvp.ScoreNameOverlap(new[] { "Walk" }, brain, incoming);
        Assert.Greater(s, 0f);
    }

    [Test]
    public void SendThoughtAction_Execute_ResolvesBindingsAndSetsGoal()
    {
        var sendGo = new GameObject("a1");
        var recvGo = new GameObject("a2");
        var sender = sendGo.AddComponent<Brain>();
        var recv = recvGo.AddComponent<Brain>();
        sender.enabled = false;
        recv.enabled = false;
        var bt = recvGo.AddComponent<BehaviorTree>();
        recv.behaviorTree = bt;

        var bindGo = new GameObject("bind");
        var bindings = bindGo.AddComponent<NarrativeBindings>();
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "s", value = sendGo });
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "r", value = recvGo });
        bindings.RebuildIndex();

        var ctx = new NarrativeExecutionContext(null, bindings, null);
        var action = new SendThoughtAction
        {
            senderKey = "s",
            receiverKey = "r",
            thoughtType = NarrativeThoughtType.Decision,
            decisionPayload = new NarrativeDecisionThoughtPayload { proposedGoalName = "Sit", conviction = 1f }
        };

        var state = new NarrativeRuntimeState();
        var status = action.Execute(ctx, state);
        Assert.AreEqual((int)BehaviorTreeStatus.Success, (int)status);
        FlushThoughtQueue(recv);
        Assert.IsNotNull(bt.currentGoal);
        Assert.AreEqual("Sit", bt.currentGoal.goalName);
    }

    [Test]
    public void AcceptThoughtDecision_WhenFalse_IgnoresDecision()
    {
        var sendGo = new GameObject("sender_br");
        var recvGo = new GameObject("recv_br");
        var sender = sendGo.AddComponent<Brain>();
        var recv = recvGo.AddComponent<Brain>();
        sender.enabled = false;
        recv.enabled = false;
        recv.acceptThoughtDecision = false;
        var bt = recvGo.AddComponent<BehaviorTree>();
        recv.behaviorTree = bt;

        var payload = new DecisionThoughtPayload { proposedGoalName = "Walk", conviction = 1f };
        var td = new ThoughtData(sender, recv, ThoughtType.Decision, payload);
        sender.SendThought(recv, td);

        FlushThoughtQueue(recv);
        Assert.IsNull(bt.currentGoal);
    }
}
