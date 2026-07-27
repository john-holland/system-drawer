using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Resolves {P:action|maps-to=...} / keymap lemmas onto ActionInputMapRegistry.</summary>
public static class ActionInputLemmaResolver
{
    public static bool IsActionInputLemma(string placeholderName)
    {
        if (string.IsNullOrEmpty(placeholderName)) return false;
        string n = placeholderName.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
        for (int i = 0; i < ActionInputLemmaPropertyKeys.LemmaPlaceholders.Length; i++)
        {
            if (n == ActionInputLemmaPropertyKeys.LemmaPlaceholders[i])
                return true;
        }
        return false;
    }

    public static ActionInputLemmaProperties Resolve(
        Dictionary<string, string> parameters,
        string placeholderName = "action") =>
        ActionInputLemmaProperties.ResolveFromParams(parameters, placeholderName);

    public static void Paint(
        ActionInputMapRegistry registry,
        Dictionary<string, string> parameters,
        string placeholderName = "action")
    {
        if (registry == null) return;
        var props = Resolve(parameters, placeholderName);
        registry.ApplyLemma(props);
    }

    /// <summary>Parse all action/keymap/maps spans in a prompt and apply to the registry (created if missing).</summary>
    public static int ApplyFromPrompt(string prompt, ActionInputMapRegistry registry = null)
    {
        if (string.IsNullOrEmpty(prompt)) return 0;
        registry = registry != null ? registry : ActionInputMapRegistry.FindOrCreate();
        var segments = PromptSpanParser.Parse(prompt);
        int applied = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            if (!IsActionInputLemma(seg.placeholderName)) continue;
            Paint(registry, seg.placeholderParams, seg.placeholderName);
            applied++;
        }
        return applied;
    }
}

/// <summary>Applies a serialized lemmaPrompt of action keymap spans on enable / demand.</summary>
[AddComponentMenu("Locomotion/Input/Action Input Lemma Applier")]
public sealed class ActionInputLemmaApplier : MonoBehaviour
{
    [TextArea(2, 8)]
    public string lemmaPrompt =
        "{P:action|id=jump|maps-to=Space}\n{P:action|id=fire|subscribe=KEY_UP|maps-to=MOUSE_0}";

    public ActionInputMapRegistry registry;
    public bool applyOnEnable = true;

    void OnEnable()
    {
        if (applyOnEnable)
            Apply();
    }

    [ContextMenu("Apply Lemma Prompt")]
    public void Apply()
    {
        if (registry == null)
            registry = GetComponent<ActionInputMapRegistry>()
                       ?? ActionInputMapRegistry.FindOrCreate();
        ActionInputLemmaResolver.ApplyFromPrompt(lemmaPrompt, registry);
    }
}
