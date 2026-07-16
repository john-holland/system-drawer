# Life Systems ↔ Gov-Glove / Need Aspects

Society snapshot features and DreamCycle need aspects **bias baselines only**. They never apply illness or organ trauma.

## Feature → channels

| Society feature (camel / snake) | Channels biased |
|---------------------------------|-----------------|
| `healthcareCoverage` / `healthcare_coverage` | immune, vitamins, heart_rate, blood pressure, blood_sugar, lipids, cholesterol, lymph, endocrine |
| `water` | hydration |
| `civic_trust` / `civicTrust` | morale, empathy, sympathy |
| `taxRate` / `tax_rate` | liberalism |
| `congressStability` / `congress_stability` | conservatism |
| `welfareBenefits` / `welfare_benefits` | socialism, communism |

## Need aspect → channels

| Aspect id | Soft influence |
|-----------|----------------|
| `need_physiological` | metabolic / clinical band |
| `need_safety` | conservatism, safety-linked affect |
| `need_belonging` | empathy, sympathy, socialism |
| `need_esteem` | morale, liberalism |
| `need_self_actualization` | clear_thought, attention (light) |

## API

```csharp
LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, societyFeatures, needSatisfied01);
```

Biases are clamped into each channel’s soft healthy band.
