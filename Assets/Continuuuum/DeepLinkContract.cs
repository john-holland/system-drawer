using System;

/// <summary>
/// Shared deeplink parse/route contract used by Editor <c>DeepLinkHandler</c> and Edit Mode tests.
/// Web Lemma Build posts <c>window: "System Drawer/Lemmas/Lemma Build"</c> plus a <c>form</c> object.
/// </summary>
public static class DeepLinkContract
{
    public const string LemmaBuildWindow = "System Drawer/Lemmas/Lemma Build";
    public const string LemmaPropertiesWindow = "Continuuuum/Lemma Properties";

    public enum Target
    {
        None,
        Explorer,
        Episodes,
        LemmaBuild,
        LemmaProperties
    }

    public static Target ResolveTarget(string window, string episodeId)
    {
        if (string.IsNullOrEmpty(window) && string.IsNullOrEmpty(episodeId))
            return Target.None;

        if ((!string.IsNullOrEmpty(window) &&
             window.IndexOf("Explorer", StringComparison.OrdinalIgnoreCase) >= 0) ||
            !string.IsNullOrEmpty(episodeId))
            return Target.Explorer;

        if (!string.IsNullOrEmpty(window) &&
            window.IndexOf("Episodes", StringComparison.OrdinalIgnoreCase) >= 0)
            return Target.Episodes;

        // Prefer "Lemma Build" before bare "Lemma" (Properties).
        if (!string.IsNullOrEmpty(window) &&
            window.IndexOf("Lemma Build", StringComparison.OrdinalIgnoreCase) >= 0)
            return Target.LemmaBuild;

        if (!string.IsNullOrEmpty(window) &&
            window.IndexOf("Lemma", StringComparison.OrdinalIgnoreCase) >= 0)
            return Target.LemmaProperties;

        return Target.None;
    }

    public static string ExtractJsonObject(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return "";

        var token = "\"" + key + "\"";
        var start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return "";

        var brace = json.IndexOf('{', start + token.Length);
        if (brace < 0)
            return "";

        var depth = 0;
        for (var i = brace; i < json.Length; i++)
        {
            var c = json[i];
            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return json.Substring(brace, i - brace + 1);
            }
        }

        return "";
    }

    public static string ParseJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return "";

        var token = "\"" + key + "\"";
        if (!json.Contains(token))
            return "";

        var start = json.IndexOf(token, StringComparison.Ordinal);
        var valStart = json.IndexOf(':', start) + 1;
        while (valStart < json.Length && (json[valStart] == ' ' || json[valStart] == '"'))
            valStart++;

        if (valStart > 0 && valStart < json.Length && json[valStart - 1] == '"')
            valStart--;

        var valEnd = json.IndexOf('"', valStart + 1);
        if (valEnd > valStart)
            return json.Substring(valStart + 1, valEnd - valStart - 1).Trim();

        return "";
    }
}
