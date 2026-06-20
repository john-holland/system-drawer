using System;

/// <summary>Canonical Farey interval identity for a clause: (ln/ld, rn/rd].</summary>
[Serializable]
public sealed class FareySpanRecord
{
    public int ln;
    public int ld;
    public int rn;
    public int rd;

    public static FareySpanRecord Root => new FareySpanRecord { ln = 0, ld = 1, rn = 1, rd = 1 };
}
