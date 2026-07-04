using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Resolves a procedural generator asset to an AudioClip.
    /// Implemented by DynamicMusicGenerator so narrative music code stays decoupled from Generated.Runtime.
    /// </summary>
    public interface IProceduralAudioSource
    {
        AudioClip ResolveAudioClip();
    }
}
