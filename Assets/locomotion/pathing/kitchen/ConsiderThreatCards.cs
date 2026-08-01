using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Kitchen/Consider Threat Cards")]
public sealed class ConsiderThreatCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public ThreatWarden warden;

    public List<GoodSection> GenerateFromReport(ThreatKind kind, GameObject source = null)
    {
        if (warden == null) warden = ThreatWarden.Instance ?? GetComponent<ThreatWarden>();
        var card = warden != null
            ? warden.RaiseThreat(kind, source)
            : ThreatCard.Generate(kind, gameObject, source);
        var justice = JusticeCard.Generate(JusticeAction.ShutOffHeat, source);
        var list = new List<GoodSection> { card, justice };
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (cardSolver != null) cardSolver.AddCards(list);
        return list;
    }

    public static void AssignToNearestStaff(ThreatCard card, GameObject contextOwner, List<RetinuePeckingEntry> staff)
    {
        if (card == null) return;
        var ownerPos = contextOwner != null ? contextOwner.transform.position : Vector3.zero;
        var nearest = FindNearestStaff(ownerPos, staff);
        if (nearest == null || nearest.actor == null) return;
        var solver = nearest.actor.GetComponent<PhysicsCardSolver>();
        if (solver == null) solver = nearest.actor.AddComponent<PhysicsCardSolver>();
        var cards = new List<GoodSection> { card };
        // Justice shut-off for fire-like threats
        if (card.threatKind == ThreatKind.Fire || card.threatKind == ThreatKind.SmokeDetectorAlarm)
            cards.Add(JusticeCard.Generate(JusticeAction.ShutOffHeat, card.reportedSource));
        solver.AddCards(cards);

        // Dialog BT branch marker
        var dialog = nearest.actor.GetComponent<ThreatDialogBranch>();
        if (dialog == null) dialog = nearest.actor.AddComponent<ThreatDialogBranch>();
        dialog.Bind(card, staff);
    }

    public static RetinuePeckingEntry FindNearestStaff(Vector3 origin, List<RetinuePeckingEntry> staff)
    {
        if (staff == null || staff.Count == 0) return null;
        RetinuePeckingEntry best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < staff.Count; i++)
        {
            var e = staff[i];
            if (e == null || e.actor == null) continue;
            float dist = Vector3.Distance(origin, e.actor.transform.position);
            // Prefer lower pecking order (closer to owner of context) then proximity
            float score = e.peckingOrder * 10f + dist;
            if (score < bestScore)
            {
                bestScore = score;
                best = e;
            }
        }
        return best;
    }

    public static RetinuePeckingEntry FindSolverForContact(string contactAgency, List<RetinuePeckingEntry> staff)
    {
        if (staff == null) return null;
        RetinuePeckingEntry best = null;
        int bestOrder = int.MaxValue;
        for (int i = 0; i < staff.Count; i++)
        {
            var e = staff[i];
            if (e == null) continue;
            bool match = string.Equals(e.agencyAffinity, contactAgency, System.StringComparison.OrdinalIgnoreCase)
                         || string.Equals(e.role, contactAgency, System.StringComparison.OrdinalIgnoreCase)
                         || (contactAgency == ThreatAgencyId.BuildingMaintenance && e.role != null && e.role.Contains("maintenance"));
            if (!match) continue;
            if (e.peckingOrder < bestOrder)
            {
                bestOrder = e.peckingOrder;
                best = e;
            }
        }
        return best;
    }
}

/// <summary>Attached to nearest staff: telecom escalation along pecking order / contacts.</summary>
[AddComponentMenu("Locomotion/Kitchen/Threat Dialog Branch")]
public sealed class ThreatDialogBranch : MonoBehaviour
{
    public ThreatCard boundThreat;
    public List<string> dialogSuggestions = new List<string>();
    List<RetinuePeckingEntry> _staff;

    public void Bind(ThreatCard card, List<RetinuePeckingEntry> staff)
    {
        boundThreat = card;
        _staff = staff;
        dialogSuggestions.Clear();
        if (card == null) return;
        dialogSuggestions.Add($"Report {card.threatKind} ({card.alertLemma})");
        if (card.telecomContacts != null)
        {
            for (int i = 0; i < card.telecomContacts.Count; i++)
                dialogSuggestions.Add($"Telecom {card.telecomContacts[i]}");
        }
        EscalateTelecom();
    }

    public void EscalateTelecom()
    {
        if (boundThreat == null || boundThreat.telecomContacts == null) return;
        for (int i = 0; i < boundThreat.telecomContacts.Count; i++)
        {
            var contact = boundThreat.telecomContacts[i];
            var solver = ConsiderThreatCards.FindSolverForContact(contact, _staff);
            if (solver != null && solver.actor != null)
            {
                var pcs = solver.actor.GetComponent<PhysicsCardSolver>() ?? solver.actor.AddComponent<PhysicsCardSolver>();
                pcs.AddCards(new List<GoodSection> { boundThreat });
                return;
            }
        }
    }
}
