using System;
using System.Collections.Generic;

/// <summary>Validates causality leaf prefixes — rejects bisecting snake forks.</summary>
public static class CausalityFamilyAudit
{
    public sealed class AuditResult
    {
        public bool Ok;
        public string Reason = "";
        public readonly List<string> Violations = new List<string>();
    }

    public static AuditResult ValidateTreeRegistry(NetworkTreeRegistry registry)
    {
        var result = new AuditResult { Ok = true };
        if (registry == null)
        {
            result.Ok = false;
            result.Reason = "null registry";
            return result;
        }

        var prefixes = new List<string>();
        foreach (var pair in registry.Trees)
        {
            string p = pair.Value.CausalityLeafPrefix;
            if (string.IsNullOrEmpty(p))
                continue;
            prefixes.Add(p);
        }

        prefixes.Sort(StringComparer.Ordinal);
        for (int i = 0; i < prefixes.Count; i++)
        {
            for (int j = i + 1; j < prefixes.Count; j++)
            {
                if (IsBisectingSnake(prefixes[i], prefixes[j]))
                {
                    result.Ok = false;
                    result.Violations.Add(prefixes[i] + " x " + prefixes[j]);
                }
            }
        }

        if (!result.Ok)
            result.Reason = "bisecting snake detected";
        return result;
    }

    public static bool IsCompatiblePrefix(string parentPrefix, string childPrefix)
    {
        if (string.IsNullOrEmpty(parentPrefix))
            return true;
        if (string.IsNullOrEmpty(childPrefix))
            return false;
        if (childPrefix == parentPrefix)
            return true;
        return childPrefix.StartsWith(parentPrefix + ".", System.StringComparison.Ordinal);
    }

    static bool IsBisectingSnake(string a, string b)
    {
        if (a == b)
            return false;
        if (IsCompatiblePrefix(a, b) || IsCompatiblePrefix(b, a))
            return false;
        int shared = SharedPrefixDotSegments(a, b);
        return shared > 0 && !a.StartsWith(b + ".", System.StringComparison.Ordinal) &&
               !b.StartsWith(a + ".", System.StringComparison.Ordinal);
    }

    static int SharedPrefixDotSegments(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');
        int n = 0;
        for (int i = 0; i < pa.Length && i < pb.Length; i++)
        {
            if (pa[i] != pb[i])
                break;
            n++;
        }
        return n;
    }
}
