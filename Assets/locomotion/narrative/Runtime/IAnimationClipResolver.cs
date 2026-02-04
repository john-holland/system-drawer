using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Resolves a stable key (e.g. generator key + "_clip") to an AnimationClip.
    /// Implemented by AssetLoader so narrative can play generated animations by key.
    /// </summary>
    public interface IAnimationClipResolver
    {
        AnimationClip ResolveClip(string key);
    }
}
