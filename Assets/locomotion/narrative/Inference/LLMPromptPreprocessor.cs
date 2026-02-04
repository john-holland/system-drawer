using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Stub preprocessor: copies original to procedural (no LLM call). Replace or extend with real API call when configured.
    /// </summary>
    public class LLMPromptPreprocessor : MonoBehaviour, IPromptPreprocessor
    {
        [Tooltip("Optional API endpoint for LLM (e.g. OpenAI-compatible). When empty, stub behavior: copy original to procedural.")]
        public string apiEndpoint = "";
        [Tooltip("Optional API key (store in project settings in production).")]
        public string apiKey = "";

        public bool Preprocess(NarrativePromptAsset asset, SceneObjectRegistry sceneObjectRegistry)
        {
            if (asset == null) return false;
            string original = asset.originalText ?? "";
            string procedural;
            if (!string.IsNullOrEmpty(apiEndpoint) && !string.IsNullOrEmpty(apiKey))
            {
                if (!TryCallLLM(original, sceneObjectRegistry, out procedural))
                    procedural = original;
            }
            else
            {
                procedural = original;
            }
            asset.proceduralText = procedural ?? "";
            asset.lastPreprocessedAtTicks = DateTime.UtcNow.Ticks;
            return true;
        }

        private bool TryCallLLM(string original, SceneObjectRegistry sceneObjectRegistry, out string procedural)
        {
            procedural = original;
            return false;
        }
    }
}
