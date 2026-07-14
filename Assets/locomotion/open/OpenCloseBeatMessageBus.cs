using System;
using UnityEngine;

namespace Locomotion.Open
{
    public enum OpenCloseBeatChannel
    {
        Sound,
        Dialogue,
        Quest,
        UI,
        Music,
    }

    public enum OpenCloseBeatPhase
    {
        Open,
        Close,
        Note,
        Unlock,
    }

    public readonly struct OpenCloseBeatMessage
    {
        public readonly OpenCloseBeatChannel channel;
        public readonly OpenCloseBeatPhase phase;
        public readonly string nodeId;
        public readonly string text;
        public readonly string refId;
        public readonly AudioClip clip;
        public readonly OpenCloseBeatProfile profile;
        public readonly Vector3 worldPosition;

        public OpenCloseBeatMessage(
            OpenCloseBeatChannel channel,
            OpenCloseBeatPhase phase,
            string nodeId,
            string text = null,
            string refId = null,
            AudioClip clip = null,
            OpenCloseBeatProfile profile = null,
            Vector3 worldPosition = default)
        {
            this.channel = channel;
            this.phase = phase;
            this.nodeId = nodeId ?? "";
            this.text = text ?? "";
            this.refId = refId ?? "";
            this.clip = clip;
            this.profile = profile;
            this.worldPosition = worldPosition;
        }
    }

    /// <summary>Unified sound / dialogue / quest / UI / music beat events for open-close topology.</summary>
    public static class OpenCloseBeatMessageBus
    {
        public static event Action<OpenCloseBeatMessage> Raised;

        public static void Raise(OpenCloseBeatMessage message) => Raised?.Invoke(message);

        public static void RaiseOpenBeat(string nodeId, OpenCloseBeatProfile profile, Vector3 worldPos)
        {
            if (profile == null)
                return;

            if (profile.soundOpen != null)
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Sound, OpenCloseBeatPhase.Open, nodeId,
                    clip: profile.soundOpen, profile: profile, worldPosition: worldPos));
            }

            if (!string.IsNullOrEmpty(profile.dialogueSpanRef))
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Dialogue, OpenCloseBeatPhase.Open, nodeId,
                    text: profile.dialogueSpanRef, refId: profile.dialogueSpanRef, profile: profile));
            }

            if (profile.questHintKind != OpenCloseQuestHintKind.None && !string.IsNullOrEmpty(profile.questObjectiveId))
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Quest, OpenCloseBeatPhase.Open, nodeId,
                    text: profile.questHintKind.ToString(), refId: profile.questObjectiveId, profile: profile));
            }
            else if (profile.questHintKind == OpenCloseQuestHintKind.Note)
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Quest, OpenCloseBeatPhase.Note, nodeId,
                    text: profile.uiMessageText, refId: profile.questObjectiveId, profile: profile));
            }

            if (!string.IsNullOrEmpty(profile.uiMessageId) || !string.IsNullOrEmpty(profile.uiMessageText))
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.UI, OpenCloseBeatPhase.Open, nodeId,
                    text: profile.uiMessageText, refId: profile.uiMessageId, profile: profile));
            }

            if (profile.playMusicOnOpen || profile.musicPlan != null)
            {
                string toLeaf = !string.IsNullOrEmpty(profile.musicActiveLeafId)
                    ? profile.musicActiveLeafId
                    : $"open_beat_active_{nodeId}";
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Music, OpenCloseBeatPhase.Open, nodeId,
                    text: toLeaf, refId: profile.musicIdleLeafId ?? "open_beat_idle", profile: profile));
            }
        }

        public static void RaiseCloseBeat(string nodeId, OpenCloseBeatProfile profile, Vector3 worldPos)
        {
            if (profile == null)
                return;

            if (profile.soundClose != null)
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.Sound, OpenCloseBeatPhase.Close, nodeId,
                    clip: profile.soundClose, profile: profile, worldPosition: worldPos));
            }

            if (!string.IsNullOrEmpty(profile.uiMessageId) || !string.IsNullOrEmpty(profile.uiCloseMessageText))
            {
                Raise(new OpenCloseBeatMessage(
                    OpenCloseBeatChannel.UI, OpenCloseBeatPhase.Close, nodeId,
                    text: !string.IsNullOrEmpty(profile.uiCloseMessageText) ? profile.uiCloseMessageText : profile.uiMessageText,
                    refId: profile.uiMessageId, profile: profile));
            }
        }

        public static void RaiseUnlock(string nodeId, OpenCloseBeatProfile profile, Vector3 worldPos)
        {
            Raise(new OpenCloseBeatMessage(
                OpenCloseBeatChannel.Sound, OpenCloseBeatPhase.Unlock, nodeId,
                profile: profile, worldPosition: worldPos));
        }
    }
}
