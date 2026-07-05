using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Non-physics guard gate before child hinge can open.</summary>
    public sealed class OpenableLatch : MonoBehaviour
    {
        public bool isUnlocked;
        public string requiredToolLemma;
        public AudioClip unlockClip;

        public bool TryUnlock(string toolLemma = null)
        {
            if (isUnlocked)
                return true;
            if (!string.IsNullOrEmpty(requiredToolLemma) &&
                !string.Equals(requiredToolLemma, toolLemma, System.StringComparison.OrdinalIgnoreCase))
                return false;
            isUnlocked = true;
            if (unlockClip != null)
                AudioSource.PlayClipAtPoint(unlockClip, transform.position);
            return true;
        }

        public void Relock()
        {
            isUnlocked = false;
        }
    }
}
