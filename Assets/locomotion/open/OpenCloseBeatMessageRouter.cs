using Locomotion.Narrative;
using Locomotion.Narrative.Music;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Scene wiring: fans <see cref="OpenCloseBeatMessageBus"/> into quest / music / dialogue / debug UI.
    /// </summary>
    public sealed class OpenCloseBeatMessageRouter : MonoBehaviour
    {
        public QuestRunner questRunner;
        public CausalityMusicBridge musicBridge;
        public NarrativeExecutor narrativeExecutor;
        public bool logUiMessages = true;

        void OnEnable() => OpenCloseBeatMessageBus.Raised += OnMessage;
        void OnDisable() => OpenCloseBeatMessageBus.Raised -= OnMessage;

        void OnMessage(OpenCloseBeatMessage msg)
        {
            switch (msg.channel)
            {
                case OpenCloseBeatChannel.Quest:
                    HandleQuest(msg);
                    break;
                case OpenCloseBeatChannel.Music:
                    HandleMusic(msg);
                    break;
                case OpenCloseBeatChannel.Dialogue:
                    HandleDialogue(msg);
                    break;
                case OpenCloseBeatChannel.UI:
                    if (logUiMessages)
                        Debug.Log($"[OpenClose UI] {msg.phase} node={msg.nodeId} id={msg.refId} text={msg.text}");
                    break;
                case OpenCloseBeatChannel.Sound:
                    // Clip playback remains immediate in Open/Close joint nodes; bus is for listeners.
                    break;
            }
        }

        void HandleQuest(OpenCloseBeatMessage msg)
        {
            var quest = questRunner != null ? questRunner : Object.FindAnyObjectByType<QuestRunner>();
            var profile = msg.profile;
            if (quest == null || profile == null)
                return;

            if (msg.phase == OpenCloseBeatPhase.Note || profile.questHintKind == OpenCloseQuestHintKind.Note)
            {
                OpenCloseBeatHooks.OnBeatNote(narrativeExecutor, !string.IsNullOrEmpty(msg.text) ? msg.text : profile.uiMessageText);
                return;
            }

            if (string.IsNullOrEmpty(profile.questObjectiveId))
                return;

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
            }
        }

        void HandleMusic(OpenCloseBeatMessage msg)
        {
            var music = musicBridge != null ? musicBridge : Object.FindAnyObjectByType<CausalityMusicBridge>();
            if (music == null)
                return;
            string fromLeaf = !string.IsNullOrEmpty(msg.refId) ? msg.refId : "open_beat_idle";
            string toLeaf = !string.IsNullOrEmpty(msg.text) ? msg.text : $"open_beat_active_{msg.nodeId}";
            music.OnCausalityLeafTransition(fromLeaf, toLeaf);
        }

        void HandleDialogue(OpenCloseBeatMessage msg)
        {
            if (string.IsNullOrEmpty(msg.refId) && string.IsNullOrEmpty(msg.text))
                return;
            OpenCloseBeatHooks.OnBeatNote(narrativeExecutor, !string.IsNullOrEmpty(msg.refId) ? msg.refId : msg.text);
        }
    }
}
