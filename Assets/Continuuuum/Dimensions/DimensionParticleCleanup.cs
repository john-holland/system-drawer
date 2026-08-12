using System.Collections.Generic;
using UnityEngine;

/// <summary>Stops/clears particle systems at dimension switch; optional orphan destroy for dim-local roots.</summary>
public static class DimensionParticleCleanup
{
    public static void Run(IEnumerable<Transform> roots, bool destroyDimLocalOrphans = true)
    {
        if (roots == null)
            return;
        foreach (var root in roots)
        {
            if (root == null)
                continue;
            RunOnRoot(root, destroyDimLocalOrphans);
        }
    }

    public static void RunOnRoot(Transform root, bool destroyDimLocalOrphans = true)
    {
        if (root == null)
            return;
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null)
                continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.enabled = false;
            ps.Clear(true);
        }

        if (!destroyDimLocalOrphans)
            return;

        var toDestroy = new List<GameObject>();
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null)
                continue;
            var go = ps.gameObject;
            if (go == root.gameObject)
                continue;
            if (go.name.IndexOf("DimLocal", System.StringComparison.OrdinalIgnoreCase) >= 0)
                toDestroy.Add(go);
        }
        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (toDestroy[i] != null)
                Object.Destroy(toDestroy[i]);
        }
    }

    /// <summary>Re-enable emission on KeepAlive hosts after fade completes.</summary>
    public static void RestartEmission(IEnumerable<Transform> roots)
    {
        if (roots == null)
            return;
        foreach (var root in roots)
        {
            if (root == null)
                continue;
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null)
                    continue;
                var emission = ps.emission;
                emission.enabled = true;
                if (!ps.isPlaying)
                    ps.Play(true);
            }
        }
    }
}
