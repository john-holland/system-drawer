using Locomotion.Musculature;
using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Orients nozzle toward mouth and drives jaw opening during sips.</summary>
    public sealed class DrinkMouthJawAligner : MonoBehaviour
    {
        public Transform nozzleTip;
        public RagdollJaw jaw;
        public DrinkMouthAnchor mouthAnchor;

        DrinkLemmaProperties _activeProps;

        void Awake()
        {
            if (jaw == null)
                jaw = GetComponentInChildren<RagdollJaw>();
            if (mouthAnchor == null)
            {
                var ragdoll = GetComponentInParent<RagdollSystem>();
                if (ragdoll != null)
                    mouthAnchor = DrinkMouthAnchor.FindOrCreate(ragdoll);
            }
        }

        public void BeginSip(DrinkLemmaProperties props)
        {
            _activeProps = props;
            if (props.autoMiddleMouthJaw && mouthAnchor == null)
            {
                var ragdoll = GetComponentInParent<RagdollSystem>();
                if (ragdoll != null)
                    mouthAnchor = DrinkMouthAnchor.FindOrCreate(ragdoll);
            }
            if (props.placeNozzleOnMouth && nozzleTip != null && mouthAnchor != null)
                AlignNozzleToMouth();
            if (jaw != null)
                jaw.jawOpenAmount = Mathf.Lerp(jaw.jawOpenAmount, props.drinkEfficacy * 0.5f, 0.25f);
        }

        void AlignNozzleToMouth()
        {
            if (nozzleTip == null || mouthAnchor == null)
                return;
            Vector3 toMouth = mouthAnchor.WorldPosition - nozzleTip.position;
            if (toMouth.sqrMagnitude < 1e-6f)
                return;
            nozzleTip.rotation = Quaternion.LookRotation(toMouth.normalized, Vector3.up);
        }

        public void TickFlow(float flowLitersPerSecond, float deltaTime)
        {
            if (jaw == null || deltaTime <= 0f)
                return;
            float target = Mathf.Clamp01(_activeProps.drinkEfficacy + flowLitersPerSecond * 0.1f);
            jaw.jawOpenAmount = Mathf.Lerp(jaw.jawOpenAmount, target, deltaTime * 4f);
        }
    }
}
