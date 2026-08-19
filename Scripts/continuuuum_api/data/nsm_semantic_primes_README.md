# NSM semantic primes (English)

Canonical Continuuuum inventory of the **65** Natural Semantic Metalanguage English exponents.

## Files

| File | Role |
|------|------|
| `nsm_semantic_primes_en.json` | Canonical 65 terms (term, pos, segment, group) |
| `nsm_semantic_prime_glosses_en.json` | Hand-authored glosses + `mechanicalRole` (`LemmaMechanicalRole` enum names) |
| `nsm_prime_associations_en.json` | Pairwise causality / temporal / fuzzy_hedge edges |
| `nsm_fuzzy_hedges_en.json` | Phrase hedges + adjustable membership curves |
| `builtin_vocabulary.json` | Exported Unity builtins (must tag every prime with `nsm` + `prime`) |
| [builtin_vocabulary_README.md](builtin_vocabulary_README.md) | Export → completion sync, hyphen/space parse notes |

Unity: `VocabularyBuiltInRegistry.cs` (`TagOrAddPrime`), gameplay via `{P:nsm|term=…}` → `NsmPrimeLemmaResolver`.

## Implemented meaning

A prime is **implemented** when:

1. API: association/logical-form wiring present (`POST /api/nsm/seed-wiring`)
2. Unity: a handler path exists in `NsmPrimeLemmaResolver` for that term (group dispatch)

## Logical forms + fuzzy

- AST ops: `prime`, `var`, `not`, `and`, `or`, `if`, `because`, `can`, `maybe`, `before`, `after`, `when`, `like`, `true`, `hedge`, `grade`
- Evaluate: `POST /api/nsm/evaluate` with `mode: bool|fuzzy`, optional `sessionId` + `upsertVars`
- Hedges: `GET/PATCH /api/nsm/fuzzy/hedges`
- Session cache: `GET/PUT /api/nsm/fuzzy/vars/<sessionId>` (supports *less skittish*, *just like … before*)

Composed fixture: **later** → `after(now)` (association + form).

## Change of basis

Rewrite engine (`change_of_basis_engine.py`) supports language-pair rules with multi-word / word-id activation, clause replacement, conjugation, count defaults, loop warnings, and Turing-style completion validation:

- `POST /api/thesaurus/change-of-basis` (`dryRun`, `conjugation`, …)
- `GET/PUT /api/thesaurus/change-of-basis/rules`
- `GET/PUT /api/thesaurus/change-of-basis/defaults`
- `POST /api/thesaurus/change-of-basis/validate` → `complete` | `incomplete` | `divergent`

## Overlap tagging

Nine primes share URNs with existing domain builtins (tags only):

`this`, `move`, `when`, `here`, `near`, `inside`, `not`, `because`, `if`

## Dual exponent

`time` is tagged `nsm`/`prime` for WHEN~TIME, but it is **not** a 66th row in the canonical 65 list.

## Seed

```bash
# From Scripts/
python -c "import sqlite3; from continuuuum_api.nsm_wiring_db import seed_nsm_prime_wiring; c=sqlite3.connect('continuuuum.db'); c.row_factory=sqlite3.Row; print(seed_nsm_prime_wiring(c))"
```

Or `POST /api/nsm/seed-wiring`. Lemma completion UI: `/lemma-completion` → scope **Primes**.

## Tests

```bash
python -m pytest tests/test_nsm_semantic_primes.py tests/test_nsm_logical_form.py tests/test_nsm_fuzzy_cache.py tests/test_nsm_wiring_routes.py tests/test_change_of_basis_engine.py -q
```

Unity Edit Mode: `VocabularyBuiltInEditModeTests` (`NsmSemanticPrimes_*`), `NsmPrimeLemmaResolverTests`.
