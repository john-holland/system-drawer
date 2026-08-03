# Hospitality, Security & Specialty Venues

Shared spines and per-institution packages for NightClub through BarberShop. Deep IK (knighting, barber SPH live mask) is stretch; hooks and stubs ship first.

## Shared spines

| Type | Role |
|------|------|
| `CompanyRegistration` / `ICompanyHost` | PVI-style company id, funding, staff pecking, optional parent |
| `ParkingLot` | Indexes `ParkingZoneVolume`; `SeedTravelAgents` on venue open |
| `CivilVenueAmenities` | parking + company + music + kitchen + front desk / dance floor |
| `MusicAmbianceSchedule` | Cron slots → composition id, ambiance tag, celebrity appearance |
| `BeatQuantizedActionBinder` | Snaps card starts to beat subdivisions |
| `DanceIkTrainingCatalog` | `bar_sway`, `club_groove`, … |
| `KeycardItem` / `KeycardLock` / `KeycardAccessRegistry` | Door locks + Continuuuum registry |

Venue wake: `CivilInstitutionStub.SetAwake` → amenities open/close + `OnCivilVenueOpen` / `OnCivilVenueClose`. Hospitality kinds auto-add `HospitalityInstitutionBootstrap`.

## Venue packages

Each kind has BioRhythm + Runtime + cards (see `pathing/civil/venues/`).

| Kind | Notes |
|------|--------|
| NightClub | Music-quantized groove, `NightClubCard`, `BouncerCard` / `ValetCard` |
| Bar | Ambiance schedule, optional dance + Justice detail |
| Inn / Hotel | Keycards, `HotelCard` / `MaidCard`, linens, maintenance company; Inn = kin pecking, Hotel = corporate |
| MilitaryCheckpoint | Ops center + entrance/exit Justice, beds Civic upkeep |
| SpyAgency | Steep Justice all staff (incl. kitchen), hushed/classical meetings |
| Embassy | Moderate civilian Justice; steeper army |
| GovLegislative | Company pecking, state/local LE Justice |
| Monarchic | Work↔home waypoints; `MonarchCard` decorum stubs |
| Spa | `SpaCard` ∘ Wrestling treatments |
| PrivateIndustry | Company + optional valet/checkpoint; MonarchCard where helpful |
| BarberShop | `BarberCard` ∘ Spa ∘ Wrestling; hairdo blend stub via wet mask |

## Continuuuum

- Page: `/keycards` — bind keycard ↔ node, list actors; thin company CRUD
- `GET/PUT /api/civil/keycards`
- `GET/PUT /api/civil/companies/<id>`
- `GET /api/civil/hospitality-meta`
- Persona-day `VENUE_CATALOG` + settings: `musicQuantizeEnabled`, `keycardLateCheckoutTelecomPolicy`

## Defaults

- Spy kitchen staff: steep Justice like other employees
- Embassy army Justice steeper (lower violence threshold) than civilian staff
- Inn nepotism via family pecking; Hotel corporate pecking
- Barber hair flux may use `HairdoBlend` params when live SPH wet mask is incomplete
