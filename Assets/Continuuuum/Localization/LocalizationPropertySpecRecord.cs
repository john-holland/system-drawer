using System;

[Serializable]
public sealed class LocalizationPropertySpecRecord
{
    public string key;
    public string valueType;
    public string allowedValuesJson;
    public string defaultValue;
    public string description;
}
