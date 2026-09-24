using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Overlap ragdoll colliders with measurement objects and activate GoodSections
/// through Consider / NervousSystem. Safe in edit mode after Physics.SyncTransforms.
/// </summary>
public static class GoodSectionContactActivation
{
    public struct TickResult
    {
        public int contactCount;
        public int sectionsEnabled;
        public readonly List<GameObject> contacts;
        public readonly List<GoodSection> sections;

        public TickResult(int dummy)
        {
            contactCount = 0;
            sectionsEnabled = 0;
            contacts = new List<GameObject>();
            sections = new List<GoodSection>();
        }
    }

    public static TickResult Tick(
        RagdollSystem ragdoll,
        IList<GameObject> objects,
        InteractedObjectCheckpoint checkpoint = null)
    {
        var result = new TickResult(0);
        if (ragdoll == null || objects == null || objects.Count == 0)
            return result;

        Physics.SyncTransforms();
        var ragdollColliders = ragdoll.GetComponentsInChildren<Collider>(true);
        if (ragdollColliders == null || ragdollColliders.Length == 0)
            return result;

        var nervous = ragdoll.GetComponent<NervousSystem>() ?? ragdoll.GetComponentInParent<NervousSystem>()
                      ?? ragdoll.GetComponentInChildren<NervousSystem>();
        var solver = ragdoll.GetComponent<PhysicsCardSolver>() ?? ragdoll.GetComponentInParent<PhysicsCardSolver>()
                     ?? ragdoll.GetComponentInChildren<PhysicsCardSolver>();
        var seen = new HashSet<int>();

        for (int oi = 0; oi < objects.Count; oi++)
        {
            var go = objects[oi];
            if (go == null || !go.activeInHierarchy)
                continue;
            var cols = go.GetComponentsInChildren<Collider>(true);
            if (cols == null || cols.Length == 0)
            {
                if (OverlapsBounds(ragdollColliders, go))
                    ActivateContact(go, ragdoll, nervous, solver, checkpoint, ref result, seen);
                continue;
            }
            bool hit = false;
            for (int ri = 0; ri < ragdollColliders.Length && !hit; ri++)
            {
                var rc = ragdollColliders[ri];
                if (rc == null || !rc.enabled)
                    continue;
                for (int ci = 0; ci < cols.Length; ci++)
                {
                    var oc = cols[ci];
                    if (oc == null || !oc.enabled)
                        continue;
                    if (rc.bounds.Intersects(oc.bounds))
                    {
                        hit = true;
                        break;
                    }
                }
            }
            if (hit)
                ActivateContact(go, ragdoll, nervous, solver, checkpoint, ref result, seen);
        }

        if (nervous != null)
            nervous.PumpImpulsesForEditor();
        return result;
    }

    static bool OverlapsBounds(Collider[] ragdollColliders, GameObject go)
    {
        var b = new Bounds(go.transform.position, Vector3.one * 0.25f);
        var r = go.GetComponentInChildren<Renderer>();
        if (r != null)
            b = r.bounds;
        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            if (ragdollColliders[i] != null && ragdollColliders[i].bounds.Intersects(b))
                return true;
        }
        return false;
    }

    static void ActivateContact(
        GameObject go,
        RagdollSystem ragdoll,
        NervousSystem nervous,
        PhysicsCardSolver solver,
        InteractedObjectCheckpoint checkpoint,
        ref TickResult result,
        HashSet<int> seen)
    {
        int id = go.GetInstanceID();
        if (!seen.Add(id))
            return;
        checkpoint?.RememberFirstSeen(go);
        result.contacts.Add(go);
        result.contactCount++;

        var sensory = new SensoryData(go.transform.position, Vector3.up, 1f, go, "Contact");
        if (nervous != null)
        {
            var impulse = new ImpulseData(ImpulseType.Sensory, "EditModeContact", "NervousSystem", sensory);
            nervous.SendImpulseUp("Spinal", impulse);
            var sections = nervous.GetAvailableGoodSections(go);
            if (sections != null)
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    if (sections[i] == null)
                        continue;
                    result.sections.Add(sections[i]);
                    checkpoint?.MarkDirtyFromGoodSection(sections[i], solver);
                }
                if (solver != null && sections.Count > 0)
                    solver.AddCards(sections);
                result.sectionsEnabled += sections.Count;
            }
        }
        else
        {
            var consider = ragdoll.GetComponentInChildren<Consider>();
            if (consider != null)
            {
                var cards = consider.GenerateCardsForTarget(go);
                if (cards != null)
                {
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (cards[i] == null)
                            continue;
                        result.sections.Add(cards[i]);
                        checkpoint?.MarkDirtyFromGoodSection(cards[i], solver);
                    }
                    if (solver != null && cards.Count > 0)
                        solver.AddCards(cards);
                    result.sectionsEnabled += cards.Count;
                }
            }
        }

        checkpoint?.MarkDirtyFromPhysicsTranslation(go);
        _ = ragdoll;
    }

    public static void CollectCascadeFromMoved(
        GameObject moved,
        IList<GameObject> candidates,
        InteractedObjectCheckpoint checkpoint)
    {
        if (moved == null || candidates == null || checkpoint == null)
            return;
        var mc = moved.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < candidates.Count; i++)
        {
            var other = candidates[i];
            if (other == null || other == moved)
                continue;
            var oc = other.GetComponentsInChildren<Collider>(true);
            bool hit = false;
            if (mc != null && oc != null)
            {
                for (int a = 0; a < mc.Length && !hit; a++)
                {
                    if (mc[a] == null) continue;
                    for (int b = 0; b < oc.Length; b++)
                    {
                        if (oc[b] != null && mc[a].bounds.Intersects(oc[b].bounds))
                        {
                            hit = true;
                            break;
                        }
                    }
                }
            }
            if (hit)
                checkpoint.RememberFirstSeen(other);
        }
    }
}
