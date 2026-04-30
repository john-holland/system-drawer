using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Scene API for sending <see cref="ThoughtData"/> between actors; register with <see cref="SystemDrawerService"/>.
    /// </summary>
    public interface IBrainMessageApi
    {
        void SendThought(ThoughtType type, Brain from, Brain to, object payload);
        bool SendThoughtByKeys(string fromKey, string toKey, ThoughtType type, object payload);
        void SetLieDetectionEnabled(Brain target, bool enabled);
    }

    /// <summary>
    /// Default implementation: optional <see cref="NarrativeBindings"/> for key resolution.
    /// Registers under <see cref="DefaultServiceKey"/> unless overridden.
    /// </summary>
    public class BrainMessageService : MonoBehaviour, IBrainMessageApi
    {
        public const string DefaultServiceKey = "actor.brain";

        [Tooltip("Optional: resolve actor keys for SendThoughtByKeys.")]
        public NarrativeBindings bindings;

        [SerializeField] private string registerKey = DefaultServiceKey;

        private void Awake()
        {
            var hub = SystemDrawerService.FindInScene();
            if (hub != null)
                hub.Register(registerKey, this);
        }

        private void OnDestroy()
        {
            var hub = SystemDrawerService.FindInScene();
            if (hub != null)
                hub.Unregister(registerKey);
        }

        public void SendThought(ThoughtType type, Brain from, Brain to, object payload)
        {
            if (from == null || to == null)
                return;
            var td = new ThoughtData(from, to, type, payload);
            from.SendThought(to, td);
        }

        public bool SendThoughtByKeys(string fromKey, string toKey, ThoughtType type, object payload)
        {
            if (bindings == null)
                bindings = FindAnyObjectByType<NarrativeBindings>();
            if (bindings == null || !bindings.TryResolveGameObject(fromKey, out var fg) || !bindings.TryResolveGameObject(toKey, out var tg))
                return false;
            var fb = fg.GetComponent<Brain>();
            var tb = tg.GetComponent<Brain>();
            if (fb == null || tb == null)
                return false;
            SendThought(type, fb, tb, payload);
            return true;
        }

        public void SetLieDetectionEnabled(Brain target, bool enabled)
        {
            if (target == null)
                return;
            target.enableLieDetection = enabled;
        }
    }
}
