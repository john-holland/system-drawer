/// <summary>Civil venue kinds managed by PersonaDayManager lattice.</summary>
public enum CivilSystemKind
{
    Generic = 0,
    Kitchen = 1,
    School = 2,
    Mall = 3,
    Library = 4,
    Church = 5,
    SoupKitchen = 6,
    Factory = 7,
    LiquorStore = 8,
    PoliceStation = 9,
    Bathroom = 10,
    CarRepair = 11,
    Gym = 12,
    House = 13,
    GasStation = 14,
    TownHall = 15,
    NightClub = 16,
    Bar = 17,
    Inn = 18,
    Hotel = 19,
    MilitaryCheckpoint = 20,
    SpyAgency = 21,
    Embassy = 22,
    GovLegislative = 23,
    Monarchic = 24,
    Spa = 25,
    PrivateIndustry = 26,
    BarberShop = 27,
    FireStation = 28,
    BusDepot = 29,
    TransitHub = 30,
    Airport = 31
}

/// <summary>Per-venue simulation fidelity under budget + speed LOD.</summary>
public enum CivilLodTier
{
    FullSim = 0,
    Proxy = 1,
    Ghost = 2,
    Culled = 3
}
