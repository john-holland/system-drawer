using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Drives dimensional cross-fade from DimensionalShaderComponent jobs.</summary>
[DefaultExecutionOrder(-100)]
public sealed class DimensionMaterialCrossFader : MonoBehaviour
{
    public static DimensionMaterialCrossFader Instance { get; private set; }

    static readonly HashSet<DimensionalShaderComponent> Registered = new HashSet<DimensionalShaderComponent>();

    public float defaultDurationSeconds = 0.35f;
    public AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public DimensionalShaderComponent sceneDefault;

    public bool IsFading { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static DimensionMaterialCrossFader Ensure()
    {
        if (Instance != null)
            return Instance;
        var existing = FindAnyObjectByType<DimensionMaterialCrossFader>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        var go = new GameObject("DimensionMaterialCrossFader");
        return go.AddComponent<DimensionMaterialCrossFader>();
    }

    public static void Register(DimensionalShaderComponent c)
    {
        if (c != null)
            Registered.Add(c);
    }

    public static void Unregister(DimensionalShaderComponent c)
    {
        if (c != null)
            Registered.Remove(c);
    }

    public IEnumerator FadeInScope(Transform scopeRoot, Action onComplete = null)
    {
        IsFading = true;
        var jobs = new List<DimensionalShaderFadeJob>();
        var comps = CollectComponents(scopeRoot);
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i].TryBuildFadeJob(out var job) && job != null)
                jobs.Add(job);
        }
        if (jobs.Count == 0 && sceneDefault != null && sceneDefault.TryBuildFadeJob(out var def) && def != null)
            jobs.Add(def);

        float maxDur = defaultDurationSeconds;
        for (int i = 0; i < jobs.Count; i++)
            maxDur = Mathf.Max(maxDur, jobs[i].durationSeconds);

        float elapsed = 0f;
        while (elapsed < maxDur)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                float t = Mathf.Clamp01(elapsed / job.durationSeconds);
                float curved = job.blendCurve != null ? job.blendCurve.Evaluate(t) : t;
                job.source.ApplyBlend(job, curved);
            }
            yield return null;
        }

        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            job.source.ApplyBlend(job, 1f);
            if (job.commitOnComplete)
                job.source.CommitB();
        }

        IsFading = false;
        onComplete?.Invoke();
    }

    List<DimensionalShaderComponent> CollectComponents(Transform scopeRoot)
    {
        var list = new List<DimensionalShaderComponent>();
        if (scopeRoot != null)
        {
            var local = scopeRoot.GetComponentsInChildren<DimensionalShaderComponent>(true);
            for (int i = 0; i < local.Length; i++)
                if (local[i] != null)
                    list.Add(local[i]);
        }
        foreach (var c in Registered)
        {
            if (c == null || list.Contains(c))
                continue;
            if (scopeRoot == null || c.transform.IsChildOf(scopeRoot) || c.transform == scopeRoot)
                list.Add(c);
            else if (scopeRoot == null)
                list.Add(c);
        }
        if (list.Count == 0)
        {
            foreach (var c in Registered)
                if (c != null)
                    list.Add(c);
        }
        return list;
    }
}
