using UnityEngine;

/// <summary>Domestic bio channels for HousingBuildingRagdoll.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Bio Rhythm")]
public sealed class HouseBioRhythm : MonoBehaviour
{
    public BuildingBioRhythmService buildingBio;
    [Range(0f, 1f)] public float cleanliness01 = 0.7f;
    [Range(0f, 1f)] public float trashFill01;
    [Range(0f, 1f)] public float laundryLoad01;
    [Range(0f, 1f)] public float utilityComfort01 = 0.8f;
    [Range(0f, 1f)] public float gasAvailable01 = 1f;
    [Range(0f, 1f)] public float oilAvailable01 = 1f;
    [Range(0f, 1f)] public float electricAvailable01 = 1f;

    void Awake()
    {
        if (buildingBio == null)
            buildingBio = GetComponent<BuildingBioRhythmService>();
    }

    public void Tick(float dt)
    {
        buildingBio?.Tick(dt);
        trashFill01 = Mathf.MoveTowards(trashFill01, trashFill01 > 0.01f ? 1f : 0f, dt * 0.002f);
        laundryLoad01 = Mathf.MoveTowards(laundryLoad01, laundryLoad01 > 0.01f ? 1f : 0f, dt * 0.0015f);
        cleanliness01 = Mathf.MoveTowards(cleanliness01, 0.35f, dt * 0.001f);
        utilityComfort01 = Mathf.Clamp01((gasAvailable01 + oilAvailable01 + electricAvailable01) / 3f);
    }

    public void ApplyChore(HouseChoreKind chore)
    {
        switch (chore)
        {
            case HouseChoreKind.TakeOutTrash:
                trashFill01 = 0f;
                break;
            case HouseChoreKind.Laundry:
                laundryLoad01 = Mathf.Clamp01(laundryLoad01 - 0.5f);
                break;
            case HouseChoreKind.Clean:
                cleanliness01 = Mathf.Clamp01(cleanliness01 + 0.25f);
                break;
            case HouseChoreKind.Yard:
                cleanliness01 = Mathf.Clamp01(cleanliness01 + 0.05f);
                break;
        }
    }
}
