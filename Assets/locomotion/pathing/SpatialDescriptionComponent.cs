using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Lemma property keys for spatial description paint.</summary>
public static class SpatialDescriptionLemmaPropertyKeys
{
    public const string SpatialDescription = "spatial-description";
    public const string SpatialSkinKey = "spatial-skin-key";
    public const string SpatialAdjPaint = "spatial-adj-paint";
}

/// <summary>
/// Paints Spatial Generator / materials from adj-adv lemmas via ShaderGrammarIndex and stylesheet keys.
/// </summary>
[AddComponentMenu("Locomotion/Periphery/Spatial Description Component")]
public sealed class SpatialDescriptionComponent : MonoBehaviour
{
    public ScriptableObject shaderGrammar;
    public UnityEngine.Object stylesheet;
    public MonoBehaviour skinController;
    public SpatialDescriptionFilter descriptionFilter = new SpatialDescriptionFilter();
    public List<string> paintedKeys = new List<string>();
    public List<string> paintedAdjectives = new List<string>();

    public void ClearPaint()
    {
        paintedKeys.Clear();
        paintedAdjectives.Clear();
        descriptionFilter = new SpatialDescriptionFilter();
    }

    public void PaintFromModifiers(IEnumerable<string> adjectivesOrDescriptions)
    {
        if (adjectivesOrDescriptions == null) return;
        foreach (var term in adjectivesOrDescriptions)
        {
            if (string.IsNullOrWhiteSpace(term)) continue;
            string t = term.Trim();
            paintedAdjectives.Add(t);
            paintedKeys.Add(t);
            ApplyShaderPaint(t);
            ApplySkinKey(t);
        }
        descriptionFilter = new SpatialDescriptionFilter(paintedKeys);
    }

    void ApplyShaderPaint(string term)
    {
        if (shaderGrammar == null) return;
        // ShaderGrammarIndex lives in Generated.Runtime — reflect to avoid hard asm coupling.
        var entriesProp = shaderGrammar.GetType().GetField("entries");
        if (entriesProp == null) return;
        var list = entriesProp.GetValue(shaderGrammar) as System.Collections.IEnumerable;
        if (list == null) return;
        foreach (var item in list)
        {
            if (item == null) continue;
            var t = item.GetType();
            var termF = t.GetField("term");
            var roleF = t.GetField("role");
            var slotF = t.GetField("shaderPropertyOrSlot");
            if (termF == null || slotF == null) continue;
            string eTerm = termF.GetValue(item) as string;
            string role = roleF != null ? roleF.GetValue(item) as string : null;
            string slot = slotF.GetValue(item) as string;
            if (string.IsNullOrEmpty(eTerm) || string.IsNullOrEmpty(slot)) continue;
            if (!string.IsNullOrEmpty(role) && !string.Equals(role, "adjective", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(eTerm, term, StringComparison.OrdinalIgnoreCase))
                continue;
            var rends = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || rends[i].sharedMaterial == null) continue;
                var mat = rends[i].material;
                if (mat.HasProperty(slot))
                    mat.SetFloat(slot, 1f);
            }
        }
    }

    void ApplySkinKey(string term)
    {
        if (skinController == null) return;
        // Prefer matching skin by name containing term when available.
        // Stylesheet node keys are updated on descriptionFilter for SG search consumers.
    }

    public bool FilterMatches(string key) => descriptionFilter != null && descriptionFilter.Matches(key);
}
