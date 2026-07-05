using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Generates open/close GoodSection cards from nearby jointed panels.</summary>
    [DisallowMultipleComponent]
    public sealed class ConsiderOpenCloseCards : MonoBehaviour
    {
        public const string TagOpen = "open_close_open";
        public const string TagClose = "open_close_close";

        [SerializeField] PhysicsCardSolver cardSolver;
        [SerializeField] float scanRangeM = 4f;
        [SerializeField] LayerMask panelMask = ~0;

        readonly List<GoodSection> _generated = new List<GoodSection>();

        void Awake()
        {
            if (cardSolver == null)
                cardSolver = GetComponent<PhysicsCardSolver>();
        }

        public List<GoodSection> GenerateCards()
        {
            _generated.Clear();
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, panelMask, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (c == null)
                    continue;
                var driver = c.GetComponentInParent<OpenableJointDriver>();
                if (driver == null)
                    continue;
                _generated.Add(BuildOpenCard(driver));
                _generated.Add(BuildCloseCard(driver));
            }

            if (cardSolver != null)
                cardSolver.AddCards(_generated);
            return _generated;
        }

        static GoodSection BuildOpenCard(OpenableJointDriver driver)
        {
            return new GoodSection
            {
                sectionName = $"open_{driver.name}",
                description = "Pull/push to open panel",
                traversabilityMode = TraversabilityMode.Custom,
                physicalPathingTag = TagOpen,
                enablesTraversability = true,
                limits = new SectionLimits { maxTorque = 200f, maxDegreesDifference = driver.targetOpenAngle },
            };
        }

        static GoodSection BuildCloseCard(OpenableJointDriver driver)
        {
            return new GoodSection
            {
                sectionName = $"close_{driver.name}",
                description = "Push/pull to close panel",
                traversabilityMode = TraversabilityMode.Custom,
                physicalPathingTag = TagClose,
                enablesTraversability = true,
                limits = new SectionLimits { maxTorque = 200f, maxDegreesDifference = driver.targetOpenAngle },
            };
        }

        public static bool IsOpenCard(GoodSection card) =>
            card != null && card.physicalPathingTag == TagOpen;
    }
}
