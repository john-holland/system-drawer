using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HealthInpaintEventSpec
{
    public string eventId;
    public HealthInpaintKind kind;
    [TextArea(2, 6)] public string promptTemplate =
        "{kind} on {part} after contact with {allergen} near {venue}";
    public string[] triggerRelationKinds;
    public string[] triggerRuleIds;
    public bool usePartSpecific = true;
    public string defaultPartId;
    public bool applyInterpretedToCalendar;
    public bool applyInterpretedToHealthBridge = true;
}

[CreateAssetMenu(fileName = "HealthInpaintEventCatalog", menuName = "Locomotion/Stat/Health Inpaint Event Catalog")]
public sealed class HealthInpaintEventCatalog : ScriptableObject
{
    public List<HealthInpaintEventSpec> events = new List<HealthInpaintEventSpec>();

    public bool TryGet(string eventId, out HealthInpaintEventSpec spec)
    {
        spec = null;
        if (events == null || string.IsNullOrEmpty(eventId)) return false;
        for (int i = 0; i < events.Count; i++)
        {
            if (events[i] != null &&
                string.Equals(events[i].eventId, eventId, StringComparison.OrdinalIgnoreCase))
            {
                spec = events[i];
                return true;
            }
        }
        return false;
    }

    public HealthInpaintEventSpec MatchRelation(TradeRelationKind kind)
    {
        if (events == null) return null;
        string name = kind.ToString();
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (e?.triggerRelationKinds == null) continue;
            for (int j = 0; j < e.triggerRelationKinds.Length; j++)
                if (string.Equals(e.triggerRelationKinds[j], name, StringComparison.OrdinalIgnoreCase))
                    return e;
        }
        return null;
    }

    public HealthInpaintEventSpec MatchRule(string ruleId)
    {
        if (events == null || string.IsNullOrEmpty(ruleId)) return null;
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (e?.triggerRuleIds == null) continue;
            for (int j = 0; j < e.triggerRuleIds.Length; j++)
                if (string.Equals(e.triggerRuleIds[j], ruleId, StringComparison.OrdinalIgnoreCase))
                    return e;
        }
        return null;
    }

    public static HealthInpaintEventCatalog CreateDefaultRuntime()
    {
        var c = CreateInstance<HealthInpaintEventCatalog>();
        c.events = new List<HealthInpaintEventSpec>
        {
            new HealthInpaintEventSpec
            {
                eventId = "hives_from_toad",
                kind = HealthInpaintKind.Hives,
                promptTemplate = "hives on {part} after contact with {allergen} near {venue}",
                triggerRelationKinds = new[] { "AllergySensitize" },
                triggerRuleIds = new[] { "allergy_crosslink" },
                defaultPartId = "LeftForearm"
            },
            new HealthInpaintEventSpec
            {
                eventId = "infected_sports_scrape",
                kind = HealthInpaintKind.Infection,
                promptTemplate = "infected scrape on {part} after extreme sports near {venue}",
                triggerRuleIds = new[] { "pathogen_bloom" },
                defaultPartId = "LeftShin"
            },
            new HealthInpaintEventSpec
            {
                eventId = "glue_residue",
                kind = HealthInpaintKind.AdhesiveResidue,
                promptTemplate = "adhesive residue on {part} from {allergen}",
                triggerRuleIds = new[] { "glue_cure" },
                triggerRelationKinds = new[] { "ChemicalBond" }
            },
            new HealthInpaintEventSpec
            {
                eventId = "tongue_stuck_flagpole",
                kind = HealthInpaintKind.AdhesiveResidue,
                promptTemplate = "tongue stuck to a freezing flag pole; ice bond on {part} near {venue}",
                triggerRuleIds = new[] { "tongue_freeze_bond" },
                triggerRelationKinds = new[] { "ChemicalBond" },
                defaultPartId = "Tongue"
            }
        };
        return c;
    }
}

[AddComponentMenu("Locomotion/Stat/Health Inpaint Event Runner")]
public sealed class HealthInpaintEventRunner : MonoBehaviour
{
    public HealthInpaintEventCatalog catalog;
    /// <summary>Assign NarrativeLSTMPromptInterpreter (Inference asm). Typed as MonoBehaviour to avoid Runtime↔Inference cycle.</summary>
    public MonoBehaviour interpreter;
    public string lastPrompt;
    public string lastEventId;
    public List<string> lastInterpretedTitles = new List<string>();

    public void EnsureDefaults()
    {
        if (catalog == null)
            catalog = HealthInpaintEventCatalog.CreateDefaultRuntime();
        if (interpreter == null)
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b != null && b.GetType().Name == "NarrativeLSTMPromptInterpreter")
                {
                    interpreter = b;
                    break;
                }
            }
        }
    }

    public HealthInpaintRequest Fire(
        string eventId, GameObject actor, string partId = null, string allergen = null,
        string venue = null, float intensity01 = 0.5f)
    {
        EnsureDefaults();
        if (catalog == null || !catalog.TryGet(eventId, out var spec) || spec == null)
            return null;
        return FireSpec(spec, actor, partId, allergen, venue, intensity01);
    }

    public HealthInpaintRequest FireForRule(
        string ruleId, GameObject actor, string partId = null, string allergen = null, string venue = null)
    {
        EnsureDefaults();
        var spec = catalog?.MatchRule(ruleId);
        if (spec == null) return ActorHealthInpaintBridge.RequestFromRule(actor, ruleId, partId, allergen);
        return FireSpec(spec, actor, partId, allergen, venue, 0.55f);
    }

    public HealthInpaintRequest FireForRelation(
        TradeRelationKind kind, GameObject actor, string partId = null, string allergen = null, string venue = null)
    {
        EnsureDefaults();
        var spec = catalog?.MatchRelation(kind);
        if (spec == null)
            return ActorHealthInpaintBridge.RequestFromTrade(actor,
                new TradeEdge { relationKind = kind, allergenOrBondKey = allergen }, partId);
        return FireSpec(spec, actor, partId, allergen, venue, 0.5f);
    }

    HealthInpaintRequest FireSpec(
        HealthInpaintEventSpec spec, GameObject actor, string partId, string allergen, string venue, float intensity01)
    {
        lastEventId = spec.eventId;
        string part = partId;
        if (string.IsNullOrEmpty(part)) part = spec.defaultPartId;
        if (!spec.usePartSpecific) part = null;

        string prompt = (spec.promptTemplate ?? "")
            .Replace("{kind}", spec.kind.ToString())
            .Replace("{part}", string.IsNullOrEmpty(part) ? "body" : part)
            .Replace("{allergen}", string.IsNullOrEmpty(allergen) ? "unknown" : allergen)
            .Replace("{venue}", string.IsNullOrEmpty(venue) ? "here" : venue);
        lastPrompt = prompt;
        lastInterpretedTitles.Clear();

        if (interpreter != null)
        {
            try
            {
                var interpret = interpreter.GetType().GetMethod("Interpret", new[] { typeof(string) });
                var result = interpret?.Invoke(interpreter, new object[] { prompt });
                if (result is System.Collections.IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        var titleProp = item.GetType().GetField("title") ?? (object)item.GetType().GetProperty("title");
                        string title = null;
                        if (titleProp is System.Reflection.FieldInfo fi)
                            title = fi.GetValue(item) as string;
                        else if (titleProp is System.Reflection.PropertyInfo pi)
                            title = pi.GetValue(item) as string;
                        if (!string.IsNullOrEmpty(title))
                            lastInterpretedTitles.Add(title);
                    }
                }
                if (spec.applyInterpretedToCalendar)
                {
                    foreach (var m in interpreter.GetType().GetMethods())
                    {
                        if (m.Name != "ApplyToCalendar") continue;
                        var ps = m.GetParameters();
                        if (ps.Length == 0)
                        {
                            m.Invoke(interpreter, null);
                            break;
                        }
                        if (ps.Length == 1)
                        {
                            m.Invoke(interpreter, new object[] { null });
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HealthInpaintEventRunner] LSTM Interpret skipped: {e.Message}");
            }
        }

        if (!spec.applyInterpretedToHealthBridge) return null;
        string hint = prompt;
        if (lastInterpretedTitles != null && lastInterpretedTitles.Count > 0)
            hint = $"{prompt} | {lastInterpretedTitles[0]}";

        return ActorHealthInpaintBridge.Request(new HealthInpaintRequest
        {
            actor = actor,
            kind = spec.kind,
            partId = part,
            allergenOrBondKey = allergen,
            promptHint = hint,
            intensity01 = intensity01
        });
    }
}
