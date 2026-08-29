# Relationship wardens, route, and editor

Love / Romance / Consent venue wardens that score and route. They do not replace lovemaking IK (`LoveCard`, `RomanceProfile`, `RomanceSeverity` stay). Shared Theocratic + Justice(+Rights) contracts live here so legal/court can implement the same types once.

**Compatibility:** this feature owns Love / Romance / Consent and defines shared Theocratic + Justice(+Rights). Legal/court owns Court, Corruption, Constitution, Rights, Law, Government, junta. Relationship **reads** those types; until legal ships, this tree includes thin compatible stubs (`lastScore01` / `Allow01()`).

**Out of scope:** The Gap in the mall (no `MallGapWarden`, no `CivilSystemKind.TheGap`). Mall stays `CivilSystemKind.Mall`. Criminality beyond Corruption coefficients. Courtroom PixelLight.

Informal warden contract (no required base class): `float lastScore01`, `float Allow01()` (default `lastScore01`), optional action enum, optional `WardenLimitKv[]`.

Shared types: `Assets/locomotion/pathing/civil/wardens/`. Relationship-specific: `Assets/locomotion/pathing/civil/relationship/`.

## Catalog

| Class | Owner | Role | Score / action | Relationship use |
|-------|-------|------|----------------|------------------|
| **ThreatWarden** | existing kitchen | Agency alert/threat | `threatScore01`, `alertScore01`, `MaxThreat01()` | Consent input `(1 - threat)` |
| **TheocraticWarden** | shared | Church/doctrine; named SG3D `string→Vector3` and SG4D `string→Bounds4`; active scripture refs | Allow / Counsel / Forbid | Consent doctrine axis; diamond Doctrine |
| **ConstitutionWarden** | legal (stub) | Bills vs articles; junta can suspend | `lastScore01` | Indirect via Rights |
| **RightsWarden** | legal (stub) | Reads ConstitutionWarden (0 if junta suspended / kangaroo court) | `lastScore01` | Consent extra weight; Justice input |
| **JusticeWarden** | shared | Wraps `PrisonWarden.lastScore01` + `JusticeCard.EffectiveViolenceThreshold01` + RightsWarden | Allow / Caution / Restrain | Consent justice axis; diamond Safety |
| **LoveWarden** | relationship | Affection/physicality toward a stage | 4-axis `lastGrade01` | Route complete; diamond Affection |
| **RomanceWarden** | relationship | `RomanceSeverity`, fidelity, admirers | stage + `lastGrade01` | Route complete; diamond Affection/stage |
| **ConsentWarden** | relationship | Aggregator; caps Love physicality; SoftGate | `lastScore01`, `maxPhysicality01` | Required; planner hook |
| **CourtWarden** | legal (stub) | Trial procedure, pecking | Proceed / Recess / Mistrial | Optional marriage/license / restraining-order steps |
| **CorruptionWarden** | legal (stub) | Subversion/sedition | `lastScore01` | Optional dialog escape |
| **GovernmentWarden** | legal (stub) | Civic authority (not CareerWarden) | `lastScore01` | Optional civil marriage / license |
| **LawWarden** | legal (stub) | `LawCard` / `ReligiousLawCard` through-lines | `lastScore01` | Optional statute vs custom/religious marriage |

**Junta / kangaroo:** `JuntaRuntime.canSuspendConstitution` or `CourtKind.Kangaroo` → Rights and Constitution report ~0 allow → Justice leans Restrain and Consent tightens when `wRights > 0`. Relationship does not reimplement junta.

**Geneva:** `ThreatWarden.IsTorture` consults Consent / Rights / Justice / Romance. `GenevaConventionWarden` (legal) scores compliance; `respectsGenevaConventions` on junta and prison defaults true.

## Consent blend

Unused sources contribute 0 and remaining weights **renormalize** (a missing legal warden is not a hole):

```
consent01 =
  wThreat * (1 - threatScore01) +
  wTheo   * theo.Allow01() +
  wJust   * justice.Allow01() +
  wRights * rights.Allow01()
```

Defaults: threat / theo / justice **1/3** each; `wRights = 0` until RightsWarden exists, then a fourth equal share (or inspector weights). Love `physicality01` is soft-clamped to `ConsentWarden.maxPhysicality01`.

`ConsentWardenPlannerService.SoftGate` reads a venue `ConsentWarden` when present and falls back to its own field.

## Route and editor

`RelationshipTravelAgent.ResolvePath` builds Approach → Share Space → Dialog column → optional Intimacy (Consent-gated) → optional License/Vow. Subjects are any `GameObject`s. Missing ragdoll → transform-only placement.

Editor: **Locomotion/Relationship Travel Agent** (hub: Narrative + Civil). Power diamond axes **Affection, Consent, Doctrine, Safety**:

| Layer | Meaning |
|-------|---------|
| Red | Limits (`fireLimit01`, Consent/theocratic/justice/rights/threat caps) |
| Green | Optimal (Romance/Love + step `expected01`) |
| Dashed white | Actual (bio-rhythm + in-paint / live grade), sinusoidal specular |

Missing optional warden → that axis uses **0.5** (neutral). Education diamonds stay blue via the existing `blue01` argument.

## Budget

- `legal_court` rank **35** — Court/Corruption/Constitution/Rights/Law/Government + shared TheocraticWarden, JusticeWarden
- `relationship` rank **36** — LoveWarden, RomanceWarden, ConsentWarden, RelationshipTravelAgent + shared TheocraticWarden, JusticeWarden
