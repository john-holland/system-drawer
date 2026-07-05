using Locomotion.Narrative;
using Locomotion.Narrative.Music;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Quest, music, and narrative hooks on open/close beats.</summary>
    public static class OpenCloseBeatHooks
    {
        public static void OnBeatOpened(BehaviorTree tree, OpenCloseBeatProfile profile)
        {
            if (profile == null)
                return;

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
                        var goals = new System.Collections.Generic.Dictionary<string, bool>
                        {
                            [profile.questObjectiveId] = true,
                        };
                        quest.SyncGoals(goals);
                        break;
                }
            }

            if (profile.playMusicOnOpen)
            {
                var music = Object.FindAnyObjectByType<CausalityMusicBridge>();
                music?.OnCausalityLeafTransition("open_beat_idle", "music_box_beat_active");
            }
        }

        public static void OnBeatNote(NarrativeExecutor executor, string note)
        {
            if (string.IsNullOrEmpty(note))
                return;
            Debug.Log($"[OpenClose] Note: {note}");
        }
    }
}
