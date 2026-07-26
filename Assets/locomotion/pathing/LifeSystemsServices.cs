using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene/actor management for life-systems sheets, timed effects, queries, and organ ops.
/// Illness never spawns spontaneously — only via ApplyEffect / lemma apply.
/// </summary>
[AddComponentMenu("Locomotion/Life Systems Services")]
public sealed class LifeSystemsServices : MonoBehaviour
{
    public const string ServiceKey = "life.systems";

    static LifeSystemsServices _instance;

    public static LifeSystemsServices Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<LifeSystemsServices>();
            return _instance;
        }
    }

    readonly List<LifeSystemsSheet> _sheets = new List<LifeSystemsSheet>();

    void Awake()
    {
        _instance = this;
        TryRegisterSystemDrawer();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    static void TryRegisterSystemDrawer()
    {
        try
        {
            var t = Type.GetType("SystemDrawerService, SystemDrawer");
            if (t == null) return;
            var instProp = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var inst = instProp?.GetValue(null);
            if (inst == null) return;
            var reg = t.GetMethod("Register", new[] { typeof(string), typeof(object) });
            reg?.Invoke(inst, new object[] { ServiceKey, Instance != null ? (object)Instance : null });
        }
        catch
        {
            // optional
        }
    }

    public LifeSystemsSheet GetOrCreate(GameObject actor)
    {
        if (actor == null) return null;
        var sheet = actor.GetComponent<LifeSystemsSheet>();
        if (sheet == null)
            sheet = actor.AddComponent<LifeSystemsSheet>();
        sheet.EnsureDefaults();
        if (actor.GetComponent<HomeostasisController>() == null)
            actor.AddComponent<HomeostasisController>();
        if (!_sheets.Contains(sheet))
            _sheets.Add(sheet);
        WireOrganHosts(sheet);
        return sheet;
    }

    public void WireOrganHosts(LifeSystemsSheet sheet)
    {
        if (sheet?.organs == null) return;
        sheet.organs.EnsureCatalogDefaults();
        var organs = OrganCatalog.Organs;
        for (int i = 0; i < organs.Count; i++)
        {
            var def = organs[i];
            if (!sheet.organs.TryGet(def.id, out var entry) || entry == null)
                continue;
            var host = BodyPartLifeModifier.FindHost(sheet, def.hostRegion);
            if (host != null)
            {
                entry.hostBodyPartPath = GetPath(host.transform);
                if (host.hostedOrganIds == null || host.hostedOrganIds.Length == 0)
                    host.hostedOrganIds = new[] { def.id };
            }
        }
    }

    static string GetPath(Transform t)
    {
        if (t == null) return "";
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    public void ApplyEffect(LifeSystemsSheet sheet, LifeSystemsEffectSpec spec)
    {
        if (sheet == null || spec == null) return;
        sheet.EnsureDefaults();
        if (string.IsNullOrEmpty(spec.id))
            spec.id = Guid.NewGuid().ToString("N");

        long nowTicks = DateTime.UtcNow.Ticks;
        if (spec.startUtcTicks > 0 && spec.startUtcTicks > nowTicks)
        {
            // Defer: store inactive until start — for v1 apply when start reached in Tick
            sheet.activeEffects.Add(new LifeSystemsActiveEffect
            {
                spec = spec,
                appliedUnscaledTime = -1,
                channelApplied = false,
                organApplied = false
            });
            return;
        }

        ApplyEffectImmediate(sheet, spec);
    }

    public void ApplyEffect(LifeSystemsEffectSpec spec)
    {
        if (spec == null) return;
        // Apply to all known sheets if no actor key; else resolve by name
        if (_sheets.Count == 0)
        {
            var found = FindObjectsByType<LifeSystemsSheet>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
                GetOrCreate(found[i].gameObject);
        }
        for (int i = 0; i < _sheets.Count; i++)
        {
            var s = _sheets[i];
            if (s == null) continue;
            if (!string.IsNullOrEmpty(spec.targetActorKey) &&
                !s.gameObject.name.Contains(spec.targetActorKey))
                continue;
            ApplyEffect(s, spec);
        }
    }

    void ApplyEffectImmediate(LifeSystemsSheet sheet, LifeSystemsEffectSpec spec)
    {
        var ae = new LifeSystemsActiveEffect
        {
            spec = spec,
            appliedUnscaledTime = Time.unscaledTimeAsDouble,
            channelApplied = true,
            organApplied = true
        };

        if (spec.channelDeltas != null)
        {
            for (int i = 0; i < spec.channelDeltas.Count; i++)
            {
                var d = spec.channelDeltas[i];
                if (d == null || string.IsNullOrEmpty(d.channelId)) continue;
                sheet.Adjust01(d.channelId, d.delta01);
            }
        }

        if (spec.organDeltas != null)
        {
            for (int i = 0; i < spec.organDeltas.Count; i++)
            {
                var d = spec.organDeltas[i];
                if (d == null || string.IsNullOrEmpty(d.organId)) continue;
                float traumaScale = 1f;
                if (sheet.organs.TryGet(d.organId, out var entry) &&
                    !string.IsNullOrEmpty(entry.hostBodyPartPath))
                {
                    var mods = sheet.GetComponentsInChildren<BodyPartLifeModifier>(true);
                    for (int m = 0; m < mods.Length; m++)
                    {
                        if (mods[m] != null && mods[m].HostsOrgan(d.organId))
                        {
                            traumaScale = mods[m].organTraumaMultiplier;
                            break;
                        }
                    }
                }
                float delta = d.rawDelta;
                if (delta < 0f)
                    delta = new BodyPartLifeModifier { organTraumaMultiplier = traumaScale }.ScaleOrganDamage(delta);
                sheet.organs.ApplyRawDelta(d.organId, delta, sheet.difficulty, 1f);
            }
        }

        if (Mathf.Abs(spec.lifeForceDelta) > 1e-6f)
            sheet.lifeForce.ApplyDelta(spec.lifeForceDelta);
        if (Mathf.Abs(spec.bioRhythmAmplitudeDelta) > 1e-6f)
            sheet.bioRhythm.ApplyAmplitudeDelta(spec.bioRhythmAmplitudeDelta);

        sheet.activeEffects.Add(ae);
    }

    public void ClearEffect(LifeSystemsSheet sheet, string effectId)
    {
        if (sheet?.activeEffects == null || string.IsNullOrEmpty(effectId)) return;
        sheet.activeEffects.RemoveAll(e => e?.spec != null &&
            string.Equals(e.spec.id, effectId, StringComparison.OrdinalIgnoreCase));
    }

    public void ClearEffect(string effectId)
    {
        for (int i = 0; i < _sheets.Count; i++)
            ClearEffect(_sheets[i], effectId);
    }

    public float GetOrganHealth(GameObject actor, string organId, bool raw = false)
    {
        var sheet = GetOrCreate(actor);
        if (sheet == null) return raw ? OrganCatalog.GreatSpawnRaw : 1f;
        return raw ? sheet.organs.GetRaw(organId) : sheet.organs.GetNormalized(organId, sheet.difficulty);
    }

    public bool TryQuery(GameObject actor, string promptOrChannel, out LifeSystemsQueryResult result)
    {
        result = default;
        var sheet = GetOrCreate(actor);
        if (sheet == null) return false;
        result = LifeSystemsQuery.Evaluate(sheet, promptOrChannel);
        return true;
    }

    public void Tick(float dt)
    {
        long nowTicks = DateTime.UtcNow.Ticks;
        double now = Time.unscaledTimeAsDouble;
        for (int s = _sheets.Count - 1; s >= 0; s--)
        {
            var sheet = _sheets[s];
            if (sheet == null)
            {
                _sheets.RemoveAt(s);
                continue;
            }
            // Activate deferred effects
            for (int i = 0; i < sheet.activeEffects.Count; i++)
            {
                var ae = sheet.activeEffects[i];
                if (ae?.spec == null || ae.channelApplied) continue;
                if (ae.spec.startUtcTicks > 0 && ae.spec.startUtcTicks > nowTicks)
                    continue;
                // Re-apply path for deferred
                var spec = ae.spec;
                sheet.activeEffects.RemoveAt(i);
                i--;
                ApplyEffectImmediate(sheet, spec);
            }
            // Expire timed effects (homeostasis resumes after)
            for (int i = sheet.activeEffects.Count - 1; i >= 0; i--)
            {
                var ae = sheet.activeEffects[i];
                if (ae?.spec == null) { sheet.activeEffects.RemoveAt(i); continue; }
                if (ae.spec.durationSeconds <= 0f) continue;
                if (ae.appliedUnscaledTime < 0) continue;
                if (now - ae.appliedUnscaledTime >= ae.spec.durationSeconds)
                    sheet.activeEffects.RemoveAt(i);
            }
        }
    }
}

public struct LifeSystemsQueryResult
{
    public string summary;
    public string channelId;
    public string organId;
    public float value01;
    public float rawValue;
    public string label;
}

public static class LifeSystemsQuery
{
    public static LifeSystemsQueryResult Evaluate(LifeSystemsSheet sheet, string q)
    {
        q = (q ?? "").Trim().ToLowerInvariant();
        if (q.StartsWith("organ:") || q.StartsWith("organ|") || q == "organ")
        {
            string id = "heart";
            int idx = q.IndexOf(':');
            if (idx < 0) idx = q.IndexOf('|');
            if (idx >= 0 && idx < q.Length - 1)
                id = q.Substring(idx + 1).Trim();
            // also support q=organ id=liver style fragments
            if (q.Contains("id="))
            {
                int p = q.IndexOf("id=", StringComparison.Ordinal);
                id = q.Substring(p + 3).Trim();
                int amp = id.IndexOfAny(new[] { '|', ' ', '&' });
                if (amp >= 0) id = id.Substring(0, amp);
            }
            float raw = sheet.organs.GetRaw(id);
            float n = sheet.organs.GetNormalized(id, sheet.difficulty);
            string label = OrganHealthNormalize.Label(n);
            return new LifeSystemsQueryResult
            {
                organId = id,
                rawValue = raw,
                value01 = n,
                label = label,
                summary = $"{id}: {label} (normalized={n:0.00}, raw={raw:0.00})"
            };
        }

        if (q == "mood" || q.Contains("mood"))
        {
            float dep = sheet.Get01(LifeSystemsChannelCatalog.Depression);
            float mania = sheet.Get01(LifeSystemsChannelCatalog.Mania);
            float morale = sheet.Get01(LifeSystemsChannelCatalog.Morale);
            float empathy = sheet.Get01(LifeSystemsChannelCatalog.Empathy);
            float valence = Mathf.Clamp01(0.5f + (morale - dep) * 0.35f + (empathy - mania) * 0.15f);
            string label = valence >= 0.7f ? "upbeat" : valence >= 0.45f ? "even" : "low";
            return new LifeSystemsQueryResult
            {
                channelId = "mood",
                value01 = valence,
                label = label,
                summary =
                    $"mood: {label} (valence={valence:0.00}; depression={dep:0.00}, mania={mania:0.00}, morale={morale:0.00}, empathy={empathy:0.00})"
            };
        }

        if (LifeSystemsChannelCatalog.TryGet(q, out _))
        {
            float v = sheet.Get01(q);
            return new LifeSystemsQueryResult
            {
                channelId = q,
                value01 = v,
                rawValue = sheet.GetClinical(q),
                label = v.ToString("0.00"),
                summary = $"{q}={v:0.00}"
            };
        }

        return new LifeSystemsQueryResult
        {
            summary = "unknown query",
            label = "unknown"
        };
    }
}
