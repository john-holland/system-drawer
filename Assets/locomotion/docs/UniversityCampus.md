# University Campus

Boarding-school university on the existing **EducationalTravelAgent** / **CivilianPaperDoll** / **CityPixelGrid** stack. `CivilSystemKind.School` stays the institution kind; `SchoolBootstrap` wires the campus.

## Academic success

**EducationWarden** is the academic authority. It does not replace CareerWarden (jobs stay vocational).

| Signal | Source |
|--------|--------|
| In-paint | `EducationalTravelAgent` step `inpaintWorld` / staff `inpaintPrompt` (SG4D / lemma fragment on the room) |
| Out-paint | Doll `expected01` / `fireLimit01` (Skill→scholarship, Conduct→discipline, Reliability→attendance, Authority→rank) |
| Emergent | BT attendance, flocking delay, `InnHotelVenueRuntime.SleepInPaintComfort01()` |

Grade = weighted blend. Over fire-limit → fail / probation (`EducationWarden.OverFireLimit`).

## Course load

`EducationalTravelAgent.ResolveCourseLoad` builds steps from `UniversityCurriculumAsset` for the student's `UniversityAgeBracket` (LowerSchool / UpperSchool / Undergrad / Graduate), not only career cert/degree gaps. Teacher + TA share `courseId` with pecking 15–24 vs 25–34.

Room-and-board: `InnHotelVenueRuntime` + `KeycardAccessRegistry` on the school stub. Meal/attend via existing `CivilianDutyKind.SchoolAttend`.

## Campus grid

`CityPixelGrid.EnsureCampusLayers()` adds quad, path, dorm, lecture, library, dining, maintenance, parking. **Locomotion → Campus Pixel Grid Designer** (hub: City Planning + Civil). Rooms are `CampusRoomSpec` (PixelLight slot **or** `sg4dPrompt` / `inpaintPrompt`). Floors use existing stamp `floorIndex` / `heightCells`. Optional `CampusElevationBand` or a `StreetBlocksPlanAsset` (no second MST).

Crowd / RTS hints live on `CityPixelBrushStamp` (campus **and** city): `crowdHint`, `flockGroupId`, ambulation cache key / likelihood / tolerance, `travelHintRow`.

## Crowds

`BoidsCrowdLayer` adds separation / alignment / cohesion on `TravelAgentRegistry`. Shared route still comes from `WaypointGuidanceService`. `AmbulationPathCache` reuses polylines within `cacheToleranceM`; non-human actors default to a higher cache likelihood so humans keep BT nuance.

## Scribes

`ScribePaperDoll` + `ScribeCard` (Copy / Illuminate / Bind / Deliver). **Window → System Drawer → Cards → Scribe** authors the duty card **and** page body: string content or **Upload Image**. That applies to `ScribePageRuntime` / `PenInkDrawingTarget` (`SourceKind.Text` or `Image`). Continuuuum `scribe_pages` still stores TEXT + optional library blobs. Format enum is ODF/OOXML-compatible metadata; v1 does not parse full `.odt`/`.docx`. **Pen and Ink Studio** (Look) bakes nibs, canvas, and compiles strokes for IK — not the card window.

## Feature budget

`FeatureBudgetIds.University` (`university`) rank 34. Perf prefixes: `EducationWarden`, `UniversityCampus`, `CampusPixel`. Crowds use pathing prefixes `BoidsCrowd`, `AmbulationPathCache`.
