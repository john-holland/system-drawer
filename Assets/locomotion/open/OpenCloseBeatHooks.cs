using Locomotion.Narrative;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Legacy quest/music helpers. Prefer <see cref="OpenCloseBeatMessageBus"/> + router.</summary>
    public static class OpenCloseBeatHooks
    {
        public static void OnBeatOpened(BehaviorTree tree, OpenCloseBeatProfile profile)
            => OnBeatOpened(tree, profile, null, default);

        public static void OnBeatOpened(
            BehaviorTree tree,
            OpenCloseBeatProfile profile,
            string nodeId,
            Vector3 worldPos)
        {
            _ = tree;
            _ = worldPos;
            if (profile == null)
                return;

            // When a router is present, bus messages are handled there (raised by joint nodes).
            if (Object.FindAnyObjectByType<OpenCloseBeatMessageRouter>() != null)
                return;

            ApplyLegacyQuestAndMusic(profile, nodeId);
        }

        static void ApplyLegacyQuestAndMusic(OpenCloseBeatProfile profile, string nodeId)
        {
            var quest = Object.FindAnyObjectByType<QuestRunner>();
            if (quest != null && !string.IsNullOrEmpty(profile.questObjectiveId))
            {
                switch (profile.questHintKind)
                {
                    case OpenCloseQuestHintKind.Complete:
                        quest.CompleteObjective(profile.questObjectiveId);
                        break;
                    case OpenCloseQuestHintKind.Advance:
                        quest.ActivateObjective(profile.questObjectiveId);
                        break;
                    case OpenCloseQuestHintKind.Change:
                        quest.SyncGoals(new System.Collections.Generic.Dictionary<string, bool>
                        {
                            [profile.questObjectiveId] = true,
                        });
                        break;
                    case OpenCloseQuestHintKind.Note:
                        OnBeatNote(null, profile.uiMessageText);
                        break;
                }
            }

            if (profile.playMusicOnOpen || profile.musicPlan != null)
            {
                var music = Object.FindAnyObjectByType<Locomotion.Narrative.Music.CausalityMusicBridge>();
                string fromLeaf = !string.IsNullOrEmpty(profile.musicIdleLeafId) ? profile.musicIdleLeafId : "open_beat_idle";
                string toLeaf = !string.IsNullOrEmpty(profile.musicActiveLeafId)
                    ? profile.musicActiveLeafId
                    : $"open_beat_active_{(!string.IsNullOrEmpty(nodeId) ? nodeId : "beat")}";
                music?.OnCausalityLeafTransition(fromLeaf, toLeaf);
            }
        }

        public static void OnBeatNote(NarrativeExecutor executor, string note)
        {
            if (string.IsNullOrEmpty(note))
                return;
            Debug.Log($"[OpenClose] Note: {note}");
            _ = executor;
        }
    }
}
