using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Assigned animation references for drink tool-usage BT training and playback.</summary>
    [CreateAssetMenu(fileName = "DrinkAnimationReference", menuName = "Continuum/Drink/Animation Reference")]
    public sealed class DrinkAnimationReference : ScriptableObject
    {
        [Header("Clips")]
        public AnimationClip holdClip;
        public AnimationClip putClip;
        public AnimationClip sipClip;
        public AnimationClip nozzleLoopClip;

        [Header("ABT clip configs")]
        public ABTClipConfig holdConfig;
        public ABTClipConfig putConfig;
        public ABTClipConfig sipConfig;

        public string AssetId => name;
    }
}
