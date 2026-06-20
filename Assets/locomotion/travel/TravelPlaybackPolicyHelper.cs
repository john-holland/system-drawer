using Locomotion.Narrative;
using UnityEngine;

/// <summary>Resolves active phrase and policy context for travel execution.</summary>
public static class TravelPlaybackPolicyHelper
{
    public static string ResolveActivePhrase(TravelAgent agent, int segmentIndex, AnimationPlaybackPolicyContext policy)
    {
        if (agent?.authoringRows != null && agent.authoringRows.Count > 0)
        {
            int nodeIndex = 0;
            foreach (TravelAuthoringRow row in agent.authoringRows)
            {
                if (row == null)
                    continue;
                if (row.kind != TravelAuthoringRowKind.Node)
                    continue;
                if (nodeIndex == segmentIndex)
                {
                    if (row.promptRef != null)
                    {
                        string t = row.promptRef.GetActivePromptText();
                        if (!string.IsNullOrWhiteSpace(t))
                            return ExtractPhraseFromPrompt(t) ?? row.notes ?? "";
                    }
                    if (!string.IsNullOrWhiteSpace(row.notes))
                        return row.notes.Trim();
                }
                nodeIndex++;
            }
        }

        if (policy != null && segmentIndex >= 0)
        {
            foreach (var b in policy.GetPhraseBindingsForEvent(segmentIndex))
            {
                if (!string.IsNullOrWhiteSpace(b.phrase))
                    return b.phrase.Trim();
            }
        }

        return policy?.activePhrase ?? "";
    }

    static string ExtractPhraseFromPrompt(string promptText)
    {
        foreach (PromptSegment seg in PromptSpanParser.Parse(promptText ?? ""))
        {
            if (seg != null && seg.isPlaceholder && !string.IsNullOrEmpty(seg.placeholderName))
                return seg.placeholderName;
        }
        return null;
    }
}
