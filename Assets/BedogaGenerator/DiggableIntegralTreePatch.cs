using UnityEngine;

/// <summary>
/// Applies a diggable clear or fill to the spatial generator whose bounds contain the integral box.
/// Two-dimensional generators patch the quadtree. Three-dimensional generators patch the octree.
/// The 4D generator patches the time slice that contains the narrative clock.
/// </summary>
public static class DiggableIntegralTreePatch
{
    public static int patchCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        DiggableVolume.IntegralCommitted -= OnCommitted;
        DiggableVolume.IntegralCommitted += OnCommitted;
    }

    public static void OnCommitted(DiggableIntegralReport report)
    {
        if (report.volume == null)
            return;
        int removed = 0;
        var generators = Object.FindObjectsByType<SpatialGenerator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < generators.Length; i++)
            removed += PatchGenerator(generators[i], report);
        var generators4 = Object.FindObjectsByType<SpatialGenerator4D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < generators4.Length; i++)
            removed += PatchGenerator4D(generators4[i], report);
        patchCount += removed;
    }

    public static int PatchGenerator(SpatialGenerator generator, DiggableIntegralReport report)
    {
        if (generator == null)
            return 0;
        var bounds = new Bounds(generator.transform.position, generator.generationSize);
        if (!bounds.Intersects(report.box))
            return 0;
        int removed = 0;
        if (generator.mode == SpatialGenerator.GenerationMode.TwoDimensional)
        {
            SGQuadTreeSolver quad = generator.GetComponent<SGQuadTreeSolver>();
            if (quad != null && quad.GetQuadTree() != null)
                removed += quad.GetQuadTree().RemoveInside(report.box);
        }
        else
        {
            SGOctTreeSolver oct = generator.GetComponent<SGOctTreeSolver>();
            if (oct != null && oct.GetOctTree() != null)
                removed += oct.GetOctTree().RemoveInside(report.box);
        }
        Reinsert(generator.GetComponent<SGOctTreeSolver>(), generator.GetComponent<SGQuadTreeSolver>(), report);
        return removed;
    }

    public static int PatchGenerator4D(SpatialGenerator4D generator, DiggableIntegralReport report)
    {
        if (generator == null)
            return 0;
        SGOctTree slice = generator.GetSliceTree(0f);
        if (slice == null)
            return 0;
        int removed = slice.RemoveInside(report.box);
        if (report.volume != null)
            slice.Insert(report.volume.WorldBounds, report.volume.gameObject, report);
        return removed;
    }

    static void Reinsert(SGOctTreeSolver oct, SGQuadTreeSolver quad, DiggableIntegralReport report)
    {
        if (report.volume == null)
            return;
        Bounds solid = report.volume.WorldBounds;
        if (oct != null && oct.GetOctTree() != null)
            oct.GetOctTree().Insert(solid, report.volume.gameObject, report);
        if (quad != null && quad.GetQuadTree() != null)
            quad.GetQuadTree().Insert(solid, report.volume.gameObject, report);
    }
}
