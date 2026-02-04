using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// One generation request parsed from (GENERATE) or (GENERATE: description) in prompt text.
    /// </summary>
    [Serializable]
    public class GenerationRequest
    {
        public int start;
        public int length;
        public string description;

        public GenerationRequest(int start, int length, string description = null)
        {
            this.start = start;
            this.length = length;
            this.description = description ?? "";
        }
    }

    /// <summary>
    /// Prompt ORM asset: stores original (authored) and procedural (generated) prompt text.
    /// Interpreter always operates on this asset; preprocessing writes procedural, never original.
    /// </summary>
    [CreateAssetMenu(fileName = "NarrativePrompt", menuName = "Locomotion/Narrative/Prompt Asset", order = 0)]
    public class NarrativePromptAsset : ScriptableObject
    {
        [Tooltip("Identity key for lookup (e.g. in PromptRegistry).")]
        public string key = "";

        [Tooltip("Authored prompt text; never overwritten by preprocessing.")]
        [TextArea(3, 8)]
        public string originalText = "";

        [Tooltip("Generated/refactored prompt; written by LLM preprocessing. Interpreter uses this when non-empty, else originalText.")]
        [TextArea(3, 8)]
        public string proceduralText = "";

        [Tooltip("Optional alternate names for registry lookup.")]
        public List<string> synonyms = new List<string>();

        [Header("Metadata (optional)")]
        [Tooltip("Version or name of allowed vocabulary used for last preprocessing.")]
        public string allowedVocabularyVersion = "";
        [Tooltip("Timestamp (UTC ticks) when procedural was last generated. 0 = never.")]
        public long lastPreprocessedAtTicks;

        [Tooltip("(GENERATE) spans parsed from active prompt; populated after interpretation or explicit parse.")]
        public List<GenerationRequest> generationRequests = new List<GenerationRequest>();

        /// <summary>Text to use for interpretation: procedural if non-empty, else original.</summary>
        public string GetActivePromptText()
        {
            return !string.IsNullOrWhiteSpace(proceduralText) ? proceduralText : (originalText ?? "");
        }

        /// <summary>Whether the asset has a generated procedural version.</summary>
        public bool HasProcedural => !string.IsNullOrWhiteSpace(proceduralText);

        /// <summary>Clear procedural text (and optional metadata) so next interpret uses original.</summary>
        public void ClearProcedural()
        {
            proceduralText = "";
            lastPreprocessedAtTicks = 0;
        }
    }
}
