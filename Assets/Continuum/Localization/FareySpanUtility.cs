using System;

/// <summary>Farey interval helpers aligned with Continuum screenplay clause containment.</summary>
public static class FareySpanUtility
{
    /// <summary>True when inner interval (ln/ld, rn/rd] is contained in outer (same open-closed convention).</summary>
    public static bool Contains(FareySpanRecord outer, FareySpanRecord inner)
    {
        if (outer == null || inner == null)
            return false;
        if (outer.ld <= 0 || outer.rd <= 0 || inner.ld <= 0 || inner.rd <= 0)
            return false;

        double oLeft = (double)outer.ln / outer.ld;
        double oRight = (double)outer.rn / outer.rd;
        double iLeft = (double)inner.ln / inner.ld;
        double iRight = (double)inner.rn / inner.rd;
        return iLeft >= oLeft - 1e-12 && iRight <= oRight + 1e-12;
    }

    /// <summary>Parse keys like "0/1-1/1".</summary>
    public static bool TryParse(string fareyKey, out FareySpanRecord span)
    {
        span = null;
        if (string.IsNullOrWhiteSpace(fareyKey))
            return false;

        string[] halves = fareyKey.Split('-');
        if (halves.Length != 2)
            return false;
        if (!TryParseFraction(halves[0], out int ln, out int ld))
            return false;
        if (!TryParseFraction(halves[1], out int rn, out int rd))
            return false;

        span = new FareySpanRecord { ln = ln, ld = ld, rn = rn, rd = rd };
        return true;
    }

    static bool TryParseFraction(string s, out int num, out int den)
    {
        num = 0;
        den = 1;
        if (string.IsNullOrWhiteSpace(s))
            return false;
        int slash = s.IndexOf('/');
        if (slash < 0)
            return false;
        if (!int.TryParse(s.Substring(0, slash).Trim(), out num))
            return false;
        if (!int.TryParse(s.Substring(slash + 1).Trim(), out den))
            return false;
        return den > 0;
    }

    /// <summary>Stub: returns document root until AST char-to-Farey bridge is wired.</summary>
    public static FareySpanRecord CharRangeToFareySpan(string scriptText, int charStart, int charEnd)
    {
        return FareySpanRecord.Root;
    }
}
