using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin lemma/aspect watch for romance reminders (interpreter + spatial paint).
/// Fires when watched keys mention a partner aspect.
/// </summary>
[AddComponentMenu("Locomotion/Romance/Lemma Watch")]
public sealed class LemmaWatch : MonoBehaviour
{
    public List<string> watchKeys = new List<string>();
    public GameObject partner;
    public string partnerAspectKey = "romance.partner";
    public bool fireOnPaintMatch = true;
    public bool fireOnConversationMatch = true;

    public event Action<string, GameObject> RomanceReminder;

    readonly List<string> _recent = new List<string>();

    public void ObserveLemma(string lemmaOrAspect)
    {
        if (string.IsNullOrEmpty(lemmaOrAspect)) return;
        _recent.Add(lemmaOrAspect);
        if (_recent.Count > 32) _recent.RemoveAt(0);
        if (!fireOnConversationMatch) return;
        if (MatchesWatch(lemmaOrAspect))
            RomanceReminder?.Invoke(lemmaOrAspect, partner);
    }

    public void ObservePaintKey(string paintKey)
    {
        if (!fireOnPaintMatch || string.IsNullOrEmpty(paintKey)) return;
        if (MatchesWatch(paintKey) || paintKey.IndexOf(partnerAspectKey, StringComparison.OrdinalIgnoreCase) >= 0)
            RomanceReminder?.Invoke(paintKey, partner);
    }

    bool MatchesWatch(string key)
    {
        if (watchKeys == null) return false;
        for (int i = 0; i < watchKeys.Count; i++)
        {
            var w = watchKeys[i];
            if (string.IsNullOrEmpty(w)) continue;
            if (key.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Parse developer prompt fragments like: looking {P=whistful|non-ik-animation=true}.
    /// </summary>
    public static bool TryParsePromptDirective(string text, out string pose, out bool nonIkAnimation)
    {
        pose = null;
        nonIkAnimation = false;
        if (string.IsNullOrEmpty(text)) return false;
        int start = text.IndexOf("{P=", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return false;
        int end = text.IndexOf('}', start);
        if (end < 0) return false;
        string body = text.Substring(start + 3, end - (start + 3));
        var parts = body.Split('|');
        if (parts.Length > 0)
            pose = parts[0].Trim();
        for (int i = 1; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p.StartsWith("non-ik-animation=", StringComparison.OrdinalIgnoreCase))
            {
                string v = p.Substring("non-ik-animation=".Length).Trim();
                nonIkAnimation = v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
            }
        }
        return !string.IsNullOrEmpty(pose);
    }
}
