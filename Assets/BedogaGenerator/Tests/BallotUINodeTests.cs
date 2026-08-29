#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BallotUINodeTests
{
    [Test]
    public void HoverConfirm_WritesVoterChoice()
    {
        var go = new GameObject("ballot-ui");
        try
        {
            var ui = go.AddComponent<BallotUINode>();
            var vote = go.AddComponent<VoteBehaviorTreeNode>();
            var spec = ScriptableObject.CreateInstance<BallotSpec>();
            spec.EnsureQuestionDefaults();
            ui.Bind(spec, vote);
            Assert.IsTrue(ui.Hover("no"));
            Assert.IsTrue(ui.TryConfirmHovered());
            Assert.AreEqual("no", vote.voter.chosenOptionId);
            Assert.IsTrue(vote.voter.hasChosen);
            Assert.AreEqual("Ballot", ui.BallotLabel);
            Assert.AreEqual("questions", ui.KindListLabel());
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Rank_AppendsAndConfirmSetsFirstPref()
    {
        var go = new GameObject("ballot-rank");
        try
        {
            var ui = go.AddComponent<BallotUINode>();
            var vote = go.AddComponent<VoteBehaviorTreeNode>();
            var spec = ScriptableObject.CreateInstance<BallotSpec>();
            spec.kind = BallotKind.Candidate;
            spec.tallyMethod = BallotTallyMethod.Irv;
            spec.options = new System.Collections.Generic.List<BallotOption>
            {
                new BallotOption { optionId = "a", displayName = "A" },
                new BallotOption { optionId = "b", displayName = "B" }
            };
            ui.Bind(spec, vote);
            Assert.AreEqual("candidates", ui.KindListLabel());
            Assert.IsTrue(ui.Rank("b"));
            Assert.IsTrue(ui.Rank("a"));
            Assert.AreEqual("b", ui.ranking[0]);
            ui.Hover("a");
            Assert.IsTrue(ui.TryConfirmHovered());
            Assert.AreEqual("b", vote.voter.chosenOptionId);
            Assert.AreEqual("b", vote.ranking[0]);
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
