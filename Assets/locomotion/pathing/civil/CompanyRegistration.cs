using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>PVI-style company host mixin for restaurants, hotels, agencies, offices.</summary>
public interface ICompanyHost
{
    CompanyRegistration Company { get; }
}

[Serializable]
public sealed class CompanyFundingSource
{
    public string sourceId;
    public string label;
    [Range(0f, 1f)] public float share01 = 1f;
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Company Registration")]
public sealed class CompanyRegistration : MonoBehaviour, ICompanyHost
{
    public string companyId;
    public string displayName;
    public string parentCompanyId;
    public List<CompanyFundingSource> fundingSources = new List<CompanyFundingSource>();
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();

    public CompanyRegistration Company => this;

    void Awake()
    {
        if (string.IsNullOrEmpty(companyId))
            companyId = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = companyId;
    }

    public Dictionary<string, object> ToDto()
    {
        var staffDto = new List<object>();
        for (int i = 0; i < staff.Count; i++)
        {
            var s = staff[i];
            if (s == null) continue;
            staffDto.Add(new Dictionary<string, object>
            {
                ["personaKey"] = s.personaKey ?? "",
                ["role"] = s.role ?? "",
                ["peckingOrder"] = s.peckingOrder
            });
        }
        return new Dictionary<string, object>
        {
            ["companyId"] = companyId,
            ["displayName"] = displayName,
            ["parentCompanyId"] = parentCompanyId ?? "",
            ["staff"] = staffDto
        };
    }
}
