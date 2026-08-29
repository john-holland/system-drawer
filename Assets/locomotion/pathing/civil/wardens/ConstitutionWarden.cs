using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Scores bills against constitution articles. Junta suspend / kangaroo court report ~0 allow.
/// Events announce limit/removal/return of a right.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Constitution Warden")]
public sealed class ConstitutionWarden : MonoBehaviour
{
    public const string RightLimitedEvent = "constitution-right-limited";
    public const string RightRemovedEvent = "constitution-right-removed";
    public const string RightsReturnedEvent = "constitution-rights-returned";

    [Range(0f, 1f)] public float lastScore01 = 1f;
    public bool articlesEnabled = true;
    public JuntaRuntime junta;
    public CourtKind courtKind = CourtKind.American;
    public ConstitutionAsset constitution;
    public LawCard pendingBill;
    public NarrativeCalendarAsset calendar;
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();

    public float Allow01()
    {
        Evaluate();
        return lastScore01;
    }

    public float Evaluate()
    {
        if (Suspended())
        {
            lastScore01 = 0f;
            return 0f;
        }
        float score = articlesEnabled ? 1f : 0f;
        if (constitution != null && constitution.articles != null)
        {
            int n = 0, on = 0;
            for (int i = 0; i < constitution.articles.Count; i++)
            {
                var a = constitution.articles[i];
                if (a == null) continue;
                n++;
                if (a.enabled) on++;
            }
            if (n > 0) score = on / (float)n;
        }
        lastScore01 = Mathf.Clamp01(score);
        return lastScore01;
    }

    public bool Suspended()
    {
        if (!articlesEnabled) return true;
        if (courtKind == CourtKind.Kangaroo) return true;
        var j = junta != null ? junta : GetComponent<JuntaRuntime>();
        return j != null && j.canSuspendConstitution;
    }

    public NarrativeCalendarEvent AnnounceRightLimited(string articleId)
    {
        return Enqueue(RightLimitedEvent, "Right limited", articleId);
    }

    public NarrativeCalendarEvent AnnounceRightRemoved(string articleId)
    {
        var article = constitution != null ? constitution.FindArticle(articleId) : null;
        if (article != null)
            article.enabled = false;
        return Enqueue(RightRemovedEvent, "Right removed", articleId);
    }

    public NarrativeCalendarEvent AnnounceRightsReturned(string articleId = null)
    {
        RestoreRights(articleId);
        var evt = Enqueue(RightsReturnedEvent, "Rights returned", articleId);
        if (evt?.tags != null)
        {
            evt.tags.Add(LegalLemmaPropertyKeys.AnnounceRightsReturned);
            evt.tags.Add(LegalLemmaPropertyKeys.RightsReturned);
        }
        return evt;
    }

    void RestoreRights(string articleId)
    {
        articlesEnabled = true;
        var j = junta != null ? junta : GetComponent<JuntaRuntime>();
        if (j != null)
            j.canSuspendConstitution = false;
        if (constitution == null || constitution.articles == null)
            return;
        if (!string.IsNullOrEmpty(articleId))
        {
            var article = constitution.FindArticle(articleId);
            if (article != null)
                article.enabled = true;
            return;
        }
        for (int i = 0; i < constitution.articles.Count; i++)
            if (constitution.articles[i] != null)
                constitution.articles[i].enabled = true;
    }

    NarrativeCalendarEvent Enqueue(string id, string title, string articleId)
    {
        if (calendar == null)
            calendar = GetComponent<NarrativeCalendarAsset>();
        if (calendar == null) return null;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        var evt = new NarrativeCalendarEvent
        {
            id = id + "-" + (articleId ?? ""),
            title = title,
            notes = articleId ?? "",
            tags = new List<string> { id, "constitution" }
        };
        calendar.events.Add(evt);
        return evt;
    }
}
