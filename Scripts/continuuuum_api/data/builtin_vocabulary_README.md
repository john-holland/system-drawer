# Built-in vocabulary: export, completion, and parsing

Unity `VocabularyBuiltInRegistry` is the source of truth. Lemma completion and prompt parsers are consumers. They drift unless export + sync run after a registry change, and they disagree unless they share one hyphen/space strategy.

## Pipeline

```
VocabularyBuiltInRegistry (C#)
        │  Continuuuum → Export Built-in Vocabulary JSON
        ▼
builtin_vocabulary.json
        │  POST /api/lemma-completion/sync-builtins
        ▼
lemma_completion (is_builtin + is_implemented)
```

- **Registry** mints `urn:unity:continuuuum:builtin:v1:/{lang}/{segment}/{slug}`.
- **JSON** is a flat export of term, pos, category, tags. Re-export after every `Add` / `TagOrAddPrime`.
- **Completion** seeds NSM primes + the common-5000 list, then overlays builtins. Compound terms that are not in the 5000 (`open-chat`, `gas-station`) are **inserted**. Homonyms that already exist (`chat`, `send`) are **flagged** on the existing row.

A registry add that is not exported never reaches completion. An export that is not synced leaves the live DB stale (this is how the structural-chat block sat in JSON while `lemma_completion` still had 261 builtins).

## Three spellings of the same lemma

| Surface | Example | Who uses it |
|---------|---------|-------------|
| Display term | `open-chat` | Registry `Term`, `{P:open-chat}`, completion `term` |
| URN slug | `open_chat` | `VocabularyLanguageEncoding.SlugSegment` (hyphen → `_`) |
| Tokenizer chunks | `open`, `chat` | `VocabularyBuiltInTokenizer` (`[a-z0-9]+` only) |

Hyphens and spaces do not survive tokenization. `open-chat`, `open_chat`, and `open chat` all become `["open", "chat"]`. Parsers that look up the joined string `"open chat"` against a dictionary keyed by `"open-chat"` miss. Parsers that only try the first token hit the generic verb `open` instead of `open-chat`.

Completion sync is more forgiving: `_term_aliases` matches hyphen / underscore / space variants when attaching `is_builtin`. Unity lookup is not.

## Two parsers

**`VocabularyBuiltInLookup.TryResolvePhrase`** (titles → URN for `NarrativeBuiltInBindingHelper`):

1. Tokenize (hyphens drop).
2. `CanonicalizeToken` **per token** (`nil` → `null`; multi-word aliases are not applied here).
3. Longest prefix of the token array, joined with **spaces**, looked up in `_byLemma` keyed by registry `Term` (usually hyphenated).

So `"open chat"` / `"open-chat"` become `"open chat"`, which is not a registry key. The loop then tries `"open"` and binds the generic open-close verb. `TryCanonicalizeMultiWordPhrase` exists and is tested, but this path does not call it.

**`WithLemmaRegistry`** (spatial / deictic / chat surface phrases):

1. Longest-first slice (up to 4 tokens).
2. `TryCanonicalizeMultiWordPhrase` → hyphenated canonical (`open the chat` → `open-chat`).
3. Domain maps (`RelationByLemma`) store **both** `"left-of"` and `"to the left of"`.

This is the parse that actually resolves compound builtins. Chat aliases are registered twice (static `BuiltInSynonyms` table and `RegisterBuiltInSynonyms`) so narrative layout and lemma painting stay aligned.

## Homonyms and last write

`_byLemma[term] = descriptor` keeps one row per display term. `time` is both a spatial noun and a literal type; `open` is a generic verb and the prefix of `open-chat`; `send` / `chat` are common-5000 words and structural-chat builtins. Completion collapses those onto one row. Gameplay must disambiguate with **placeholder + properties** (`{P:chat|op=open}` vs `{P:open-chat}` vs `{P:open}`), not by term string alone.

Literal types use `posTag = type_name` and segment `literal` so `time` the type does not share a URN with `time` the noun. Completion still keys by term, so the overlay lands on whichever row `lower(term)` hits first.

## Placeholders are not the same as builtins

`ChatLemmaPropertyKeys.LemmaPlaceholders` is `chat`, `open-chat`, `close-chat`, `dismiss`. `ParseOp` also accepts `join`, `leave`, `hang-up`, `flip`, `show` — those are **op aliases**, not registry terms, and should stay out of `builtin_vocabulary.json` unless they become first-class lemmas.

Kiss placeholders include `"making out"` (space) while the registry term is `making-out`. That only works if the caller runs the multi-word synonym path.

When adding a builtin:

1. Add the hyphenated term to the registry (and a well-known id in `VocabularyBuiltInIds` if call sites need it).
2. Add space / “the” surface forms to `BuiltInSynonyms` **and** `WithLemmaRegistry.RegisterBuiltInSynonyms` if narrative parsing should see them.
3. Export JSON, then `POST /api/lemma-completion/sync-builtins` (or re-seed).
4. Prefer `{P:canonical|…}` in prompts; treat free text as a best-effort prefix match.

## Shared parse (`VocabularyBuiltInLookup.TryResolvePhrase`)

Longest-first, after tokenize + per-token synonym canonicalize:

0. `if` is a predicate in every operator position (`IfPredicate`): prefix `if P`, infix `P if Q`, postfix `P if` / `P if so`, circumfix `if P then Q`. The anaphor `if so` after an adverb composes (`randomly, if so` → `randomly-if-so`). A leading prefix/circumfix `if` always resolves to the conjunction, not `if-so`.
1. Multi-word synonym on the slice (`open the chat` → `open-chat`; `home address` → `home-address`).
2. Hyphen-join the slice (`open` + `chat` → `open-chat`) and look up.
3. Space-join only if a registry term is actually spaced (almost none are).
4. Single-token lookup last, so compounds win over generic verbs.

## Related

- Export: Unity menu `Continuuuum/Export Built-in Vocabulary JSON`, or `-executeMethod VocabularyBuiltInJsonExporter.ExportFromCli`
- Sync: `POST /api/lemma-completion/sync-builtins`
- Primes overlay: [nsm_semantic_primes_README.md](nsm_semantic_primes_README.md)
- UI: `/lemma-completion` (scope **all**; builtin rows are `isBuiltin`)
