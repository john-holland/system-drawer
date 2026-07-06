using System.Collections.Generic;
using UnityEngine;

public sealed class FeatureBudgetRatioRegistry
{
    readonly List<FeatureBudgetRatioBinding> _bindings = new List<FeatureBudgetRatioBinding>();
    float _anchorRadius = 500f;

    public float AnchorRadius => _anchorRadius;
    public IReadOnlyList<FeatureBudgetRatioBinding> Bindings => _bindings;

    public void LoadFromProfile(FeatureBudgetProfile profile)
    {
        _bindings.Clear();
        if (profile?.ratioBindings == null)
            return;
        for (int i = 0; i < profile.ratioBindings.Count; i++)
        {
            var src = profile.ratioBindings[i];
            _bindings.Add(new FeatureBudgetRatioBinding
            {
                fieldId = src.fieldId,
                ratio = src.ratio,
                ratioLocked = src.ratioLocked,
                budgetGoverned = src.budgetGoverned,
                unlockReason = src.unlockReason,
                manualOverride = src.manualOverride,
                sourceFeatureId = src.sourceFeatureId,
                granularityLevel = src.granularityLevel
            });
        }
    }

    public void SyncFromPlanetSource(IPlanetRatioSource source, FeatureBudgetProfile profile)
    {
        if (source == null)
            return;
        _anchorRadius = Mathf.Max(0.01f, source.AnchorRadius);
        var captured = new List<RatioFieldSnapshot>();
        source.CaptureRatioFields(captured);
        MergeCapturedFields(captured, profile);
    }

    public void MergeCapturedFields(List<RatioFieldSnapshot> captured, FeatureBudgetProfile profile)
    {
        if (captured == null)
            return;
        for (int i = 0; i < captured.Count; i++)
        {
            var snap = captured[i];
            if (!TryGetBinding(snap.id, out var binding))
            {
                binding = new FeatureBudgetRatioBinding
                {
                    fieldId = snap.id,
                    ratioLocked = snap.ratioLocked,
                    budgetGoverned = false,
                    sourceFeatureId = ""
                };
                _bindings.Add(binding);
            }

            binding.ratio = snap.ratio;
            if (snap.ratioLocked)
                binding.manualOverride = snap.manualOverride;
            else if (string.IsNullOrWhiteSpace(binding.unlockReason))
                binding.ratioLocked = snap.ratioLocked;
            binding.manualOverride = snap.manualOverride;

            if (profile != null)
            {
                var persisted = profile.FindRatioBinding(snap.id);
                if (persisted != null && !string.IsNullOrWhiteSpace(persisted.unlockReason))
                {
                    binding.unlockReason = persisted.unlockReason;
                    binding.ratioLocked = persisted.ratioLocked;
                }
            }
        }
    }

    public void SetGranularityForFeature(string featureId, float level)
    {
        float clamped = Mathf.Clamp01(level);
        for (int i = 0; i < _bindings.Count; i++)
        {
            if (_bindings[i].sourceFeatureId == featureId && _bindings[i].budgetGoverned)
                _bindings[i].granularityLevel = clamped;
        }
    }

    public float GetEffectiveValue(string fieldId)
    {
        if (!TryGetBinding(fieldId, out var b))
            return 0f;
        return b.EffectiveValue(_anchorRadius);
    }

    public float GetGranularityForField(string fieldId)
    {
        return TryGetBinding(fieldId, out var b) ? b.granularityLevel : 1f;
    }

    public bool TryGetBinding(string fieldId, out FeatureBudgetRatioBinding binding)
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            if (_bindings[i].fieldId == fieldId)
            {
                binding = _bindings[i];
                return true;
            }
        }
        binding = null;
        return false;
    }

    public bool TryUnlock(string fieldId, string reason)
    {
        if (!FeatureBudgetRatioBinding.IsValidUnlockReason(reason))
            return false;
        if (!TryGetBinding(fieldId, out var binding))
            return false;
        binding.unlockReason = reason.Trim();
        binding.ratioLocked = false;
        return true;
    }

    public bool Relock(string fieldId)
    {
        if (!TryGetBinding(fieldId, out var binding))
            return false;
        if (_anchorRadius > 1e-6f)
            binding.ratio = binding.manualOverride / _anchorRadius;
        binding.ratioLocked = true;
        binding.unlockReason = "";
        return true;
    }

    public void WriteBackToProfile(FeatureBudgetProfile profile)
    {
        if (profile == null)
            return;
        profile.ratioBindings ??= new List<FeatureBudgetRatioBinding>();
        for (int i = 0; i < _bindings.Count; i++)
        {
            var src = _bindings[i];
            var dst = profile.FindRatioBinding(src.fieldId);
            if (dst == null)
            {
                profile.ratioBindings.Add(CloneBinding(src));
                continue;
            }
            dst.ratio = src.ratio;
            dst.ratioLocked = src.ratioLocked;
            dst.budgetGoverned = src.budgetGoverned;
            dst.unlockReason = src.unlockReason;
            dst.manualOverride = src.manualOverride;
            dst.sourceFeatureId = src.sourceFeatureId;
            dst.granularityLevel = src.granularityLevel;
        }
    }

    static FeatureBudgetRatioBinding CloneBinding(FeatureBudgetRatioBinding src)
    {
        return new FeatureBudgetRatioBinding
        {
            fieldId = src.fieldId,
            ratio = src.ratio,
            ratioLocked = src.ratioLocked,
            budgetGoverned = src.budgetGoverned,
            unlockReason = src.unlockReason,
            manualOverride = src.manualOverride,
            sourceFeatureId = src.sourceFeatureId,
            granularityLevel = src.granularityLevel
        };
    }
}
