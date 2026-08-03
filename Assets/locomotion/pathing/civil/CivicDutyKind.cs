/// <summary>Building/service repair duties for CivicCard.</summary>
public enum CivicDutyKind
{
    Inspect = 0,
    Repair = 1,
    Replace = 2,
    Secure = 3,
    Clean = 4
}

/// <summary>Individual civilian duties for CivilCard (developer-authored; filterable).</summary>
public enum CivilianDutyKind
{
    WorkShift = 0,
    Commute = 1,
    Leisure = 2,
    SchoolAttend = 3,
    Shop = 4,
    Worship = 5,
    Exercise = 6,
    RestAtHome = 7,
    FleeThreat = 8,
    Socialize = 9,
    /// <summary>Sample irreverent builtin — hide via civic content filter.</summary>
    PrivateLeisure = 10,
    GatherHomeless = 11,
    GatherKids = 12,
    FakeLibraryCard = 13
}

/// <summary>Physics material class for impulse memory tau selection.</summary>
public enum BuildingMaterialClass
{
    Generic = 0,
    Wood = 1,
    Metal = 2,
    Masonry = 3,
    Glass = 4
}
