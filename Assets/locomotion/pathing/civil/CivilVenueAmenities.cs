using UnityEngine;

/// <summary>Optional amenities shared by Kitchen, NightClub, Hotel, Spy, Industry, Checkpoint.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Civil Venue Amenities")]
public sealed class CivilVenueAmenities : MonoBehaviour
{
    public ParkingLot parkingLot;
    public CompanyRegistration company;
    public MusicAmbianceSchedule musicSchedule;
    public RestaurantVenueRuntime kitchenVenue;
    public Transform frontDesk;
    public Transform danceFloor;
    public bool seedParkingOnOpen = true;

    void Awake()
    {
        if (parkingLot == null) parkingLot = GetComponentInChildren<ParkingLot>();
        if (company == null) company = GetComponent<CompanyRegistration>();
        if (musicSchedule == null) musicSchedule = GetComponent<MusicAmbianceSchedule>();
        if (kitchenVenue == null) kitchenVenue = GetComponentInChildren<RestaurantVenueRuntime>();
    }

    public void OnVenueOpen()
    {
        if (seedParkingOnOpen)
            parkingLot?.SeedTravelAgents();
        kitchenVenue?.SetOpen(true);
    }

    public void OnVenueClose()
    {
        kitchenVenue?.SetOpen(false);
    }
}
