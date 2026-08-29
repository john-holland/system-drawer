using System;
using UnityEngine;

/// <summary>Win or lose property assignment applied on certify.</summary>
[Serializable]
public sealed class VotePropertyAssignment
{
    public string propertyName = "governor";
    public string propertyValue = "";

    public VotePropertyAssignment() { }

    public VotePropertyAssignment(string name, string value)
    {
        propertyName = name ?? "";
        propertyValue = value ?? "";
    }
}

/// <summary>Runtime bag of certified civic properties (governor, law.state.25b, …).</summary>
[Serializable]
public sealed class VotePropertyBag
{
    public const string HomeAddressKey = "homeAddress";

    public System.Collections.Generic.List<VotePropertyAssignment> values =
        new System.Collections.Generic.List<VotePropertyAssignment>();

    public string Get(string propertyName)
    {
        if (values == null || string.IsNullOrEmpty(propertyName)) return null;
        for (int i = 0; i < values.Count; i++)
            if (values[i] != null && values[i].propertyName == propertyName)
                return values[i].propertyValue;
        return null;
    }

    public void Set(string propertyName, string propertyValue)
    {
        if (string.IsNullOrEmpty(propertyName)) return;
        if (values == null) values = new System.Collections.Generic.List<VotePropertyAssignment>();
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] != null && values[i].propertyName == propertyName)
            {
                values[i].propertyValue = propertyValue ?? "";
                return;
            }
        }
        values.Add(new VotePropertyAssignment(propertyName, propertyValue));
    }
}
