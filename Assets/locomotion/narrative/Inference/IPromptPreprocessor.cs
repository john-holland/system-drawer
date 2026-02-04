using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Preprocesses a prompt asset: reads originalText, produces procedural text (e.g. via LLM), writes to asset.proceduralText.
    /// </summary>
    public interface IPromptPreprocessor
    {
        /// <summary>Preprocess asset: read original, optionally call LLM with allowed vocabulary from registry, write procedural to asset.</summary>
        /// <param name="asset">Prompt asset; originalText is read, proceduralText is written.</param>
        /// <param name="sceneObjectRegistry">Optional; used to build allowed vocabulary for LLM.</param>
        /// <returns>True if procedural text was written.</returns>
        bool Preprocess(NarrativePromptAsset asset, SceneObjectRegistry sceneObjectRegistry);
    }
}
