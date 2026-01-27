using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to carry an object.
    /// </summary>
    [Serializable]
    public class NarrativeCarryAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the object to carry")]
        public string objectToCarryKey = "object";

        [Tooltip("Key resolved via NarrativeBindings for the carrier GameObject")]
        public string carrierKey = "carrier";

        [Tooltip("Attachment point relative to carrier (optional)")]
        public Vector3 attachmentPoint = Vector3.zero;

        [Tooltip("Use physics joint for carrying")]
        public bool usePhysicsJoint = true;

        [NonSerialized]
        private bool isCarrying = false;

        [NonSerialized]
        private FixedJoint carryJoint;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(carrierKey, out var carrierGo) || carrierGo == null)
            {
                Debug.LogWarning("[NarrativeCarryAction] Could not resolve carrier GameObject");
                return BehaviorTreeStatus.Failure;
            }

            if (!ctx.TryResolveGameObject(objectToCarryKey, out var objectGo) || objectGo == null)
            {
                Debug.LogWarning("[NarrativeCarryAction] Could not resolve object to carry");
                return BehaviorTreeStatus.Failure;
            }

            if (isCarrying)
            {
                // Already carrying - check if still valid
                if (carryJoint != null && carryJoint.connectedBody != null)
                {
                    return BehaviorTreeStatus.Running;
                }
                else
                {
                    // Joint broken or invalid
                    isCarrying = false;
                }
            }

            // Start carrying
            if (StartCarrying(carrierGo, objectGo))
            {
                isCarrying = true;
                return BehaviorTreeStatus.Running;
            }

            return BehaviorTreeStatus.Failure;
        }

        private bool StartCarrying(GameObject carrier, GameObject objectToCarry)
        {
            // Get or create attachment point
            Transform attachmentTransform = null;
            if (attachmentPoint != Vector3.zero)
            {
                // Create attachment point GameObject
                GameObject attachmentPointObj = new GameObject("CarryAttachmentPoint");
                attachmentPointObj.transform.SetParent(carrier.transform);
                attachmentPointObj.transform.localPosition = attachmentPoint;
                attachmentTransform = attachmentPointObj.transform;
            }
            else
            {
                // Use carrier's transform
                attachmentTransform = carrier.transform;
            }

            // Get rigidbodies
            Rigidbody carrierRb = carrier.GetComponent<Rigidbody>();
            Rigidbody objectRb = objectToCarry.GetComponent<Rigidbody>();

            if (carrierRb == null || objectRb == null)
            {
                Debug.LogWarning("[NarrativeCarryAction] Carrier or object missing Rigidbody");
                return false;
            }

            if (usePhysicsJoint)
            {
                // Create fixed joint
                carryJoint = objectToCarry.AddComponent<FixedJoint>();
                carryJoint.connectedBody = carrierRb;
                carryJoint.breakForce = Mathf.Infinity;
                carryJoint.breakTorque = Mathf.Infinity;
            }
            else
            {
                // Use parent-child relationship
                objectToCarry.transform.SetParent(attachmentTransform);
            }

            // Generate carry card for physics solver if available
            GenerateCarryCard(carrier, objectToCarry);

            return true;
        }

        private void GenerateCarryCard(GameObject carrier, GameObject objectToCarry)
        {
            // Generate a physics card for the carry action using reflection
            var cardSolverType = System.Type.GetType("PhysicsCardSolver, Assembly-CSharp");
            if (cardSolverType != null)
            {
                var cardSolver = carrier.GetComponent(cardSolverType);
                if (cardSolver != null)
                {
                    // Create a carry card using reflection
                    var goodSectionType = System.Type.GetType("GoodSection, Assembly-CSharp");
                    if (goodSectionType != null)
                    {
                        var carryCard = System.Activator.CreateInstance(goodSectionType);
                        if (carryCard != null)
                        {
                            // Set properties using reflection
                            var nameProp = goodSectionType.GetProperty("sectionName");
                            var descProp = goodSectionType.GetProperty("description");
                            if (nameProp != null)
                                nameProp.SetValue(carryCard, "Carry_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
                            if (descProp != null)
                                descProp.SetValue(carryCard, $"Carry {objectToCarry.name}");

                            // Add to solver's available cards
                            var availableCardsProp = cardSolverType.GetProperty("availableCards");
                            if (availableCardsProp != null)
                            {
                                var cards = availableCardsProp.GetValue(cardSolver) as System.Collections.IList;
                                if (cards != null && !cards.Contains(carryCard))
                                {
                                    cards.Add(carryCard);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stop carrying.
        /// </summary>
        public void StopCarrying()
        {
            if (carryJoint != null)
            {
                UnityEngine.Object.Destroy(carryJoint);
                carryJoint = null;
            }

            isCarrying = false;
        }
    }
}
