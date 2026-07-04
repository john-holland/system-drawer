using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Middle-mouth anchor for nozzle alignment and floosh targeting.</summary>
    public sealed class DrinkMouthAnchor : MonoBehaviour
    {
        [Tooltip("Optional offset from auto-detected jaw center.")]
        public Vector3 localOffset;

        public static DrinkMouthAnchor FindOrCreate(RagdollSystem ragdoll)
        {
            if (ragdoll == null)
                return null;
            var existing = ragdoll.GetComponentInChildren<DrinkMouthAnchor>();
            if (existing != null)
                return existing;
            var jaw = ragdoll.GetComponentInChildren<Locomotion.Musculature.RagdollJaw>();
            if (jaw == null)
                return null;
            var go = new GameObject("DrinkMouthAnchor");
            go.transform.SetParent(jaw.transform, false);
            var anchor = go.AddComponent<DrinkMouthAnchor>();
            go.transform.localPosition = anchor.localOffset;
            return anchor;
        }

        public Vector3 WorldPosition => transform.position;
    }
}
