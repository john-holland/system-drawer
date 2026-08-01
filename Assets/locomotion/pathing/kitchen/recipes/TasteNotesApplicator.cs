using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Applies taste notes to LifeSystems chemical mood and builds dialog suggestions.
/// sour→blood pressure; spicy→endorphin (+ adrenaline tint).
/// </summary>
public static class TasteNotesApplicator
{
    public sealed class ApplyResult
    {
        public readonly List<string> dialogSuggestions = new List<string>();
        public readonly List<string> appliedChannels = new List<string>();
    }

    public static ApplyResult Apply(LifeSystemsSheet sheet, IList<TasteNoteEntry> notes, float intensityScale = 1f)
    {
        var result = new ApplyResult();
        if (notes == null || notes.Count == 0) return result;
        float scale = Mathf.Clamp01(intensityScale);
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i] == null) continue;
            float amp = Mathf.Clamp01(notes[i].intensity01) * scale * 0.12f;
            string tag = ResolveTag(notes[i]);
            ApplyTag(sheet, tag, amp, result);
            result.dialogSuggestions.Add(TasteDialogHints.LineFor(tag));
        }
        return result;
    }

    public static ApplyResult ApplyFromCsv(LifeSystemsSheet sheet, string notesCsv, float intensity01 = 0.5f)
    {
        var entries = ParseCsv(notesCsv, intensity01);
        return Apply(sheet, entries, 1f);
    }

    public static List<TasteNoteEntry> ParseCsv(string notesCsv, float intensity01 = 0.5f)
    {
        var list = new List<TasteNoteEntry>();
        if (string.IsNullOrEmpty(notesCsv)) return list;
        var parts = notesCsv.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string t = parts[i].Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(t)) continue;
            var e = new TasteNoteEntry { intensity01 = intensity01, customTag = t };
            if (Enum.TryParse(typeof(TasteNoteId), Capitalize(t), true, out var box) && box is TasteNoteId id)
            {
                e.note = id;
                e.customTag = null;
            }
            list.Add(e);
        }
        return list;
    }

    public static string BuildLemmaToken(IList<TasteNoteEntry> notes, float intensity01 = 0.5f)
    {
        var sb = new StringBuilder();
        sb.Append("{P:taste|notes=");
        if (notes != null)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ResolveTag(notes[i]));
            }
        }
        sb.Append("|intensity=").Append(intensity01.ToString("0.##")).Append('}');
        return sb.ToString();
    }

    static string ResolveTag(TasteNoteEntry e)
    {
        if (e == null) return "umami";
        if (!string.IsNullOrEmpty(e.customTag)) return e.customTag.ToLowerInvariant();
        return e.note.ToString().ToLowerInvariant();
    }

    static void ApplyTag(LifeSystemsSheet sheet, string tag, float amp, ApplyResult result)
    {
        if (sheet == null || amp <= 0f) return;
        sheet.EnsureDefaults();
        switch (tag)
        {
            case "sour":
                sheet.Adjust01(LifeSystemsChannelCatalog.BloodPressureSys, amp);
                sheet.Adjust01(LifeSystemsChannelCatalog.HypertensiveLoad, amp * 0.5f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.BloodPressureSys);
                break;
            case "spicy":
                sheet.Adjust01(LifeSystemsChannelCatalog.Endorphin, amp);
                sheet.Adjust01(LifeSystemsChannelCatalog.Adrenaline, amp * 0.35f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.Endorphin);
                break;
            case "sweet":
                sheet.Adjust01(LifeSystemsChannelCatalog.BloodSugar, amp);
                sheet.Adjust01(LifeSystemsChannelCatalog.Morale, amp * 0.5f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.BloodSugar);
                break;
            case "bitter":
                sheet.Adjust01(LifeSystemsChannelCatalog.Attention, amp);
                sheet.Adjust01(LifeSystemsChannelCatalog.ClearThought, amp * 0.5f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.Attention);
                break;
            case "umami":
                sheet.Adjust01(LifeSystemsChannelCatalog.Morale, amp);
                sheet.Adjust01(LifeSystemsChannelCatalog.Lipids, amp * 0.25f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.Morale);
                break;
            case "salty":
                sheet.Adjust01(LifeSystemsChannelCatalog.Hydration, -amp * 0.25f);
                sheet.Adjust01(LifeSystemsChannelCatalog.BloodPressureSys, amp * 0.35f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.Hydration);
                break;
            default:
                sheet.Adjust01(LifeSystemsChannelCatalog.Morale, amp * 0.25f);
                result.appliedChannels.Add(LifeSystemsChannelCatalog.Morale);
                break;
        }
    }

    static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}

/// <summary>Short dialog lines from taste notes for SendThought / dialog BT.</summary>
public static class TasteDialogHints
{
    public static string LineFor(string tag)
    {
        switch ((tag ?? "").ToLowerInvariant())
        {
            case "sour": return "That bite’s sharp — cheeks tighten.";
            case "spicy": return "Heat hits — endorphins wake up.";
            case "sweet": return "Sugar softens the edge.";
            case "bitter": return "Bitter clears the head.";
            case "umami": return "Deep savor settles in.";
            case "salty": return "Salt pulls thirst forward.";
            default: return $"Taste note: {tag}";
        }
    }

    public static void SeedSendThought(GameObject actor, IList<string> suggestions)
    {
        if (actor == null || suggestions == null || suggestions.Count == 0) return;
        actor.SendMessage("BeginFromSpanRef", "taste_notes", SendMessageOptions.DontRequireReceiver);
        actor.SendMessage("OnTasteDialogHints", string.Join(" | ", suggestions), SendMessageOptions.DontRequireReceiver);
    }
}
