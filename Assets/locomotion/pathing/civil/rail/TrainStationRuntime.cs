using UnityEngine;

/// <summary>Train station venue — couple / unfold / park ops for platform BT trees.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Station Runtime")]
public sealed class TrainStationRuntime : TrainStationOpsBase
{
    public Transform platformAnchor;
    public BuildingRagdoll buildingRagdoll;
    public CompanyRegistration company;
    public string hoursCron = "* 5-23 * * *";
    public bool isOpen = true;

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponent<BuildingRagdoll>();
        if (company == null)
            company = GetComponent<CompanyRegistration>() ?? gameObject.AddComponent<CompanyRegistration>();
        if (activeConsist == null)
            activeConsist = GetComponentInChildren<TrainVehicleRagdoll>();
        if (activeCar == null && activeConsist != null)
            activeCar = activeConsist.Head;
    }
}
