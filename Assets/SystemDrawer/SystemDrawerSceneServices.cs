using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves scene services by canonical key: <see cref="SystemDrawerService"/> first, then controlled Find fallback.
/// </summary>
public static class SystemDrawerSceneServices
{
    static readonly HashSet<string> WarnedFallbackKeys = new HashSet<string>();

    /// <summary>Resolve a UnityEngine.Object by key and expected type.</summary>
    public static bool TryResolve(string key, Type expectedType, out UnityEngine.Object result)
    {
        result = null;
        if (string.IsNullOrEmpty(key) || expectedType == null)
            return false;

        SystemDrawerService svc = SystemDrawerService.FindInScene();
        if (svc != null)
        {
            UnityEngine.Object registered = svc.Get<UnityEngine.Object>(key);
            if (registered != null)
            {
                if (!expectedType.IsInstanceOfType(registered))
                {
                    if (Application.isPlaying)
                        Debug.LogWarning(
                            $"[SystemDrawerSceneServices] Key '{key}' registered type {registered.GetType().Name} does not match requested {expectedType.Name}.");
                    return false;
                }

                result = registered;
                return true;
            }
        }

        if (typeof(Component).IsAssignableFrom(expectedType))
        {
            var found = UnityEngine.Object.FindFirstObjectByType(expectedType) as UnityEngine.Object;
            if (found != null)
            {
                WarnFallbackOnce(key, expectedType);
                result = found;
                return true;
            }
        }

        return false;
    }

    /// <summary>Resolve a typed scene service.</summary>
    public static bool TryResolve<T>(string key, out T result) where T : UnityEngine.Object
    {
        result = null;
        if (!TryResolve(key, typeof(T), out UnityEngine.Object obj))
            return false;
        result = obj as T;
        return result != null;
    }

    /// <summary>Lists canonical keys that are not registered and would require Find fallback.</summary>
    public static IReadOnlyList<string> GetUnresolvedRequiredKeys(params string[] requiredKeys)
    {
        var missing = new List<string>();
        if (requiredKeys == null)
            return missing;

        SystemDrawerService svc = SystemDrawerService.FindInScene();
        for (int i = 0; i < requiredKeys.Length; i++)
        {
            string key = requiredKeys[i];
            if (string.IsNullOrEmpty(key))
                continue;
            if (svc != null && svc.Get<UnityEngine.Object>(key) != null)
                continue;
            missing.Add(key);
        }

        return missing;
    }

    static void WarnFallbackOnce(string key, Type expectedType)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!WarnedFallbackKeys.Add(key))
            return;
        Debug.LogWarning(
            $"[SystemDrawerSceneServices] Key '{key}' ({expectedType.Name}) resolved via FindFirstObjectByType fallback. Register via SystemDrawerService or a wizard Push.",
            SystemDrawerService.FindInScene());
#endif
    }
}
