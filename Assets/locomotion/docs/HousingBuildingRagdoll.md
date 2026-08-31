# HousingBuildingRagdoll

House specialization of `BuildingRagdoll`.

## Components

- `HousingBuildingRagdoll` — slots, architecture lemma, plumbing group
- `HouseBioRhythm` — cleanliness, trash, laundry, utility comfort
- `FamilyPeckingOrder` — residents + affinity/authority
- `HouseChoreCard` / `HouseChoreCatalog` — trash, dishes, laundry, clean, yard, utility maintain
- `UtilityBioRhythm` / `UtilityRoomBootstrap` / `HouseUtilityTap` / `HouseBasementFloodCache` — basement plant, street tap, flood prebake (see [HouseUtility.md](HouseUtility.md))
- `HouseInventoryBinder` — `bedroom2` / `bedroom2_dresser2` context paths

## Architecture lemmas

`quaint`, `good_size`, `mc_mansion`, `mansion`, `cabin`, `cottage`, `townhome` → footprint scale + room count hints (`HousingArchitectureLemmaPropertyKeys` / Continuuuum `HousingLemmaPropertyKeys`).

## Editor

`Window → System Drawer → Civil → Building Requirements` — House preset + Ensure HousingBuildingRagdoll.
