using System;

[Serializable]
public sealed class LocalizationChangeListDetailRecord : LocalizationChangeListRecord
{
    public LocalizationChangeListItemRecord[] items;
}
