using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Holds a table-read / Whisper-matched voice line: actor id, script offsets, USC audio id.
    /// DialogueRunner plays via ActorSpeechPlayback using audioRef / uscAudioId.
    /// </summary>
    public sealed class VoiceActorLineComponent : MonoBehaviour
    {
        public string dialogActorId;
        public int charStart;
        public int charEnd;
        public string uscAudioId;
        public string quoteText;

        public string AudioRef => uscAudioId;
    }
}
