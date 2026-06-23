using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Buffers runtime component-creation reports and pushes them to Continuum in dev mode.
/// </summary>
public static class LemmaComponentReportCollector
{
    static readonly List<ComponentMetadataPayloadDto> _buffer = new List<ComponentMetadataPayloadDto>();
    static string _runId;

    public static string CurrentRunId
    {
        get
        {
            if (string.IsNullOrEmpty(_runId))
                _runId = Guid.NewGuid().ToString();
            return _runId;
        }
    }

    public static void NotifyPrefabSpawned(
        string entryId,
        GameObject instance,
        string prefabRef = null,
        ComponentMetadataSpatialBucketDto[] buckets = null,
        ComponentMetadataCausalityLinkDto[] causalityLinks = null)
    {
        if (!ContinuumApiConfig.ShouldPushRuntimeComponentReports || string.IsNullOrEmpty(entryId) || instance == null)
            return;
        try
        {
            var nodes = new List<ComponentMetadataNodeDto>();
            WalkInstance(instance.transform, instance.name, nodes);
            var payload = new ComponentMetadataPayloadDto
            {
                schemaVersion = 1,
                entryId = entryId,
                prefabRef = prefabRef ?? "",
                source = "runtime",
                capturedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                runId = CurrentRunId,
                nodes = nodes.ToArray(),
                spatialBuckets = buckets ?? Array.Empty<ComponentMetadataSpatialBucketDto>(),
                causalityLinks = causalityLinks ?? Array.Empty<ComponentMetadataCausalityLinkDto>(),
            };
            lock (_buffer)
                _buffer.Add(payload);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LemmaComponentReportCollector] NotifyPrefabSpawned failed: {ex.Message}");
        }
    }

    public static void FlushOnSceneUnload()
    {
        if (!ContinuumApiConfig.ShouldPushRuntimeComponentReports)
            return;
        List<ComponentMetadataPayloadDto> copy;
        lock (_buffer)
        {
            if (_buffer.Count == 0)
                return;
            copy = new List<ComponentMetadataPayloadDto>(_buffer);
            _buffer.Clear();
        }
        _ = FlushReportsAsync(copy);
    }

    static async Task FlushReportsAsync(List<ComponentMetadataPayloadDto> reports)
    {
        foreach (var group in GroupByEntry(reports))
        {
            foreach (var payload in group.Value)
                await PostReportAsync(group.Key, payload);
        }
    }

    static Dictionary<string, List<ComponentMetadataPayloadDto>> GroupByEntry(List<ComponentMetadataPayloadDto> reports)
    {
        var map = new Dictionary<string, List<ComponentMetadataPayloadDto>>();
        foreach (var r in reports)
        {
            if (string.IsNullOrEmpty(r.entryId))
                continue;
            if (!map.TryGetValue(r.entryId, out var list))
            {
                list = new List<ComponentMetadataPayloadDto>();
                map[r.entryId] = list;
            }
            list.Add(r);
        }
        return map;
    }

    public static async Task<bool> PostReportAsync(string entryId, ComponentMetadataPayloadDto payload, CancellationToken ct = default)
    {
        if (ContinuumApiConfig.DisableReportingForSkunkWorks || payload == null || string.IsNullOrEmpty(entryId))
            return false;
        payload.entryId = entryId;
        payload.source = "runtime";
        payload.runId = payload.runId ?? CurrentRunId;
        var url = ContinuumApiConfig.GetApiBaseUrl().TrimEnd('/') +
                  $"/api/thesaurus/entries/{Uri.EscapeDataString(entryId)}/component-reports";
        var body = JsonUtility.ToJson(payload);
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            if (ct.IsCancellationRequested)
            {
                req.Abort();
                return false;
            }
            await Task.Yield();
        }
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[LemmaComponentReportCollector] POST failed: {req.error}");
            return false;
        }
        return true;
    }

    static void WalkInstance(Transform t, string path, List<ComponentMetadataNodeDto> nodes)
    {
        var comps = t.GetComponents<Component>();
        var compDtos = new List<ComponentMetadataComponentDto>();
        foreach (var c in comps)
        {
            if (c == null || c is Transform)
                continue;
            compDtos.Add(new ComponentMetadataComponentDto
            {
                typeName = c.GetType().Name,
                assembly = c.GetType().Assembly.GetName().Name,
            });
        }
        nodes.Add(new ComponentMetadataNodeDto
        {
            path = path,
            gameObjectName = t.name,
            components = compDtos.ToArray(),
        });
        for (var i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            WalkInstance(child, path + "/" + child.name, nodes);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RegisterSceneHook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += _ => FlushOnSceneUnload();
    }
}
