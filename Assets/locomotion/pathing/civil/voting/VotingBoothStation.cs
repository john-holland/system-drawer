using System.Collections.Generic;
using UnityEngine;

/// <summary>How approach queues sit on a voting booth station.</summary>
public enum VotingBoothQueueLayout
{
    Single = 0,
    TwoSectionBackToBack = 1,
    FourSectionDivided = 2
}

/// <summary>Booth station: one occupant plus a list of section queues (single, back-to-back, 4-way).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Voting Booth Station")]
public sealed class VotingBoothStation : MonoBehaviour
{
    public VotingBoothQueueLayout layout = VotingBoothQueueLayout.Single;
    public List<LaneGrid> sections = new List<LaneGrid>();
    public BaseAmbulatingActor occupant;
    public GameObject ballotUiHost;
    public VotingQueueHub hub;

    public static int SectionCountFor(VotingBoothQueueLayout layout)
    {
        if (layout == VotingBoothQueueLayout.FourSectionDivided) return 4;
        if (layout == VotingBoothQueueLayout.TwoSectionBackToBack) return 2;
        return 1;
    }

    public void EnsureSections()
    {
        if (sections == null) sections = new List<LaneGrid>();
        if (sections.Count == 0)
            CollectPaintedSections();
        int need = SectionCountFor(layout);
        for (int i = sections.Count; i < need; i++)
        {
            var child = new GameObject("booth_q_" + i);
            child.transform.SetParent(transform, false);
            PlaceSection(child.transform, i, need);
            var grid = child.AddComponent<LaneGrid>();
            grid.width = 1;
            grid.height = 4;
            grid.EnsureCells();
            sections.Add(grid);
        }
        if (sections.Count > need)
            sections.RemoveRange(need, sections.Count - need);
        for (int i = 0; i < sections.Count; i++)
            if (sections[i] != null)
                sections[i].EnsureCells();
        if (GetComponent<StationHierarchyNode>() == null)
        {
            var node = gameObject.AddComponent<StationHierarchyNode>();
            node.kind = StationKind.VotingBooth;
            node.displayName = gameObject.name;
            if (node.config == null) node.config = new StationConfig();
            node.config.votingBooth = this;
        }
    }

    public LaneGrid ShortestSection()
    {
        EnsureSections();
        LaneGrid best = null;
        int min = int.MaxValue;
        for (int i = 0; i < sections.Count; i++)
        {
            var g = sections[i];
            if (g == null) continue;
            int n = g.queue != null ? g.queue.Count : 0;
            if (n < min)
            {
                min = n;
                best = g;
            }
        }
        return best;
    }

    public bool TryAccept(BaseAmbulatingActor actor)
    {
        var sec = ShortestSection();
        return sec != null && sec.TryEnqueue(actor);
    }

    public bool TryOccupyHead()
    {
        if (occupant != null) return false;
        EnsureSections();
        for (int i = 0; i < sections.Count; i++)
        {
            var g = sections[i];
            if (g == null || g.Peek() == null) continue;
            occupant = g.TryDequeueToBooth();
            return occupant != null;
        }
        return false;
    }

    public bool Occupies(BaseAmbulatingActor actor) => occupant != null && occupant == actor;

    public void Vacate()
    {
        occupant = null;
    }

    void CollectPaintedSections()
    {
        var found = GetComponentsInChildren<LaneGrid>(true);
        for (int i = 0; i < found.Length; i++)
        {
            var g = found[i];
            if (g == null || sections.Contains(g)) continue;
            sections.Add(g);
        }
    }

    static void PlaceSection(Transform t, int index, int count)
    {
        if (count <= 1)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            return;
        }
        if (count == 2)
        {
            t.localPosition = new Vector3(0f, 0f, index == 0 ? 1.5f : -1.5f);
            t.localRotation = Quaternion.Euler(0f, index == 0 ? 0f : 180f, 0f);
            return;
        }
        float[] xs = { 1.5f, 1.5f, -1.5f, -1.5f };
        float[] zs = { 1.5f, -1.5f, -1.5f, 1.5f };
        float[] ys = { 0f, 90f, 180f, 270f };
        int i = Mathf.Clamp(index, 0, 3);
        t.localPosition = new Vector3(xs[i], 0f, zs[i]);
        t.localRotation = Quaternion.Euler(0f, ys[i], 0f);
    }
}
