# Unemployment Offices, Careers, and Educational Travel

Employment lifecycle on top of `CompanyRegistration` staff lists. Government jobs are a **label** (`isGovernment` + `parentCompanyId = "government"`) with the same hire / train / fire path as private jobs.

## Flow

Gov-glove snapshot (`unemploymentRate`, else `1 - welfareBenefits`) → `CivilianDemographics` quotas → `CareerWarden.RequestCivilianPaperDoll` → assign job. If `CareerRoleSpec.requireNoPretraining`, hire immediately. Otherwise `EducationalTravelAgent.ResolvePath` builds station + credential steps for developer in-paint.

Plan-step effects: Hire / Promote / Demote / Fire.

## Paper doll

`CivilianPaperDoll` (SO) + `CivilianPaperDollRuntime`. Axes: Skill, Conduct, Reliability, Authority.

- **Blue** `expected01` — civilian expected limits
- **Red** `fireLimit01` — maximum before let go
- **White** selected `EducationalStep.expected01` — where the travel-agent step says they should be (developer in-paint)

Prison `CivilianPaperDollPreview` stays justice-only.

Unemployed dolls must fit city quotas (age bands 0–17 / 18–64 / 65+, education none/cert/degree, `unemploymentRate`). Over-quota requests are rejected.

## Lanes and trees

`LearningStationKind`: Desk, Class, Computer, Library, Phone, Conversation, Certification, UniversityCourse. Physical kinds also on `StationKind`. School/library building slots: `desk`, `computer`, `phone`.

`EducationalLane` goals become travel steps. `CareerAdvancementTree` resolves promote (role that lists current as prerequisite) and demote (first prerequisite). Management / hiring-manager flags append Conversation / Phone goals.

## CareerWarden

Venue: `CivilSystemKind.UnemploymentOffice` (`job_center`, `unemployment`, `dol`). Bootstrap: government `CompanyRegistration`, counselor/intake shifts, `AuthWarden`, `CareerWarden`, `EducationalTravelAgent`.

Grade combines:

| Axis | Sources |
|------|---------|
| Skill | education + certs/degrees + lane progress |
| Conduct | ThreatWarden, PrisonWarden, SafetyWarden |
| Reliability | employment / attendance, TrafficWarden demand |
| Authority | pecking, AuthWarden grant, management flags |

Crossing red → `CareerWardenAction.Fire`.

Duties: `JobSearch`, `BenefitsClaim`, `CareerInterview`, `JobTraining`.

## Editors

- **Locomotion → Civilian Paper Doll** — sheet + blue/red/white diamond + plan-step list
- **Locomotion → Educational Travel Agent** — resolve path, timing (rng / specific / conditional), in-paint, prebake `NarrativeEducationalEvent` rows onto a `NarrativeCalendarAsset`

## Gov-glove

`unemploymentRate` on the society snapshot. High unemployment nudges morale down and socialism up (baselines only). See `LifeSystemsGovGloveMap.md`.
