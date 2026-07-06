using System.Collections.Generic;

public sealed class FeatureBudgetAttributor
{
    static readonly List<PerfTraceNode> Scratch = new List<PerfTraceNode>();

    public void AttributeFrame(FeatureBudgetProfile profile, Dictionary<string, float> msByFeature)
    {
        msByFeature.Clear();
        if (profile?.entries == null)
            return;

        Scratch.Clear();
        PerfTrace.CopyRoughNodes(Scratch);
        if (Scratch.Count == 0)
            return;

        double tickToMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        for (int n = 0; n < Scratch.Count; n++)
        {
            var node = Scratch[n];
            if (node == null)
                continue;
            string label = node.Label ?? node.Note ?? "";
            if (string.IsNullOrEmpty(label))
                continue;
            float ms = (float)(node.TotalTicks * tickToMs);
            for (int e = 0; e < profile.entries.Count; e++)
            {
                var entry = profile.entries[e];
                if (entry.perfScopePrefixes == null)
                    continue;
                for (int p = 0; p < entry.perfScopePrefixes.Length; p++)
                {
                    string prefix = entry.perfScopePrefixes[p];
                    if (string.IsNullOrEmpty(prefix))
                        continue;
                    if (label.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
                        || label.Contains(prefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!msByFeature.ContainsKey(entry.featureId))
                            msByFeature[entry.featureId] = 0f;
                        msByFeature[entry.featureId] += ms;
                        break;
                    }
                }
            }
        }
    }
}
