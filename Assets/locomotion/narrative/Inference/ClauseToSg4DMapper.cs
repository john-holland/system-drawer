using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>One structured clause from LLM (subject, verb, object, role).</summary>
    [System.Serializable]
    public struct RefactoredClause
    {
        public string subject;
        public string verb;
        public string objectPhrase;
        public string role;
    }

    /// <summary>
    /// Maps refactored clauses to SG4D terms: mark start/region (Bounds4), causality (link/token), modifiers (tags), entity keys.
    /// Optional; when LLM returns structured clauses, this can produce events/links for calendar.
    /// </summary>
    public static class ClauseToSg4DMapper
    {
        /// <summary>Map clauses to Bounds4 list (one per clause as placeholder region). Caller can merge with interpreted events.</summary>
        public static void MapToBounds4(IList<RefactoredClause> clauses, Vector3 defaultCenter, float defaultSize, float tStart, float tEnd, List<Bounds4> outVolumes)
        {
            outVolumes?.Clear();
            if (outVolumes == null || clauses == null) return;
            for (int i = 0; i < clauses.Count; i++)
            {
                var c = clauses[i];
                float cx = defaultCenter.x + i * defaultSize * 1.5f;
                var vol = new Bounds4(new Vector3(cx, defaultCenter.y, defaultCenter.z), Vector3.one * defaultSize, tStart, tEnd);
                outVolumes.Add(vol);
            }
        }

        /// <summary>Extract tags/modifiers from clauses (e.g. "unethically" -> tag).</summary>
        public static void CollectModifierTags(IList<RefactoredClause> clauses, List<string> outTags)
        {
            outTags?.Clear();
            if (outTags == null || clauses == null) return;
            foreach (var c in clauses)
            {
                if (!string.IsNullOrWhiteSpace(c.role))
                    outTags.Add(c.role.Trim());
            }
        }
    }
}
