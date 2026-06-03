using UnityEngine;

namespace Locomotion.Spaceship
{
    public enum PosteringMode
    {
        None,
        Mild,
        Badger,
        HoneyBadger,
        AadLandshark,
        SarlacPit
    }

    public sealed class PosteringComponent : MonoBehaviour
    {
        public PosteringMode mode = PosteringMode.None;
        public float rhythmSeconds = 2f;
        public AnimationCurve aggressivenessPulse = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 1f)] public float aggressiveness;

        void Update()
        {
            if (mode == PosteringMode.None)
                return;
            float amp = mode switch
            {
                PosteringMode.Mild => 0.15f,
                PosteringMode.Badger => 0.35f,
                PosteringMode.HoneyBadger => 0.55f,
                PosteringMode.AadLandshark => 0.75f,
                PosteringMode.SarlacPit => 1f,
                _ => 0f
            };
            float t = (Time.time % rhythmSeconds) / rhythmSeconds;
            aggressiveness = Mathf.Clamp01(aggressivenessPulse.Evaluate(t) * amp);
        }
    }
}
