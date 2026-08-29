# Legal, court, and government

Shared wardens with relationship live under `Assets/locomotion/pathing/civil/wardens/`. Legal owns court, constitution, law, government mix, courtroom PixelLight, Conversation Bus, and Law Travel Agent. Do not add a second TheocraticWarden or JusticeWarden.

## Court / LegalBuilding

`CivilSystemKind.CourtHouse` (ids `courthouse`, `court`, `legal`) bootstraps `LegalBuildingBootstrap`: rooms (chambers, courtroom, offices, meeting, cafeteria, bathrooms, optional gov/company suites), `CourtWarden`, `CorruptionWarden`, `LegalSystemTravelAgent` (file → hearing → trial → ruling).

**CourtKind:** American (adversarial, jury), English (inquisitorial), Kangaroo (bypasses Rights/Constitution). `CourtSystemBioRhythm` drives session hours, docket stress, audience fill.

**CorruptionWarden** gates subversion/sedition (spy agency / private investigator flags). Criminality stays a stub coefficient.

## Courtroom PixelLight

**Locomotion → Courtroom PixelLight Designer** (hub: City Planning + Civil + Narrative for travel agents). `CityPixelGrid.EnsureCourtroomLayers()` adds bench, well, jury, gallery, bar. Rooms take PixelLight slot **or** `sg4dPrompt`. `ExportCourtroomClustersToBounds4` + `CourtroomSgNodes` / `CourtroomBounds4Export`.

**AngleBase3D** yaw/pitch/roll (or look-at) per gallery cell. `CourtroomSeatBt` copies those transforms into `VehicleSeating.occupantAnchors`.

## Constitution and bicameral law

`CountrySpec` / `StateSpec`, `Congress` (House + Senate, optional parliament dolls), `LawCard` (bill text, chamber, sponsor, `LawStageKind` including filibuster / veto / amendment), `ConstitutionAsset` + Bill of Rights articles.

`ConstitutionWarden` scores bills vs articles and enqueues `constitution-right-limited` / `constitution-right-removed` / `constitution-rights-returned`. `AnnounceRightsReturned` re-enables the named article (or all articles), turns `articlesEnabled` back on, and clears `JuntaRuntime.canSuspendConstitution`. `JuntaRuntime.canSuspendConstitution` disables Constitution/Rights. `JuntaRuntime.respectsGenevaConventions` (default true) is read by `GenevaConventionWarden`, which uses `ThreatWarden.IsTorture` (Consent / Rights / Justice / Romance). When the flag is false, Geneva allow is 0. `PrisonWarden` has the same flag and forces Restraint on physical (or once torture is flagged) while it respects conventions. `RightsWarden` reads Constitution; `JusticeWarden` reads Rights. `LawWarden` blends `LawCard` with `GovernmentModelRagdoll` mix.

## Conversation Bus / Law Travel Agent

`ConversationBusTravelAgent` is telecom dialog steps, not a transit bus. Accordion **new++!** adds a section type; **new+!** clones a `WardenLimitKv` row. Diamonds use Court/Corruption/Constitution/Rights/Justice/Theocratic/Love/Romance/Consent when present, otherwise **0.5**. Prompt includes observed laws + scripture (`ComposeDialoguePrompt`); Prompt Editor **Gen dialogue** appends that context; calendar **Prebake**.

`LawTravelAgent` composes Conversation Bus legs. GraphView columns are law stages (**Add new stage**, per-stage **... → Remove**). Diamonds: Constitution / Justice / Rights / Love / Romance / Consent. Undo/Redo toolbars on courtroom, conversation, and law editors.

## Government mix and theocracy

`GovernmentModelBioRhythm` / `GovernmentModelRagdoll` persist flavor coefficients (republic, parliamentary, theocracy, ceremonial vs real monarchy, junta). Parliamentary senate can **enable theocracy**. `GovernmentWarden` covers TownHall / GovLegislative / UnemploymentOffice. `TheocraticWarden` named SG3D/SG4D + scripture; `ChurchTheocracyBootstrap` on `CivilSystemKind.Church`.

Cards: King/Queen (ceremonial vs real), Knight, Squire, Knave, Jester (dance/dialogue/parkour + stub crime coeff), Councilor, Chancellor (`isHeadOfUniversity`, shared `PaintCanvas` + `PenInkInstrument`), Executive (`Militaristic`). Holy-text decals use `PaintTransferDecal` `normal * 0.001f + inkLayer * 0.0005f` and optional `PaintCanvas.SurfaceKind.CurvedDecal`.

## Budget

`FeatureBudgetIds.LegalCourt` (`legal_court`) rank **35**. Prefixes: `CourtWarden`, `CorruptionWarden`, `ConstitutionWarden`, `RightsWarden`, `LawWarden`, `GovernmentWarden`, `TheocraticWarden`, `JusticeWarden`, `GenevaConventionWarden`, `LawTravelAgent`, `ConversationBusTravelAgent`, `LegalBuilding`.

Lemma keys: court, constitution, scripture, chamber, rights, law, junta, announce, returned, rights-returned, announce-rights-returned (`LegalLemmaPropertyKeys`).
