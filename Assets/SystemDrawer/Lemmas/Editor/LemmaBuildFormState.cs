#if UNITY_EDITOR
using System;
using System.Linq;

/// <summary>Mutable lemma build form state for the Lemma Build tab.</summary>
public sealed class LemmaBuildFormState
{
    public string lemma = "";
    public string posTag = "verb";
    public LemmaMechanicalRole mechanicalRole = LemmaMechanicalRole.ConnectorConjunction;
    public int outputTier;
    public string functionalDescription = "";
    public string mechanismPrompt = "";
    public string synonymsCsv = "";
    public string engine = "unity";
    public LemmaCompositionChildPutDto[] compositionChildren = Array.Empty<LemmaCompositionChildPutDto>();
    public ThesaurusEntryPropertyRecord[] properties = Array.Empty<ThesaurusEntryPropertyRecord>();

    public string ApplyStatusMessage;

    public LemmaBuildFormSnapshot ToSnapshot()
    {
        return new LemmaBuildFormSnapshot
        {
            lemma = lemma ?? "",
            posTag = posTag ?? "",
            mechanicalRole = mechanicalRole.ToString(),
            outputTier = outputTier,
            functionalDescription = functionalDescription ?? "",
            mechanismPrompt = mechanismPrompt ?? "",
            synonyms = ParseSynonyms(),
            compositionChildren = compositionChildren ?? Array.Empty<LemmaCompositionChildPutDto>(),
            properties = properties ?? Array.Empty<ThesaurusEntryPropertyRecord>(),
            engine = engine ?? "unity"
        };
    }

    public void ApplyQueryForm(LemmaBuildDeeplinkForm form)
    {
        if (form == null)
            return;
        if (!string.IsNullOrEmpty(form.lemma))
            lemma = form.lemma;
        var pos = !string.IsNullOrEmpty(form.posTag) ? form.posTag : form.partOfSpeech;
        if (!string.IsNullOrEmpty(pos))
            posTag = pos;
        if (!string.IsNullOrEmpty(form.mechanicalRole) &&
            Enum.TryParse(form.mechanicalRole, true, out LemmaMechanicalRole role))
            mechanicalRole = role;
        outputTier = form.outputTier;
        if (form.functionalDescription != null)
            functionalDescription = form.functionalDescription;
        if (form.mechanismPrompt != null)
            mechanismPrompt = form.mechanismPrompt;
        if (form.synonyms != null && form.synonyms.Length > 0)
            synonymsCsv = string.Join(", ", form.synonyms);
        if (form.compositionChildren != null)
            compositionChildren = form.compositionChildren;
        if (form.properties != null)
            properties = form.properties;
        if (!string.IsNullOrEmpty(form.engine))
            engine = form.engine.Trim().ToLowerInvariant();
        ApplyStatusMessage = "Applied deeplink / query form" +
                             (string.IsNullOrEmpty(engine) ? "" : $" (engine={engine})");
    }

    public void ApplyDescriptor(LemmaMechanismDescriptor descriptor, out string[] unknownEntryWarnings)
    {
        unknownEntryWarnings = Array.Empty<string>();
        if (descriptor == null)
            return;

        if (!string.IsNullOrEmpty(descriptor.lemma))
            lemma = descriptor.lemma;
        if (!string.IsNullOrEmpty(descriptor.posTag))
            posTag = descriptor.posTag;
        if (!string.IsNullOrEmpty(descriptor.mechanicalRole) &&
            Enum.TryParse(descriptor.mechanicalRole, true, out LemmaMechanicalRole role))
            mechanicalRole = role;
        outputTier = descriptor.outputTier;
        if (descriptor.functionalDescription != null)
            functionalDescription = descriptor.functionalDescription;
        if (descriptor.mechanismPrompt != null)
            mechanismPrompt = descriptor.mechanismPrompt;
        if (descriptor.synonyms != null && descriptor.synonyms.Length > 0)
            synonymsCsv = string.Join(", ", descriptor.synonyms);
        if (descriptor.compositionChildren != null)
            compositionChildren = descriptor.compositionChildren;
        if (descriptor.properties != null)
            properties = descriptor.properties;

        unknownEntryWarnings = LemmaBuildBuiltinValidator.ValidateCompositionEntryIds(compositionChildren);
    }

    public string[] ParseSynonyms()
    {
        if (string.IsNullOrWhiteSpace(synonymsCsv))
            return Array.Empty<string>();
        return synonymsCsv
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
    }

    public string ContextChipLabel()
    {
        var tier = $"Tier {outputTier}";
        var role = mechanicalRole.ToString();
        var head = string.IsNullOrWhiteSpace(lemma) ? "(no lemma)" : lemma.Trim();
        var pos = string.IsNullOrWhiteSpace(posTag) ? "?" : posTag.Trim();
        return $"{head} · {pos} · {role} · {tier}";
    }
}
#endif
