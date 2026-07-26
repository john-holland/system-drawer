using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates rope-specific GoodSection cards (grapple, wind, climb, coil) from environment anchors.
/// </summary>
[DisallowMultipleComponent]
public class ConsiderRopeCards : MonoBehaviour
{
    public const string TagGrapple = "rope_grapple";
    public const string TagWind = "rope_wind";
    public const string TagClimb = "rope_climb";
    public const string TagCoil = "rope_coil";

    [SerializeField] RopeSystem ropeSystem;
    [SerializeField] PhysicsCardSolver cardSolver;
    [SerializeField] float scanRangeM = 12f;
    [SerializeField] float minClimbTension = 50f;
    [SerializeField] LayerMask anchorMask = ~0;
    [Tooltip("Emit inchworm mantle/lower/ledge cards with rope.* tags.")]
    [SerializeField] bool emitInchwormCards = true;
    [Tooltip("When true, climb cards carry attach_sherpa_carry tag.")]
    [SerializeField] bool attachSherpaCarry;

    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (ropeSystem == null)
            ropeSystem = GetComponent<RopeSystem>();
        if (cardSolver == null)
            cardSolver = GetComponent<PhysicsCardSolver>();
    }

    public List<GoodSection> GenerateCards()
    {
        _generated.Clear();
        if (ropeSystem == null)
            return _generated;

        Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, anchorMask, QueryTriggerInteraction.Ignore);
        foreach (Collider c in hits)
        {
            if (c == null)
                continue;
            if (c.CompareTag("RopeAnchor") || c.name.ToLowerInvariant().Contains("anchor"))
                _generated.Add(BuildGrappleCard(c.transform));
            if (c.CompareTag("RopeSpool") || c.name.ToLowerInvariant().Contains("spool"))
            {
                _generated.Add(BuildWindCard(c.transform, 1f));
                _generated.Add(BuildWindCard(c.transform, -1f));
            }
        }

        if (ropeSystem.NormalizedLoad < 0.95f && ropeSystem.MaxTensionN >= minClimbTension)
        {
            _generated.Add(BuildClimbCard());
            if (emitInchwormCards)
            {
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.MantleLeft, "Mantle left"));
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.MantleRight, "Mantle right"));
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.Lowering, "Lowering"));
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.ClimbingUp, "Climbing up"));
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.ClimbOntoLedge, "Climb onto ledge"));
                _generated.Add(BuildInchwormCard(RopeInchwormAnimationGroup.Idling, "Rope idle"));
            }
        }

        if (ropeSystem.OverlapIndex == null || !ropeSystem.OverlapIndex.HasTangle)
            _generated.Add(BuildCoilCard());

        if (cardSolver != null)
            cardSolver.AddCards(_generated);

        return _generated;
    }

    GoodSection BuildGrappleCard(Transform anchor)
    {
        return new GoodSection
        {
            sectionName = $"grapple_{anchor.name}",
            description = "Attach rope to anchor",
            traversabilityMode = TraversabilityMode.Custom,
            physicalPathingTag = TagGrapple,
            enablesTraversability = true,
            limits = new SectionLimits { maxForce = ropeSystem.TotalBreakTensionN }
        };
    }

    GoodSection BuildWindCard(Transform spool, float sign)
    {
        string dir = sign > 0f ? "reel_in" : "pay_out";
        return new GoodSection
        {
            sectionName = $"{dir}_{spool.name}",
            description = sign > 0f ? "Reel rope in" : "Pay rope out",
            traversabilityMode = TraversabilityMode.Custom,
            physicalPathingTag = TagWind,
            enablesTraversability = true
        };
    }

    GoodSection BuildClimbCard()
    {
        string tag = TagClimb;
        if (attachSherpaCarry)
            tag = TagClimb + "," + RopeInchwormAnimationGroup.TagSherpaCarry;
        return new GoodSection
        {
            sectionName = "climb_tension_rope",
            description = attachSherpaCarry ? "Climb tensioned rope (sherpa)" : "Climb tensioned rope",
            traversabilityMode = TraversabilityMode.Climb,
            physicalPathingTag = tag,
            enablesTraversability = true,
            limits = new SectionLimits { maxForce = ropeSystem.TotalBreakTensionN * 0.5f }
        };
    }

    GoodSection BuildInchwormCard(string inchwormTag, string label)
    {
        string tag = inchwormTag;
        if (attachSherpaCarry)
            tag = inchwormTag + "," + RopeInchwormAnimationGroup.TagSherpaCarry;
        return new GoodSection
        {
            sectionName = inchwormTag.Replace('.', '_'),
            description = label,
            traversabilityMode = TraversabilityMode.Climb,
            physicalPathingTag = tag,
            enablesTraversability = true,
            limits = new SectionLimits { maxForce = ropeSystem.TotalBreakTensionN * 0.45f }
        };
    }

    GoodSection BuildCoilCard()
    {
        return new GoodSection
        {
            sectionName = "coil_rope",
            description = "Coil rope at surface",
            traversabilityMode = TraversabilityMode.Custom,
            physicalPathingTag = TagCoil,
            enablesTraversability = true
        };
    }

    public bool IsCardFeasible(GoodSection card)
    {
        if (card == null || ropeSystem == null)
            return false;
        if (card.physicalPathingTag == TagGrapple || card.physicalPathingTag == TagClimb)
            return ropeSystem.NormalizedLoad < 1f;
        if (card.physicalPathingTag == TagWind)
            return ropeSystem.OverlapIndex == null || !ropeSystem.OverlapIndex.HasTangle;
        return true;
    }
}
