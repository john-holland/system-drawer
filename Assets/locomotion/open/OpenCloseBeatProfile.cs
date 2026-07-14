using System.Collections.Generic;
using Locomotion.Narrative.Music;
using UnityEngine;

namespace Locomotion.Open
{
    [CreateAssetMenu(fileName = "OpenCloseBeatProfile", menuName = "Locomotion/Open-Close Beat Profile")]
    public sealed class OpenCloseBeatProfile : ScriptableObject
    {
        [Header("Ambulation")]
        public float arrivalBlendCoefficient = 0f;
        public float reachRadiusMeters = 0.6f;
        public bool requireFacingTarget = true;
        public AutoCloseBtMode autoCloseBt = AutoCloseBtMode.OnStopExit;

        [Header("Open/Close")]
        public float openAngleDeg = 90f;
        public OpenCloseDriveMode driveMode = OpenCloseDriveMode.Hybrid;
        public string openAnimationRef;
        public string closeAnimationRef;
        public string actorIkProfileRef;
        public string objectIkProfileRef;

        [Header("Audio / narrative")]
        public AudioClip soundOpen;
        public AudioClip soundClose;
        public string dialogueSpanRef;
        public OpenCloseQuestHintKind questHintKind = OpenCloseQuestHintKind.None;
        public string questObjectiveId;
        public bool autoCloseOnExit;
        public bool playMusicOnOpen;
        public MusicCompositionPlanAsset musicPlan;
        public string musicIdleLeafId = "open_beat_idle";
        public string musicActiveLeafId;

        [Header("UI messages")]
        public string uiMessageId;
        public string uiMessageText;
        public string uiCloseMessageText;

        [Header("IK training")]
        public RagdollAnimationSet actorOpenSet;
        public RagdollAnimationSet actorCloseSet;
        public PhysicsIKTrainingRunAsset actorOpenTraining;
        public PhysicsIKTrainingRunAsset actorCloseTraining;
        public PhysicsIKTrainingRunAsset objectOpenTraining;
        public List<BehaviorTreeGoal> toolUsageGoals = new List<BehaviorTreeGoal>();

        public void CopyFromNode(OpenCloseTopologyNode node)
        {
            if (node == null)
                return;
            arrivalBlendCoefficient = node.arrivalBlendCoefficient;
            reachRadiusMeters = node.reachRadiusMeters;
            requireFacingTarget = node.requireFacingTarget;
            autoCloseBt = node.autoCloseBt;
        }
    }
}
