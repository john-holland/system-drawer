/// <summary>How authored loops partition a skinned mesh. CutSeam is the default (B-heavy).</summary>
public enum SkinnedMeshLoopSplitMode
{
    CutSeam = 0,
    FloodInterior = 1,
    NamedAssign = 2
}
