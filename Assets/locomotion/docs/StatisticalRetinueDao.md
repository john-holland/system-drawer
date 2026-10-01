# Statistical Retinue DAO

General pattern for **population-scale statistical maps** with **individual specificity**, shared by gov-glove society snapshots, civil retinues / persona storage, and other proc-gen manifolds (demographics, electorate, trade relationships, economic overlays, chemical / growth rules engines, asteroid belts, life-system baselines).

Related: [LifeSystemsGovGloveMap.md](LifeSystemsGovGloveMap.md), [PersonaDayManager.md](PersonaDayManager.md), [LifeSystemsLemmaGrammar.md](LifeSystemsLemmaGrammar.md), [InventoryLoadouts.md](InventoryLoadouts.md), [weather-liquid-integration.md](../liquid/docs/weather-liquid-integration.md).

---

## Problem

Today the same shape appears in several places, each hand-rolled:

| Surface | Statistical map | Individual specificity |
|---------|-----------------|------------------------|
| Gov-glove / need aspects | `societyFeatures`, `needSatisfied01` | `LifeSystemsGovGloveBias` → `LifeSystemsSheet` per actor |
| Persona day / prison retinue | `PersonaRequestBundle` + venue lattice | `RetinuePeckingEntry` (`personaKey`, role, pecking, actor) |
| Career quotas | `CivilianDemographics` from society features | `TryAcceptUnemployed` / `SampleUnemployed` |
| Voting | `ElectorateDemographics` slices (renormalized whole) | Per-slice tilt; actor choice wins |
| Far LOD | `AsteroidBeltStatisticalManifold` density field | Seeded near-field instances |
| Retail / company | `StoreShelfSlot` catalogs, funding shares | Face trade (`NarrativeTradeAction`), hire/fire on `CompanyRegistration` |
| *(target)* Trade demographics | commodity shares, partner affinity graph | Custom retinue↔retinue trade edges |
| *(target)* Economic overlay | retinue-as-economy ledger / velocity | Venue or company treated as a miniature economy |
| *(target)* Rules engines | chemical / stoichiometric trade rules | Audited real-time science sim on retinue ledgers |
| *(target)* Micro cultures | bacterial / fungal colony maps | Growth events that empower fungus-like systems |

We need one **data-access pattern**: sample and update a statistical field as a map, then pin, override, or materialize specific individuals without breaking the population invariants — including **custom trading relationships** between retinues, an **economic overlay** that projects a retinue as an economy, **rules engines** (e.g. chemical trades) with **audit modes** and **audit correction** for real-time science simulation, and **microbial culture / growth** models (bacteria, fungus-empowering events).

---

## Core idea

A **Statistical Retinue DAO** is a scoped store that:

1. Holds one or more **statistics models** (distributions, feature maps, spatial manifolds).
2. Exposes **map access** (sample / query / integrate over keys or coordinates).
3. Tracks **specificity** (cohort → role → persona → actor instance) with explicit override precedence.
4. Registers **factory**, **predicate**, and **curve** functions for proc-gen and lemma paint.
5. Never confuses **baseline bias** (gentle, healthy-band) with **authored trauma** (lemma / designer only).
6. Optionally projects retinues through an **economic overlay** (ledger, commodities, trade graph) without replacing persona/life models.
7. Plugs in **rules engines** (chemical, growth, custom) that fire on ledger / culture state with explicit **audit modes** and **audit correction** functions for real-time science fudge.
8. Hosts **micro culture** manifolds (bacterial colonies, fungal mycelia) and **growth empowerment events** that boost fungus-like systems without bypassing predicates.

```
Society / seed ──► Model factory ──► Statistical map (DAO)
                                         │
                    sample / query ◄─────┤
                                         │
              predicate / curve adjust ──┤
                                         │
                    materialize ─────────┼──► Retinue / Persona / Instance
                                         │
              trade edge / economy put ──┼──► Trade demographics + economic overlay
                                         │
                 rules engine fire ──────┼──► Chemical / growth rules + audit trail
                                         │
              culture / growth event ────┼──► Micro cultures (bacteria, fungus)
                                         │
                    put / patch (specific)┘
```

---

## Specificity ladder

Higher rows win when resolving a value for an individual. Map defaults fill gaps; they do not clobber pinned individuals.

| Level | Key | Example |
|-------|-----|---------|
| 0 Map | model id + feature / cell | `healthcareCoverage`, angular bin |
| 1 Cohort | city / civil kind / venue | `demo-city` + `Prison` |
| 2 Role | pecking role | `line-chef`, `dispatcher` |
| 3 Persona | `personaKey` | `bus_driver`, `fireman-a` |
| 4 Actor | instance id / `GameObject` | woken retinue member |
| 5 Authored | lemma / designer patch | `{P:stat|…}`, illness ops |

**Rule:** `Get(key, specificity)` walks down until a value exists. `Put` at level ≥ 3 records an individual update; map-level `Put` adjusts the statistical field and may re-reconcile quotas (see electorate `ReconcileChanged`). Trade edges and economic overlays follow the same ladder: city commodity priors → retinue edge → broker persona → actor accept. Rules-engine outcomes and culture biomass use the same ladder; **audit corrections** are specificity-5 authored fudges that must be logged.

---

## Multi-model registry

Each DAO instance binds to a **scope** (`cityId`, `venueStableId`, or global) and a set of named models:

| Model id | Kind | Existing analog |
|----------|------|-----------------|
| `society.features` | sparse float map (camel/snake) | gov-glove snapshot |
| `needs.satisfied` | aspect_id → 0..1 | DreamCycle need vectors |
| `life.baseline` | channel soft-band bias | `LifeSystemsGovGloveBias` |
| `civ.demographics` | quota / age / edu shares | `CivilianDemographics` |
| `vote.electorate` | renormalizing slices | `ElectorateDemographics` |
| `retinue.pecking` | ordered role slots | `RetinuePeckingEntry` list |
| `persona.bundle` | multiplex seed | `PersonaRequestBundle` |
| `space.manifold` | seeded density field | `AsteroidBeltStatisticalManifold` |
| `trade.demographics` | commodity shares + partner affinity | Target: `TradeDemographics` (below) |
| `econ.overlay` | retinue-as-economy projection | Target: `EconomicOverlay` (below) |
| `econ.ledger` | scoped commodity balances / velocity | Store shelves, funding shares, inventory |
| `rules.engine` | pluggable rule sets over ledger/culture | Target: chemical / growth engines (below) |
| `rules.audit` | accuracy mode + correction log | Real-time science sim audit trail |
| `culture.micro` | bacterial colony density / viability | Petri / bioreactor retinue hosts |
| `culture.fungus` | mycelial / fruiting growth map | Fungus empowerment events |
| `culture.cellular` | cell-role demographics + pecking retinue | Tissue / allergy / adhesion hosts |
| `growth.events` | timed / causal growth boosters | Parks horticulture, tree growth agents |

Models implement a thin contract (conceptual):

```csharp
interface IStatisticsModel
{
    string ModelId { get; }
    // Map access
    bool TrySample(in StatQuery q, out float value);
    IEnumerable<StatSample> SampleBatch(in StatQuery q, int count, int seed);
    // Individual specificity
    bool TryGetSpecific(in SpecificityKey key, out StatOverride ov);
    void PutSpecific(in SpecificityKey key, in StatOverride ov);
    // Invariants
    void Reconcile(StatReconcileMode mode);
}
```

`StatQuery` carries coordinates / feature keys / civil kind / seed. `SpecificityKey` is the ladder above.

---

## Factories

Factories construct models (and optionally retinues) from seeds. Prefer pure static/factory methods already used in-tree:

| Factory | Input | Output |
|---------|-------|--------|
| `PersonaRequestBundle.CreateDefault` | personaKey, civilKind | bundle shell |
| `CivilianDemographics.FromSocietyFeatures` | society map, population | quotas |
| `ElectorateDemographics.FromSocietyFeatures` | society map | slices |
| `ElectorateDemographics.DefaultTwoParty` | — | base slices |
| `PrisonScheduleFactory` / shift catalogs | status / cron | schedule slots |
| model registry `Create(modelId, seed)` | model id + `StatSeed` | `IStatisticsModel` |
| `TradeDemographics.FromSocietyFeatures` | society map + civil kinds | commodity / partner priors |
| `TradeDemographics.BetweenRetinues` | two retinue ids + edge params | custom trade relationship |
| `EconomicOverlay.FromRetinue` | retinue + company/store seed | economy projection |
| `EconomicOverlay.FromCompany` | `CompanyRegistration` + shelves | funded economy shell |
| `RulesEngineFactory.Create` | engine id + rule pack seed | chemical / growth / custom engine |
| `ChemicalTradeRules.FromStoichiometry` | reaction table + commodities | chemical trade rule pack |
| `MicroCulture.CreateBacterial` | species id + media + seed | bacterial culture model |
| `MicroCulture.CreateFungal` | species id + substrate + seed | fungal culture model |
| `GrowthEvent.EmpowerFungus` | event params + culture ids | fungus growth empowerment |

**Registry API (target):**

```csharp
public static class StatisticsModelFactory
{
    public static IStatisticsModel Create(string modelId, in StatSeed seed);
    public static void Register(string modelId, Func<StatSeed, IStatisticsModel> ctor);
}
```

`StatSeed` should include: `cityId`, `venueStableId`, `personaKey`, `civilKind`, `societyFeatures`, `needSatisfied01`, `rngSeed`, optional spatial frame, optional `companyId` / `retinueId` / commodity catalog keys, optional `rulePackId` / `auditMode` / culture species ids.

---

## Predicates and curve adjustments

Lemma and civil systems need **named** transform functions, not ad-hoc lambdas buried in callers.

### Predicates (`StatPredicate`)

Boolean gates before sample / materialize / wake:

| Id | Meaning | Use |
|----|---------|-----|
| `within_quota` | next individual fits demographics | career unemployed |
| `causal_depth` | 4D sample ≥ min | PersonaDay wake |
| `cron_active` | hours cron due | venue open / shift |
| `lod_allows` | FeatureBudget + speed LOD | FullSim vs Ghost |
| `actor_not_pinned` | no level-4/5 override | tilt yes/no without clobber |
| `trade_edge_allows` | directed edge exists + open window | retinue↔retinue trade |
| `within_trade_quota` | next deal fits partner quotas | wholesale / standing order |
| `ledger_solvent` | payer can cover offer value | face trade accept |
| `economy_active` | overlay open + cron / LOD | retinue-as-economy tick |
| `rule_preconditions` | rule LHS reagents / state ok | chemical trade fire |
| `audit_accuracy_ok` | last step within accuracy band | science sim gate |
| `culture_viable` | media + temp + pH in band | bacterial / fungal tick |
| `growth_event_armed` | empowerment event window open | fungus boost / spore burst |

```csharp
delegate bool StatPredicate(in StatContext ctx);
// Registry: StatPredicateCatalog.Get("within_quota")
```

### Curves (`StatCurve`)

Map a 0..1 (or unbounded) input through a named response, then clamp to model soft bands:

| Id | Shape | Existing use |
|----|-------|--------------|
| `soft_band_lerp` | lerp softMin..softMax about setpoint | gov-glove channel bias |
| `nudge_clamped` | add delta, clamp soft band | unemployment → morale |
| `speed_log_lod` | `1/(1+log(1+v/vmax))` | PersonaDay speed LOD |
| `biorhythm_amp` | `0.35*needMean + 0.65*healthcare` | persona_day_routes |
| `renormalize_slices` | shares sum to 1 | electorate |
| `hash_density` | seeded bin hash ± variance | asteroid manifold |
| `price_soft_band` | base × demand/supply tilt, clamp | shelf / wholesale price |
| `trade_affinity_lerp` | partner affinity → accept bias | custom trading relationships |
| `velocity_decay` | ledger velocity → 0 over hours | economic overlay quieting |
| `funding_share_renorm` | funding sources sum to 1 | `CompanyFundingSource` |
| `stoich_yield` | limiting reagent to product qty | chemical trade |
| `audit_blend` | trueResult vs fudgedResult by mode | audit correction |
| `monod_growth` | mu = mumax * S/(Ks+S) | bacterial culture |
| `logistic_biomass` | dN/dt = rN(1-N/K) | colony / mycelium |
| `fungus_empower` | event strength to growth rate mult | fungus empowerment |
| `arrhenius_rate` | temp to reaction / metabolism rate | science + culture |

```csharp
delegate float StatCurve(float t, in StatCurveParams p);
// Registry: StatCurveCatalog.Get("soft_band_lerp")
```

### Mapping table for lemma integration

Lemma paint should address **catalog ids**, not C# types:

```text
{P:stat|op=sample|model=society.features|key=healthcareCoverage}
{P:stat|op=adjust|model=life.baseline|channel=morale|curve=nudge_clamped|delta=-0.08}
{P:stat|op=put|model=retinue.pecking|persona=fireman-a|role=firefighter|pecking=12}
{P:stat|op=materialize|model=civ.demographics|predicate=within_quota|count=1|seed=42}
{P:stat|op=curve|id=biorhythm_amp|needMean=0.6|healthcare=0.7}
{P:stat|op=when|predicate=cron_active|then=wake}
{P:stat|op=put|model=trade.demographics|from=kitchen.retinue|to=sanitation.retinue|commodity=food_waste|share=0.2}
{P:stat|op=sample|model=econ.overlay|retinue=gas_station|key=velocity}
{P:stat|op=trade|predicate=trade_edge_allows|from=store.staff|to=player|item=radio|curve=price_soft_band}
{P:stat|op=overlay|model=econ.overlay|retinue=restaurant|enable=1}
{P:stat|op=rule|engine=chemical.trade|reaction=acid_base_neutralize|audit=strict}
{P:stat|op=audit|mode=playable|correct=audit_blend|epsilon=0.05}
{P:stat|op=culture|model=culture.micro|species=e_coli|op2=inoculate|media=lb}
{P:stat|op=culture|model=culture.fungus|species=oyster|op2=empower|event=spore_rain|mult=1.8}
{P:stat|op=growth|event=fungus_bloom|predicate=growth_event_armed|curve=fungus_empower}
```

Resolver sketch (parallel to `LifeSystemsLemmaResolver`):

| Op | DAO call |
|----|----------|
| `sample` / `query` | `TrySample` / batch |
| `adjust` | apply named `StatCurve`, then soft-clamp |
| `put` / `patch` | `PutSpecific` at persona/actor level |
| `materialize` | factory + predicates → retinue / paper doll |
| `reconcile` | `Reconcile` (quotas / slice whole) |
| `when` | evaluate `StatPredicate`, optional nested op |
| `trade` | resolve edge + transfer via inventory / ledger |
| `overlay` | enable / sample / tick economic overlay on a retinue |
| `rule` | fire named rules engine step (chemical / growth / custom) |
| `audit` | set audit mode / apply correction function / query trail |
| `culture` | inoculate / tick / harvest micro culture |
| `growth` | arm or fire growth empowerment event |

**Hard rule (unchanged):** `life.baseline` and gov-glove paths never apply illness or organ trauma. Authored illness stays on `{P:life|op=illness|…}` only.

---

## DAO surface

Suggested type name: `StatisticalRetinueDao` (scope-owned MonoBehaviour or plain service).

```csharp
public interface IStatisticalRetinueDao
{
    string ScopeId { get; }  // city / venue / global

    IStatisticsModel GetModel(string modelId);
    IStatisticsModel EnsureModel(string modelId, in StatSeed seed);

    // Map access
    bool TryGet(string modelId, in StatQuery q, out float value);
    float Sample(string modelId, in StatQuery q, int seed);
    IReadOnlyList<StatSample> SampleMap(string modelId, in StatQuery q, int count, int seed);

    // Individual updates
    void PutIndividual(string modelId, in SpecificityKey key, in StatOverride value);
    bool TryGetIndividual(string modelId, in SpecificityKey key, out StatOverride value);
    void ClearIndividual(string modelId, in SpecificityKey key);

    // Retinue bridge
    PersonaRequestBundle GetOrRequestBundle(string personaKey, CivilSystemKind kind);
    IReadOnlyList<RetinuePeckingEntry> GetRetinue(CivilVenueNode venue);
    RetinuePeckingEntry UpsertPecking(CivilVenueNode venue, RetinuePeckingEntry entry);
    void ApplyGovGloveBias(LifeSystemsSheet sheet, PersonaRequestBundle bundle);

    // Trade + economy
    TradeDemographics GetTradeDemographics();
    TradeEdge UpsertTradeEdge(TradeEdge edge);
    bool TryResolveTrade(in TradeDealRequest req, out TradeDealResult result);
    EconomicOverlay EnsureEconomicOverlay(string retinueId, in StatSeed seed);
    void TickEconomy(string retinueId, float dtHours);

    // Rules engines + audit
    IRulesEngine EnsureRulesEngine(string engineId, in StatSeed seed);
    bool TryFireRule(string engineId, string ruleId, in RuleFireContext ctx, out RuleFireResult result);
    AuditMode GetAuditMode(string engineId);
    void SetAuditMode(string engineId, AuditMode mode);
    float ApplyAuditCorrection(string engineId, string correctionId, float measured, float expected, in AuditCorrectionParams p);
    IReadOnlyList<AuditEntry> GetAuditTrail(string engineId, int maxEntries = 64);

    // Micro cultures + growth
    MicroCulture EnsureCulture(string cultureId, CultureKind kind, in StatSeed seed);
    void TickCulture(string cultureId, float dtHours);
    GrowthEvent ArmGrowthEvent(GrowthEvent ev);
    bool TryFireGrowthEvent(string eventId, out GrowthEventResult result);

    // Catalogs
    StatPredicate Predicate(string id);
    StatCurve Curve(string id);
}
```

### Persona / gov-glove bridge

Existing call path becomes a thin DAO method:

```csharp
// today
LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, bundle.societyFeatures, bundle.needSatisfied01);

// via DAO
dao.ApplyGovGloveBias(sheet, bundle);
// ≡ EnsureModel("life.baseline") + soft_band_lerp / nudge_clamped curves
```

Bundle fetch (`/api/persona-day/request`, prison retinue sync) fills `persona.bundle` + `society.features` + `needs.satisfied` models for the scope, then materializes or biases the woken retinue.

### Map vs individual update examples

**Statistical map update** (population):

```csharp
dao.EnsureModel("civ.demographics", seed);
dao.GetModel("civ.demographics").PutFeature("unemploymentRate", 0.11f);
dao.GetModel("civ.demographics").Reconcile(StatReconcileMode.Quotas);
```

**Specific individual update** (specificity ≥ 3):

```csharp
dao.PutIndividual("retinue.pecking",
    SpecificityKey.Persona("fireman-a"),
    StatOverride.Role("firefighter", pecking: 12));
dao.PutIndividual("life.baseline",
    SpecificityKey.Actor(actorId),
    StatOverride.Channel("morale", 0.72f)); // pinned; map bias skips or blends lightly
```

---

## Materialize pipeline

Shared wake / spawn path for civil and other retinues:

1. Resolve scope + models from seed / API bundle.
2. Evaluate predicates (`causal_depth`, `cron_active`, `lod_allows`, `within_quota`).
3. Sample map (demographics / electorate / density / pecking template).
4. Factory → candidate individual (`CivilianPaperDoll`, `RetinuePeckingEntry`, near-field asteroid, …).
5. Apply curves for biorhythm / life baseline (healthy band only).
6. `PutSpecific` if the candidate is pinned by designer or lemma.
7. Attach to venue retinue / spatial wake source; tick bio-rhythm.

PersonaDayManager steps 1–7 in [PersonaDayManager.md](PersonaDayManager.md) are this pipeline with civil LOD + travel side effects.

---

## Trade demographics

**Trade demographics** are a statistics model (`trade.demographics`) that describe *who trades what with whom* at the retinue / company / cohort level — analogous to `CivilianDemographics` quotas and `ElectorateDemographics` slices, but for **commodity flow and partner affinity**.

### Why

Face trade (`NarrativeTradeAction` / `TradePanel`) and retail shelves (`StoreBase`) already move items. They do not yet encode standing **relationships between retinues** (kitchen → sanitation food waste, gas station ↔ fuel authority, factory → rail bay). Trade demographics hold that graph as a map with optional per-edge specificity.

### Data shape

```csharp
[Serializable]
public sealed class TradeCommodityShare
{
    public string commodityKey;          // e.g. food, gas, wood, radio
    [Range(0f, 1f)] public float produceShare01 = 0f;
    [Range(0f, 1f)] public float consumeShare01 = 0f;
    public float softPrice = 1f;         // curve: price_soft_band
}

[Serializable]
public sealed class TradeEdge
{
    public string edgeId;
    public string fromRetinueId;         // venue / company / civil retinue stable id
    public string toRetinueId;
    public string commodityKey;
    [Range(0f, 1f)] public float affinity01 = 0.5f;
    [Range(0f, 1f)] public float volumeShare01 = 0.1f;  // of fromRetinue produce for this commodity
    public float unitPrice = -1f;        // <0 → derive via price_soft_band
    [CronExpr] public string openCron = "* * * * *";
    public bool bidirectional;
    public TradeSettlementMode settlement = TradeSettlementMode.BarterOrLedger;
    // Specificity: pin personas who may broker this edge
    public string brokerPersonaKey;
    public int minPeckingOrder = 100;
}

public enum TradeSettlementMode
{
    BarterOnly,          // NarrativeTradeAction item swap
    LedgerOnly,          // econ.ledger balances
    BarterOrLedger,      // prefer barter; fall back to ledger
    ContractStanding     // recurring; reconcile on tick
}

[Serializable]
public sealed class TradeDemographics
{
    public string scopeId;               // city or venue
    public List<TradeCommodityShare> commodities = new List<TradeCommodityShare>();
    public List<TradeEdge> edges = new List<TradeEdge>();
    [Range(0f, 1f)] public float slack01 = 0.08f;  // quota softness like CivilianDemographics
}
```

### Map vs individual

| Level | Trade meaning |
|-------|----------------|
| 0 Map | city commodity produce/consume shares; default affinity priors from society features (`commercial_activity`, `taxRate`, …) |
| 1 Cohort | civil kind templates (Kitchen produces food, Sanitation consumes waste, GasStation produces fuel access) |
| 2 Role | broker roles (`store_clerk`, `dispatcher`, `purchasing`) |
| 3 Persona | named broker / standing partner (`personaKey`) |
| 4 Actor | woken staff who may accept face trade |
| 5 Authored | lemma `{P:stat|op=put|model=trade.demographics|…}` or designer edge |

### Factories and reconcile

| API | Behavior |
|-----|----------|
| `FromSocietyFeatures(features, catalog)` | seed commodity shares from gov-glove commercial / welfare / tax signals |
| `BetweenRetinues(fromId, toId, commodity, volume, affinity)` | upsert a custom edge |
| `SamplePartner(fromId, commodity, seed)` | pick a `toRetinueId` weighted by affinity × volume |
| `TryAcceptDeal(edge, deal)` | `within_trade_quota` + `ledger_solvent` + `trade_edge_allows` |
| `Reconcile` | renormalize produce/consume shares per commodity; clamp edge `volumeShare01` into slack |

Custom relationships are first-class: designers and Continuuuum can author edges that **override** cohort defaults without rewriting shelf catalogs.

### Bridge to existing trade UI

1. Predicate `trade_edge_allows` gates `NarrativeTradeAction` accept when both parties resolve to retinue ids on an open edge.
2. Offer lists may be prefilled from edge commodity + `StoreShelfSlot` / `ActorInventory`.
3. On accept: transfer items (`InventoryManager`) and/or post ledger deltas (`econ.ledger`); standing contracts tick via `ContractStanding`.

---

## Economic overlay

An **economic overlay** (`econ.overlay`) is a projection that treats a **retinue (or company staff list) as an economy**: balances, velocity, open/close, and participation in the trade graph — without replacing persona, life-systems, or pecking models.

```
Retinue / CompanyRegistration / StoreBase
        │
        ▼
 EconomicOverlay.FromRetinue / FromCompany
        │
        ├── econ.ledger   (commodity balances, credits)
        ├── trade.demographics edges (this retinue as node)
        ├── funding shares (CompanyFundingSource → funding_share_renorm)
        └── tick: velocity_decay + standing contracts
```

### Data shape

```csharp
[Serializable]
public sealed class EconomicOverlay
{
    public string retinueId;
    public string companyId;             // optional; from CompanyRegistration
    public bool enabled;
    [CronExpr] public string marketHoursCron = "* 8-20 * * *";
    [Range(0f, 1f)] public float liquidity01 = 0.5f;
    [Range(0f, 1f)] public float velocity01;     // recent trade intensity
    public List<LedgerBalance> balances = new List<LedgerBalance>();
    public List<string> exportCommodityKeys = new List<string>();
    public List<string> importCommodityKeys = new List<string>();
    public float priceIndex = 1f;                 // multiplies price_soft_band
}

[Serializable]
public sealed class LedgerBalance
{
    public string commodityKey;          // or "credit" for abstract purchasing power
    public float quantity;
    public float reserved;               // standing contract hold
}
```

### Overlay rules

1. **Opt-in:** retinues are people/roles first; `EnsureEconomicOverlay` enables economy semantics.
2. **LOD:** `economy_active` requires overlay enabled + market cron + civil LOD allowing Proxy or better (Ghost may sample map only).
3. **Funding:** `CompanyFundingSource.share01` feeds starting `credit` / liquidity via `funding_share_renorm`.
4. **Shelves as ledger mirror:** `StoreBase.shelves` can sync into `econ.ledger` on tick (map) while individual shelf pins stay specificity ≥ 3.
5. **Retinue-as-node:** all `TradeEdge` endpoints that equal `retinueId` participate when overlay is active.
6. **Persona brokers:** pecking + `brokerPersonaKey` choose who speaks in `NarrativeTradeAction`; economy does not auto-accept without predicate.
7. **No life trauma:** economic stress may nudge morale via named curves only inside soft bands — never illness.

### Tick

```csharp
void TickEconomy(string retinueId, float dtHours)
{
    // 1. velocity_decay on overlay.velocity01
    // 2. settle ContractStanding edges (volume × dt)
    // 3. sync StoreBase shelf quantities ↔ ledger (optional)
    // 4. Reconcile trade.demographics quotas if map put occurred
}
```

PersonaDayManager (or a sibling `EconomicDayService`) may call `TickEconomy` for woke venues after bio-rhythm.

### Example: kitchen ↔ sanitation

```csharp
var trade = TradeDemographics.FromSocietyFeatures(features, catalog);
trade.BetweenRetinues("venue.kitchen", "venue.sanitation",
    commodity: "food_waste", volumeShare01: 0.25f, affinity01: 0.8f);
dao.UpsertTradeEdge(...);

var kitchenEcon = dao.EnsureEconomicOverlay("venue.kitchen", seed);
kitchenEcon.exportCommodityKeys.Add("food_waste");
var sanEcon = dao.EnsureEconomicOverlay("venue.sanitation", seed);
sanEcon.importCommodityKeys.Add("food_waste");
```

Standing contract ticks move waste volume on the ledger; face trade remains available for one-off barter when staff are woken.

---

## Rules engines (chemical trades and beyond)

A **rules engine** (`rules.engine`) is a pluggable pack of typed rules that read/write DAO models (ledger, cultures, trade edges) when preconditions hold. Chemical trades are the first-class science case: stoichiometric transfers between retinue economies with measurable accuracy.

```
econ.ledger / culture.*  -->  IRulesEngine.TryFire
                                    |
                         rule_preconditions?
                                    |
                         apply stoich / growth curves
                                    |
                         AuditMode gate + optional correction
                                    |
                         commit --> ledger / culture + AuditEntry
```

### Engine contract

```csharp
public interface IRulesEngine
{
    string EngineId { get; }                 // e.g. chemical.trade, growth.culture
    AuditMode AuditMode { get; set; }
    IReadOnlyList<IStatRule> Rules { get; }
    bool TryFire(string ruleId, in RuleFireContext ctx, out RuleFireResult result);
    void RegisterCorrection(string correctionId, AuditCorrectionFn fn);
}

public interface IStatRule
{
    string RuleId { get; }
    string[] ReadsModels { get; }            // econ.ledger, culture.micro, ...
    string[] WritesModels { get; }
    bool Preconditions(in RuleFireContext ctx);  // -> rule_preconditions catalog wrapper
    RuleFireResult Evaluate(in RuleFireContext ctx); // pure; no commit
    void Commit(in RuleFireResult result, IStatisticalRetinueDao dao);
}

public delegate float AuditCorrectionFn(
    float measured, float expected, in AuditCorrectionParams p);
```

### Chemical trade rule pack

Chemical trades are ledger moves constrained by a reaction table — not free-form barter. They can settle across retinue economic overlays (lab A reagents -> lab B products) under the same trade-edge predicates when desired.

```csharp
[Serializable]
public sealed class ChemicalSpeciesRef
{
    public string commodityKey;              // must exist on econ.ledger / trade.demographics
    public float molesPerUnit = 1f;
}

[Serializable]
public sealed class ChemicalReactionRule
{
    public string RuleId;                    // e.g. acid_base_neutralize
    public List<ChemicalSpeciesRef> reactants;
    public List<ChemicalSpeciesRef> products;
    public float rateConstant = 1f;          // scaled by arrhenius_rate(temp)
    public float enthalpyHint;               // optional; for audit expected heat
    public string fromRetinueId;             // reagent source economy
    public string toRetinueId;               // product sink economy (may equal source)
}

[Serializable]
public sealed class ChemicalTradeRules
{
    public string packId = "chemical.trade";
    public List<ChemicalReactionRule> reactions = new List<ChemicalReactionRule>();
    public float defaultTempK = 298.15f;

    public static ChemicalTradeRules FromStoichiometry(
        IEnumerable<ChemicalReactionRule> table, in StatSeed seed) { /* ... */ }
}
```

**Fire path:**

1. Resolve retinue overlays (`economy_active`) and ledger balances for reactants.
2. `rule_preconditions` — limiting reagent available; optional catalyst present; cron / LOD ok.
3. `Evaluate` — `stoich_yield` on limiting reagent to product quantities; rate via `arrhenius_rate`.
4. **Audit** — compare measured (game tick qty) vs expected (stoich ideal); see Audit modes.
5. `Commit` — debit reactants / credit products on `econ.ledger`; optional trade edge volume bump; append `AuditEntry`.

Other packs (sanitation chemistry, canal fuel/steam commodities, recipe specials) register the same way via `RulesEngineFactory.Create`.

---

## Audit modes and correction functions

Real-time science simulation needs both **accuracy pressure** and intentional **fudge** so retinues stay playable. Audit is a first-class model (`rules.audit`), not a silent lerp.

### Audit modes

| Mode | Behavior | Use |
|------|----------|-----|
| `Strict` | Commit only if abs(measured - expected) <= epsilon; else reject + log | Lab accuracy, exams, causality lockstep |
| `ReportOnly` | Always commit physics/game result; log discrepancy | Telemetry / Continuuuum audit |
| `Playable` | Commit after `audit_blend` toward expected by `fudge01` | Real-time science that must feel right |
| `AuthoritativeExpected` | Commit expected; log measured as counterfactual | Scripted demos / DreamDay steering |
| `FrozenTrail` | No commit; evaluate + log only | Scrub / replay |

```csharp
public enum AuditMode
{
    Strict,
    ReportOnly,
    Playable,
    AuthoritativeExpected,
    FrozenTrail
}

[Serializable]
public sealed class AuditCorrectionParams
{
    public float epsilon = 0.05f;            // Strict band
    [Range(0f, 1f)] public float fudge01 = 0.35f;  // Playable blend weight toward expected
    public string correctionId = "audit_blend";
    public bool recordCounterfactual = true;
}

[Serializable]
public sealed class AuditEntry
{
    public double utcTicks;
    public string engineId;
    public string ruleId;
    public AuditMode mode;
    public float measured;
    public float expected;
    public float committed;
    public string correctionId;
    public string retinueId;
    public string notes;
}
```

### Audit correction functions

Named corrections live beside `StatCurve` catalogs (lemma-addressable):

| Correction id | Meaning |
|---------------|---------|
| `audit_blend` | lerp(measured, expected, fudge01) then soft-clamp |
| `audit_snap_expected` | replace with expected (AuthoritativeExpected helper) |
| `audit_snap_measured` | keep measured (ReportOnly helper) |
| `audit_reject_band` | fail if outside epsilon (Strict) |
| `audit_retinue_fudge` | blend using retinue-specific fudge from persona/econ overlay |
| `audit_science_rt` | time-aware: stronger fudge when dt large / LOD Proxy; weaker on FullSim |

```csharp
// DAO
float committed = dao.ApplyAuditCorrection(
    "chemical.trade", "audit_science_rt", measured, expected,
    new AuditCorrectionParams { fudge01 = 0.4f, epsilon = 0.05f });
```

**Real-time science sim rule:** FullSim + `Strict` or low-`fudge01` `Playable` for instruments the player can read; Proxy/Ghost may raise fudge via `audit_science_rt` so city-scale retinues stay cheap without lying in the audit trail — every fudge writes `AuditEntry` (measured, expected, committed).

**Hard rule:** audit corrections never write `life.baseline` illness; they only adjust ledger / culture / rule outputs. Authored life trauma stays `{P:life|op=illness|...}` only.

---

## Micro cultures and growth empowerment

Micro cultures are statistical maps of **living colonies** hosted by a retinue, vessel, or park plot. They participate in rules engines (metabolism as chemical trades) and economic overlays (biomass as commodity).

### Culture kinds

| Kind | Model id | Growth curve | Notes |
|------|----------|--------------|-------|
| Bacterial | `culture.micro` | `monod_growth` / `logistic_biomass` | Media substrate, lag/log/stationary |
| Fungal | `culture.fungus` | `logistic_biomass` x `fungus_empower` | Mycelium density + fruiting body events |
| Generic biofilm | `culture.micro` (tag) | logistic + moisture | Canal / sanitation hooks |

```csharp
public enum CultureKind { Bacterial, Fungal, Biofilm }

[Serializable]
public sealed class MicroCulture
{
    public string cultureId;
    public CultureKind kind;
    public string speciesId;                 // e.g. e_coli, oyster, yeast
    public string hostRetinueId;             // lab, kitchen, park horticulture, ...
    public string mediaCommodityKey;         // ledger substrate / sugar / wood chip
    public float biomass01;
    public float viability01 = 1f;
    public float tempK = 310f;
    public float ph01 = 0.5f;
    public float carryingCapacity01 = 1f;
    public float growthRate = 0.2f;          // base r or mumax
    public bool sealedVessel = true;
    public List<string> metaboliteCommodityKeys = new List<string>();
}

[Serializable]
public sealed class GrowthEvent
{
    public string eventId;
    public string displayName;
    public GrowthEventKind kind;
    public string[] targetCultureIds;        // empty -> all fungus in scope
    public string[] targetSpeciesIds;        // e.g. oyster, shiitake
    [Range(0f, 4f)] public float empowerMult = 1.5f;
    public float durationHours = 2f;
    [CronExpr] public string windowCron;
    public float minCausalDepth;             // optional NarrativeVolumeQuery gate
    public bool sporeBurst;                  // spreads to neighbor hosts
    public string lemmaHint;                 // e.g. growth.fungus_bloom
}

public enum GrowthEventKind
{
    EmpowerFungus,       // multiply fungal growth rate
    SporeRain,           // inoculate nearby hosts
    NutrientPulse,       // temporary media bump on ledger
    HeatShock,           // viability dip + selective survive
    BacterialBloom,      // empower culture.micro
    Custom
}
```

### Tick

```csharp
void TickCulture(string cultureId, float dtHours)
{
    // 1. culture_viable? (media, temp via arrhenius_rate, pH band)
    // 2. Consume media from econ.ledger (host retinue); fail soft if insolvent
    // 3. Biomass: monod_growth or logistic_biomass; fungus applies armed empowerMult
    // 4. Emit metabolites onto ledger / optional chemical.trade auto-rules
    // 5. If sporeBurst event active -> SampleMap neighbor hosts and inoculate
}
```

### Empowering fungus (and friends)

Growth systems like fungus are empowered through **events**, not silent globals:

1. `ArmGrowthEvent` — schedules window (`growth_event_armed`).
2. `TryFireGrowthEvent` — applies `fungus_empower` (or bacterial equivalent) to matching cultures.
3. Optional `SporeRain` — materializes new `MicroCulture` on neighbor retinues / park plots (horticulture nodes, tree growth agents).
4. Lemma / narrative can fire the same ids: `{P:stat|op=growth|event=fungus_bloom|...}`.

**Bridge to existing growth:** park horticulture BT nodes and `TreeGrowthTravelAgent` may publish `GrowthEvent`s into the DAO instead of only mutating local `growth01`, so fungal and plant growth share one empowerment bus.

### Example: lab chemical trade + oyster empower

```csharp
var chem = dao.EnsureRulesEngine("chemical.trade", seed);
dao.SetAuditMode("chemical.trade", AuditMode.Playable);
dao.TryFireRule("chemical.trade", "esterification", ctx, out var fired);
// audit_science_rt blends measured yield toward stoich expected; AuditEntry recorded

var oyster = dao.EnsureCulture("bed.oyster-1", CultureKind.Fungal, seed);
dao.ArmGrowthEvent(new GrowthEvent
{
    eventId = "fungus_bloom",
    kind = GrowthEventKind.EmpowerFungus,
    targetSpeciesIds = new[] { "oyster" },
    empowerMult = 1.8f,
    durationHours = 3f,
    sporeBurst = true
});
dao.TryFireGrowthEvent("fungus_bloom", out _);
dao.TickCulture("bed.oyster-1", dtHours: 0.25f);
```

---

## Continuuuum / storage

| Layer | Responsibility |
|-------|----------------|
| Continuuuum API | Authoritative society features, need vectors, retinue rows (`persona_day`, prison retinue) |
| DAO (runtime) | Cache models per scope; map sample; individual overrides for the session |
| Persona storage | `PersonaRequestBundle` + venue `lastBundle`; pecking lists on `CivilVenueNode` / `CompanyRegistration` |
| Trade / economy | Optional Continuuuum tables for edges + ledger snapshots; runtime DAO caches overlays per retinue |
| Rules / audit | Rule packs + `AuditEntry` trail (align with Continuuuum `api_audit_log` / causality audit patterns) |
| Cultures / growth | Culture snapshots + armed growth events; optional research_suggestions hook |
| Lemma | Named ops against catalog ids; no direct society DB writes from paint except via DAO `put` / `trade` / `overlay` / `rule` / `audit` / `culture` / `growth` |

Persist only level ≥ 3 overrides the designer intends to keep; map-level regenerates from society snapshots on request/sync/merge.

---

## Runtime (implemented)

Code: `Assets/locomotion/pathing/stat/` in **Locomotion.Runtime**.

| Type | Role |
|------|------|
| `StatisticalRetinueDao` | Scoped MonoBehaviour DAO |
| `StatisticsModelFactory` / catalogs | Model, predicate, curve registries |
| Adapters | Wrap society/needs/life/civ/vote/persona/pecking/trade/econ/space/culture |
| `StatLemmaResolver` | `{P:stat\|…}` — also `NsmPrimeLemmaResolver.ExecuteStat` |
| `GrowthEventBus` | Park horticulture / `TreeGrowthTravelAgent.PublishGrowthEmpower` |

### Lemma grammar (`{P:stat|…}`)

```text
{P:stat|op=sample|model=society.features|key=healthcareCoverage}
{P:stat|op=put|model=trade.demographics|from=kitchen|to=sanitation|commodity=food_waste|share=0.2}
{P:stat|op=rule|engine=chemical.trade|reaction=acid_base_neutralize|audit=playable}
{P:stat|op=culture|model=culture.fungus|species=oyster|event=fungus_bloom|mult=1.8}
{P:stat|op=growth|event=fungus_bloom|mult=1.5}
```

Ops: `sample`/`query`, `adjust`, `put`/`patch`, `trade`, `overlay`, `rule`, `audit`, `culture`, `growth`, `curve`, `reconcile`, `when`, `materialize`, `health_inpaint`, `health_inpaint_event`.

---

## Implementation sketch (phased)

1. **Catalogs** — `StatisticsModelFactory`, `StatPredicateCatalog`, `StatCurveCatalog` with ids from the tables above; wrap existing static helpers.
2. **DAO** — `StatisticalRetinueDao` over city/venue scope; wire `ApplyGovGloveBias` + bundle cache.
3. **Trade demographics** — `TradeDemographics` / `TradeEdge` model; factories; reconcile; bridge predicates into `NarrativeTradeAction`.
4. **Economic overlay** — `EconomicOverlay` + `econ.ledger`; `FromRetinue` / `FromCompany`; day tick beside PersonaDay.
5. **Rules engines** — `IRulesEngine` / chemical trade pack; audit modes + correction catalog; trail persistence.
6. **Micro cultures** — bacterial / fungal models; growth events (fungus empower, spore rain); tick beside economy.
7. **Migrate callers** — `PersonaDayManager`, `PrisonRetinueClient`, `CareerWarden`, voting / electorate, `StoreBase` / `CompanyRegistration`, park horticulture / tree growth, optional asteroid host.
8. **Lemma** — `{P:stat|…}` resolver; ops including `trade` / `overlay` / `rule` / `audit` / `culture` / `growth`.
9. **Tests** — quota accept/reject, electorate reconcile, trade edge gate, overlay velocity decay, chemical stoich + Strict reject, Playable fudge logged, culture monod tick, fungus empower mult, baseline never leaves soft band, persona pin beats map sample.

---

## Design invariants

- **Multi-model:** one DAO, many models; never hardcode a single feature dictionary as the only store.
- **Map first, individuals second:** proc-gen samples the map; specificity patches individuals.
- **Factories are seed-pure:** same `StatSeed` → same model defaults (rng stream explicit).
- **Predicates / curves are named:** lemma and Continuuuum reference string ids.
- **Gov-glove is baseline only:** soft healthy bands; illness stays authored.
- **Reconcile after map edits:** quotas and slice wholes stay valid after statistical puts.
- **Actor choice wins:** electorate tilt and similar curves respect `actor_not_pinned`.
- **Trade is relational:** custom edges between retinues are map data with persona brokers; not only face-to-face UI.
- **Economy is an overlay:** retinues stay retinues; `econ.overlay` opts them into ledger / velocity / market hours.
- **Settlement is explicit:** barter, ledger, or standing contract — never silent inventory drains outside predicates.
- **Rules are pluggable:** chemical and growth packs share `IRulesEngine`; no one-off chemistry in trade UI.
- **Audit is visible:** every fudge records measured / expected / committed; Strict can refuse commit.
- **Corrections are named:** real-time science uses catalogued audit correction functions, not hidden lerps.
- **Cultures are maps:** colony biomass is statistical with host specificity; harvest / inoculate are puts.
- **Growth empowerment is evented:** fungus (and bacterial bloom) boosts arm/fire through `GrowthEvent`, not silent globals.

---

## Spatial demographics (civ)

Optional `DemographicSpatialGradient` on `CivilianDemographics`:

- Anchors + `spatial_falloff` = `1/(1+(d/radius)^2)` modulate unemployment prior and age/edu shares, then renormalize.
- **City quotas stay on baseline** `unemploymentRate01` (not spatially inflated).
- Optional `DemographicSpatialSampleBound` AABBs: `SamplePosForDemographics(spawnPos, seed)` randomizes **lookup** only; spawn/bind/place keep `spawnPos`.
- `CareerWarden.RequestCivilianPaperDoll(..., spawnPos)` and `CivDemographicsModel.TrySample` use sample pos when gradient is active.
- `SpatialRetinueWakeSource` sorts ingest by `WeightAt(samplePos)`; actors are not moved.

## Cellular retinues

- `CellularRetinue` / `CellularDemographics` / `CellularPeckingEntry` under `culture.cellular`.
- Reuses the same spatial gradient + sample bounds; cellular anchor deltas (`allergyPrevalenceDelta`, `glueReadinessDelta`, role shares).
- `EnsureCellularRetinue` / `SampleCellularMember` on the DAO; may link `MicroCulture` via `cultureId`.

## Allergy / glue trades

- `TradeRelationKind`: Commodity, AllergyAvoid, AllergySensitize, ChemicalBond, ChemicalRelease.
- AllergyAvoid denies `TryResolveTrade`; predicates `allergy_blocks` / `bond_ready`.
- Chemical pack adds `allergy_crosslink`, `glue_cure`, `glue_solvent_release`, `tongue_freeze_bond`.

## PhysicsManifold lerp selectors

- `PhysicsManifoldLerpGradientSelector` lerps `ManifoldCellData` channels for selection weights and `EvaluateAdhesionGate`.
- `ManifoldContactRetinueHook.TryContact` → tongue/flag-pole bond + health inpaint event `tongue_stuck_flagpole`.
- Curve `manifold_temp_lerp`; predicates `manifold_cold_stick` / `manifold_bond_ready`.

## Actor health inpaint + LSTM events

- `ActorHealthInpaintBridge` + `ActorHealthInpaintSettings.disablePartSpecificHealthInpainting`.
- Part-specific when part known and not disabled; else whole-body.
- `HealthInpaintEventCatalog` / `HealthInpaintEventRunner` fill templates → `NarrativeLSTMPromptInterpreter.Interpret` → bridge (optional calendar).
- Defaults: `hives_from_toad`, `infected_sports_scrape`, `glue_residue`, `tongue_stuck_flagpole`.
- Wizard Standard Assets (Narrative Prompt) installs catalog under `StandardAssets/Stat/` and wires a `_StandardScene` runner.

Lemma extras:

```text
{P:stat|op=put|model=trade.demographics|relation=allergy_avoid|from=cell.sensor|to=allergen.pollen|commodity=pollen}
{P:stat|op=health_inpaint|kind=hives|part=LeftForearm|allergen=toad}
{P:stat|op=health_inpaint_event|id=tongue_stuck_flagpole|part=Tongue}
{P:stat|op=sample|model=civ.demographics|key=spatial|x=1|y=0|z=2}
```
