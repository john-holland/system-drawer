using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to drop a carried object.
    /// </summary>
    [Serializable]
    public class NarrativeDropAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the object to drop")]
        public string objectToDropKey = "object";

        [Tooltip("Drop position (world space, optional - uses current position if zero)")]
        public Vector3 dropPosition = Vector3.zero;

        [Tooltip("Initial drop velocity (optional)")]
        public Vector3 dropVelocity = Vector3.zero;

        [Tooltip("Remove physics joint when dropping")]
        public bool removeJoint = true;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(objectToDropKey, out var objectGo) || objectGo == null)
            {
                Debug.LogWarning("[NarrativeDropAction] Could not resolve object to drop");
                return BehaviorTreeStatus.Failure;
            }

            // Remove joint if present
            if (removeJoint)
            {
                FixedJoint joint = objectGo.GetComponent<FixedJoint>();
                if (joint != null)
                {
                    UnityEngine.Object.Destroy(joint);
                }
            }

            // Remove from parent if parented
            if (objectGo.transform.parent != null)
            {
                objectGo.transform.SetParent(null);
            }

            // Set position if specified
            if (dropPosition != Vector3.zero)
            {
                objectGo.transform.position = dropPosition;
            }

            // Apply velocity if specified
            if (dropVelocity != Vector3.zero)
            {
                Rigidbody rb = objectGo.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = dropVelocity;
                }
            }

            // Generate drop card for physics solver
            GenerateDropCard(objectGo);

            return BehaviorTreeStatus.Success;
        }

        private void GenerateDropCard(GameObject objectToDrop)
        {
            // Find physics card solver in scene using reflection
            var cardSolverType = System.Type.GetType("PhysicsCardSolver, Assembly-CSharp");
            if (cardSolverType != null)
            {
                var cardSolver = UnityEngine.Object.FindObjectOfType(cardSolverType);
                if (cardSolver != null)
                {
                    // Create a drop card using reflection
                    var goodSectionType = System.Type.GetType("GoodSection, Assembly-CSharp");
                    if (goodSectionType != null)
                    {
                        var dropCard = System.Activator.CreateInstance(goodSectionType);
                        if (dropCard != null)
                        {
                            // Set properties using reflection
                            var nameProp = goodSectionType.GetProperty("sectionName");
                            var descProp = goodSectionType.GetProperty("description");
                            if (nameProp != null)
                                nameProp.SetValue(dropCard, "Drop_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
                            if (descProp != null)
                                descProp.SetValue(dropCard, $"Drop {objectToDrop.name}");

                            // Add to solver's available cards
                            var availableCardsProp = cardSolverType.GetProperty("availableCards");
                            if (availableCardsProp != null)
                            {
                                var cards = availableCardsProp.GetValue(cardSolver) as System.Collections.IList;
                                if (cards != null && !cards.Contains(dropCard))
                                {
                                    cards.Add(dropCard);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
