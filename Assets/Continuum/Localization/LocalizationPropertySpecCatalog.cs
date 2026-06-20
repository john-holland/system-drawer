using System.Collections.Generic;
using UnityEngine;

public static class LocalizationPropertyKeys
{
    public const string NonIkAnimation = "non-ik-animation";
}

[CreateAssetMenu(fileName = "LocalizationPropertySpec", menuName = "Continuum/Localization Property Spec")]
public sealed class LocalizationPropertySpecAsset : ScriptableObject
{
    public string key = "non-ik-animation";
    public string valueType = "Bool";
    public string[] allowedValues = { "true", "false" };
    public string defaultValue = "false";
    [TextArea] public string description = "When true, ragdoll playback uses kinematic Non-IK sampling instead of physics cards.";

    public LocalizationPropertySpecRecord ToRecord() => new LocalizationPropertySpecRecord
    {
        key = key,
        valueType = valueType,
        allowedValuesJson = allowedValues != null ? string.Join(",", allowedValues) : "",
        defaultValue = defaultValue,
        description = description
    };
}

[CreateAssetMenu(fileName = "LocalizationPropertySpecCatalog", menuName = "Continuum/Localization Property Spec Catalog")]
public sealed class LocalizationPropertySpecCatalog : ScriptableObject
{
    public List<LocalizationPropertySpecAsset> specs = new List<LocalizationPropertySpecAsset>();

    public bool TryGet(string key, out LocalizationPropertySpecAsset spec)
    {
        spec = null;
        if (string.IsNullOrEmpty(key) || specs == null)
            return false;
        foreach (var s in specs)
        {
            if (s != null && string.Equals(s.key, key, System.StringComparison.OrdinalIgnoreCase))
            {
                spec = s;
                return true;
            }
        }
        return false;
    }

    public static LocalizationPropertySpecRecord[] BuildDefaultRecords() => new[]
    {
        new LocalizationPropertySpecRecord
        {
            key = LocalizationPropertyKeys.NonIkAnimation,
            valueType = "Bool",
            allowedValuesJson = "[\"true\",\"false\"]",
            defaultValue = "false",
            description = "When true, ragdoll playback uses kinematic Non-IK sampling instead of physics cards."
        }
    };

    public static LocalizationPropertySpecCatalog CreateDefaultAsset()
    {
        var catalog = CreateInstance<LocalizationPropertySpecCatalog>();
        var spec = CreateInstance<LocalizationPropertySpecAsset>();
        catalog.specs.Add(spec);
        return catalog;
    }
}
