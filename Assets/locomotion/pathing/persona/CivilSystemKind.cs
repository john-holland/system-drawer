/// <summary>Civil venue kinds managed by PersonaDayManager lattice.</summary>
public enum CivilSystemKind
{
    Generic = 0,
    Kitchen = 1,
    School = 2,
    Mall = 3,
    Library = 4,
    Church = 5
}

/// <summary>Per-venue simulation fidelity under budget + speed LOD.</summary>
public enum CivilLodTier
{
    FullSim = 0,
    Proxy = 1,
    Ghost = 2,
    Culled = 3
}
