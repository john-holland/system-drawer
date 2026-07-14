using System.Collections.Generic;
using Locomotion.Open.Nodes;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Open.Tests
{
    public sealed class ObjectOpenCloseTopologyPlanNodeTests
    {
        [Test]
        public void BakeFromTopology_CreatesStopPerEnabledNode()
        {
            var host = new GameObject("PlanBT");
            var plan = host.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "root";
            asset.Root.enabledInGameplay = true;
            asset.AddChild(asset.Root, new OpenCloseTopologyNode { nodeId = "child", enabledInGameplay = true });
            plan.topology = asset;
            plan.BakeFromTopology();
            Assert.AreEqual(2, plan.children.Count);
            Assert.IsNotNull(host.transform.Find("Stop_root"));
            Assert.IsNotNull(host.transform.Find("Stop_child"));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void BakeFromTopology_AppliesLemmaAutoCloseToOnStopExitBranch()
        {
            var host = new GameObject("PlanBT");
            var plan = host.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "lid";
            asset.Root.enabledInGameplay = true;
            asset.Root.autoCloseBt = AutoCloseBtMode.None;
            plan.topology = asset;
            plan.lemmaOverrides = new OpenCloseLemmaProperties
            {
                autoCloseBt = OpenCloseLemmaAutoCloseBtMode.OnStopExit,
                openAngleDeg = 90f,
                driveMode = OpenCloseLemmaDriveMode.Hybrid,
                arrivalBlendCoefficient = 0f,
                reachRadiusMeters = 0.6f,
                requireFacingTarget = true,
            };
            // Lemma OnStopExit is the "unset" sentinel in ResolveAutoClose — node None should win.
            plan.BakeFromTopology();
            var stop = host.transform.Find("Stop_lid");
            Assert.IsNotNull(stop);
            Assert.IsNull(stop.Find("ExitTrigger"));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void BakeFromTopology_LemmaAfterChildrenWinsOverNodeNone()
        {
            var host = new GameObject("PlanBT");
            var plan = host.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "drawer";
            asset.Root.enabledInGameplay = true;
            asset.Root.autoCloseBt = AutoCloseBtMode.None;
            plan.topology = asset;
            plan.lemmaOverrides = new OpenCloseLemmaProperties
            {
                autoCloseBt = OpenCloseLemmaAutoCloseBtMode.AfterChildren,
                openAngleDeg = 45f,
                driveMode = OpenCloseLemmaDriveMode.Physics,
                arrivalBlendCoefficient = 0f,
                reachRadiusMeters = 0.6f,
            };
            plan.BakeFromTopology();
            Assert.AreEqual(1, plan.children.Count);
            // AfterChildren does not bake ExitTrigger; close runs during Execute.
            Assert.IsNull(host.transform.Find("Stop_drawer/ExitTrigger"));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void SequenceNode_DelegatesBakeToPlanNode()
        {
            var host = new GameObject("BT");
            var seq = host.AddComponent<OpenCloseSequenceNode>();
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "root";
            asset.Root.enabledInGameplay = true;
            asset.AddChild(asset.Root, new OpenCloseTopologyNode { nodeId = "child", enabledInGameplay = true });
            seq.topology = asset;
            seq.RebuildFromTopology();
            Assert.AreEqual(2, seq.children.Count);
            Assert.IsNotNull(host.GetComponent<ObjectOpenCloseTopologyPlanNode>());
            Object.DestroyImmediate(host);
        }

        [Test]
        public void BeatMessageBus_RaisesSoundDialogueQuestUi()
        {
            var seen = new List<OpenCloseBeatChannel>();
            void Handler(OpenCloseBeatMessage m) => seen.Add(m.channel);
            OpenCloseBeatMessageBus.Raised += Handler;
            try
            {
                var profile = ScriptableObject.CreateInstance<OpenCloseBeatProfile>();
                profile.soundOpen = AudioClip.Create("t", 440, 1, 44100, false);
                profile.dialogueSpanRef = "span.hello";
                profile.questHintKind = OpenCloseQuestHintKind.Complete;
                profile.questObjectiveId = "obj1";
                profile.uiMessageId = "ui.open";
                profile.uiMessageText = "Opened";
                profile.playMusicOnOpen = true;
                profile.musicActiveLeafId = "leaf_active";

                OpenCloseBeatMessageBus.RaiseOpenBeat("lid", profile, Vector3.zero);

                CollectionAssert.Contains(seen, OpenCloseBeatChannel.Sound);
                CollectionAssert.Contains(seen, OpenCloseBeatChannel.Dialogue);
                CollectionAssert.Contains(seen, OpenCloseBeatChannel.Quest);
                CollectionAssert.Contains(seen, OpenCloseBeatChannel.UI);
                CollectionAssert.Contains(seen, OpenCloseBeatChannel.Music);
                Object.DestroyImmediate(profile.soundOpen);
                Object.DestroyImmediate(profile);
            }
            finally
            {
                OpenCloseBeatMessageBus.Raised -= Handler;
            }
        }

        [Test]
        public void OpenableJointDriver_AnimationDrive_ReachesOpenAtOpen01()
        {
            var go = new GameObject("Joint");
            go.AddComponent<Rigidbody>().isKinematic = true;
            var driver = go.AddComponent<OpenableJointDriver>();
            driver.driveMode = OpenCloseDriveMode.Animation;
            driver.targetOpenAngle = 90f;
            driver.usePhysicsMotor = false;
            Assert.IsTrue(driver.BeginOpen());
            driver.SetAnimationProgress(1f);
            Assert.GreaterOrEqual(driver.Open01, 0.99f);
            Assert.AreEqual(OpenableJointState.Open, driver.state);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void BakePlanToScene_PersistsStopChildren()
        {
            var host = new GameObject("Host");
            var asset = ScriptableObject.CreateInstance<OpenCloseTopologyAsset>();
            asset.Root.nodeId = "a";
            asset.AddChild(asset.Root, new OpenCloseTopologyNode { nodeId = "b", enabledInGameplay = true });
            var plan = OpenCloseTopologyCompiler.BakePlanToScene(asset, host);
            Assert.IsNotNull(plan);
            Assert.AreEqual(2, plan.children.Count);
            Assert.IsInstanceOf<OpenCloseAmbulateToStopNode>(plan.children[0]);
            Object.DestroyImmediate(host);
        }
    }
}
