using System;
using System.Collections.Generic;
using Locomotion.Narrative;

namespace Locomotion.Drink
{
    /// <summary>Resolves drink lemma properties from prompt spans, clause bindings, and entry property bags.</summary>
    public static class DrinkLemmaPropertyResolver
    {
        public static DrinkLemmaProperties Resolve(
            IReadOnlyList<PromptSegment> promptSegments = null,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings = null,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties = null,
            int charStart = -1,
            int charEnd = -1)
        {
            var d = DrinkLemmaProperties.Defaults;
            d.drinkAnimationRef = ResolveString(
                DrinkLemmaPropertyKeys.DrinkAnimationRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.autoMiddleMouthJaw = ResolveBool(
                DrinkLemmaPropertyKeys.AutoMiddleMouthJaw, promptSegments, clauseBindings, lemmaProperties, "true", charStart, charEnd);
            d.jawTiltAnimationAuditInsert = ResolveBool(
                DrinkLemmaPropertyKeys.JawTiltAnimationAuditInsert, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.holdWithoutReturn = ResolveBool(
                DrinkLemmaPropertyKeys.HoldWithoutReturn, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.putWithoutRelease = ResolveBool(
                DrinkLemmaPropertyKeys.PutWithoutRelease, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.nozzleLoopEnabled = ResolveBool(
                DrinkLemmaPropertyKeys.NozzleLoopEnabled, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.liquidSimulationEnabled = ResolveBool(
                DrinkLemmaPropertyKeys.LiquidSimulationEnabled, promptSegments, clauseBindings, lemmaProperties, "true", charStart, charEnd);
            d.placeNozzleOnMouth = ResolveBool(
                DrinkLemmaPropertyKeys.PlaceNozzleOnMouth, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.drinkEfficacy = ResolveFloat(
                DrinkLemmaPropertyKeys.DrinkEfficacy, promptSegments, clauseBindings, lemmaProperties,
                DrinkLemmaPropertyKeys.DefaultDrinkEfficacy.ToString(), charStart, charEnd);
            d.drinkEfficacy = UnityEngine.Mathf.Clamp01(d.drinkEfficacy);
            d.sipCount = ResolveInt(
                DrinkLemmaPropertyKeys.SipCount, promptSegments, clauseBindings, lemmaProperties,
                DrinkLemmaPropertyKeys.DefaultSipCount.ToString(), charStart, charEnd);
            d.sipCount = UnityEngine.Mathf.Max(1, d.sipCount);
            d.totalVolumeLiters = ResolveFloat(
                DrinkLemmaPropertyKeys.TotalVolumeLiters, promptSegments, clauseBindings, lemmaProperties, "0", charStart, charEnd);
            d.totalVolumeLiters = UnityEngine.Mathf.Max(0f, d.totalVolumeLiters);
            d.partiallyRaiseAmount = UnityEngine.Mathf.Clamp01(ResolveFloat(
                DrinkLemmaPropertyKeys.PartiallyRaiseAmount, promptSegments, clauseBindings, lemmaProperties, "1", charStart, charEnd));
            d.partialRaiseDefaultWhenStalled = UnityEngine.Mathf.Clamp01(ResolveFloat(
                DrinkLemmaPropertyKeys.PartialRaiseDefaultWhenStalled, promptSegments, clauseBindings, lemmaProperties,
                DrinkLemmaPropertyKeys.DefaultPartialRaiseWhenStalled.ToString(), charStart, charEnd));
            d.trainForPerfectDrink = ResolveBool(
                DrinkLemmaPropertyKeys.TrainForPerfectDrink, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.maxSpillLitersTolerance = UnityEngine.Mathf.Max(0f, ResolveFloat(
                DrinkLemmaPropertyKeys.MaxSpillLitersTolerance, promptSegments, clauseBindings, lemmaProperties, "0.05", charStart, charEnd));
            d.closureMode = ParseClosureMode(ResolveString(
                DrinkLemmaPropertyKeys.ClosureMode, promptSegments, clauseBindings, lemmaProperties, "auto", charStart, charEnd));
            d.mouthVolumeLitersTarget = UnityEngine.Mathf.Max(0f, ResolveFloat(
                DrinkLemmaPropertyKeys.MouthVolumeLitersTarget, promptSegments, clauseBindings, lemmaProperties, "0", charStart, charEnd));
            d.infiniteDrain = ResolveBool(
                DrinkLemmaPropertyKeys.InfiniteDrain, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.infiniteDrainClosureSeconds = UnityEngine.Mathf.Max(0f, ResolveFloat(
                DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds, promptSegments, clauseBindings, lemmaProperties, "0", charStart, charEnd));
            return d;
        }

        static DrinkClosureMode ParseClosureMode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DrinkClosureMode.Auto;
            raw = raw.Trim().ToLowerInvariant().Replace("_", "-");
            return raw switch
            {
                "mouth" => DrinkClosureMode.Mouth,
                "empty-vessel" => DrinkClosureMode.EmptyVessel,
                "stalled" => DrinkClosureMode.Stalled,
                "spill-beat" => DrinkClosureMode.SpillBeat,
                "infinite-drain-beat" => DrinkClosureMode.InfiniteDrainBeat,
                _ => DrinkClosureMode.Auto,
            };
        }

        public static bool ResolveBool(
            string propertyKey,
            IReadOnlyList<PromptSegment> promptSegments,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties,
            string specDefault,
            int charStart = -1,
            int charEnd = -1) =>
            AnimationPlaybackPolicyResolver.ResolveEffectiveBool(
                propertyKey, promptSegments, clauseBindings, lemmaProperties, specDefault, charStart, charEnd);

        public static string ResolveString(
            string propertyKey,
            IReadOnlyList<PromptSegment> promptSegments,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties,
            string specDefault,
            int charStart = -1,
            int charEnd = -1)
        {
            if (TryGetFromPrompt(promptSegments, propertyKey, out string fromPrompt))
                return fromPrompt;
            if (TryGetFromClauseBindings(clauseBindings, propertyKey, charStart, charEnd, out string fromClause))
                return fromClause;
            if (TryGetFromLemmaProperties(lemmaProperties, propertyKey, out string fromLemma))
                return fromLemma;
            return specDefault ?? "";
        }

        public static float ResolveFloat(
            string propertyKey,
            IReadOnlyList<PromptSegment> promptSegments,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties,
            string specDefault,
            int charStart = -1,
            int charEnd = -1)
        {
            if (TryGetFromPrompt(promptSegments, propertyKey, out string raw) && TryParseFloat(raw, out float v))
                return v;
            if (TryGetFromClauseBindings(clauseBindings, propertyKey, charStart, charEnd, out raw) && TryParseFloat(raw, out v))
                return v;
            if (TryGetFromLemmaProperties(lemmaProperties, propertyKey, out raw) && TryParseFloat(raw, out v))
                return v;
            return TryParseFloat(specDefault, out float d) ? d : 0f;
        }

        public static int ResolveInt(
            string propertyKey,
            IReadOnlyList<PromptSegment> promptSegments,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties,
            string specDefault,
            int charStart = -1,
            int charEnd = -1)
        {
            if (TryGetFromPrompt(promptSegments, propertyKey, out string raw) && TryParseInt(raw, out int v))
                return v;
            if (TryGetFromClauseBindings(clauseBindings, propertyKey, charStart, charEnd, out raw) && TryParseInt(raw, out v))
                return v;
            if (TryGetFromLemmaProperties(lemmaProperties, propertyKey, out raw) && TryParseInt(raw, out v))
                return v;
            return TryParseInt(specDefault, out int d) ? d : 0;
        }

        static bool TryGetFromPrompt(IReadOnlyList<PromptSegment> segments, string key, out string value)
        {
            value = null;
            if (segments == null || string.IsNullOrEmpty(key))
                return false;
            foreach (var seg in segments)
            {
                if (seg == null || !seg.isPlaceholder || seg.placeholderParams == null)
                    continue;
                if (seg.placeholderParams.TryGetValue(key, out string raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    value = raw.Trim();
                    return true;
                }
            }
            return false;
        }

        static bool TryGetFromClauseBindings(
            IReadOnlyList<LocalizationClauseBindingRecord> bindings,
            string key,
            int charStart,
            int charEnd,
            out string value)
        {
            value = null;
            if (bindings == null)
                return false;
            foreach (var b in bindings)
            {
                if (b == null || !string.Equals(b.propertyKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (charStart >= 0 && charEnd > charStart && (b.charEnd <= charStart || b.charStart >= charEnd))
                    continue;
                if (!string.IsNullOrWhiteSpace(b.propertyValue))
                {
                    value = b.propertyValue.Trim();
                    return true;
                }
            }
            return false;
        }

        static bool TryGetFromLemmaProperties(
            IReadOnlyList<ThesaurusEntryPropertyRecord> properties,
            string key,
            out string value)
        {
            value = null;
            if (properties == null)
                return false;
            foreach (var p in properties)
            {
                if (p != null && string.Equals(p.propertyKey, key, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(p.propertyValue))
                {
                    value = p.propertyValue.Trim();
                    return true;
                }
            }
            return false;
        }

        public static bool TryParseFloat(string raw, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(raw) && float.TryParse(raw.Trim(), out value);
        }

        public static bool TryParseInt(string raw, out int value)
        {
            value = 0;
            return !string.IsNullOrWhiteSpace(raw) && int.TryParse(raw.Trim(), out value);
        }
    }
}
