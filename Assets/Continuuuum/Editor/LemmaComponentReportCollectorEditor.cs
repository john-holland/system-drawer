#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class LemmaComponentReportCollectorEditor
{
    public static Task<bool> PostReportAsync(string entryId, ComponentMetadataPayloadDto payload, CancellationToken ct = default)
    {
        if (payload == null || string.IsNullOrEmpty(entryId))
            return Task.FromResult(false);
        payload.entryId = entryId;
        payload.source = "runtime";
        var body = JsonUtility.ToJson(payload);
        return PostEditorAsync(entryId, body, ct);
    }

    static async Task<bool> PostEditorAsync(string entryId, string body, CancellationToken ct)
    {
        var r = await ContinuuuumEditorApiClient.RequestAsync(
            "POST",
            $"/api/thesaurus/entries/{Uri.EscapeDataString(entryId)}/component-reports",
            body,
            ct);
        return r.success;
    }
}
#endif
