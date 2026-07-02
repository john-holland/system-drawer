# Lemma-native dialogue grammar

Authors define interactive dialogue in **lemma `lemmaPrompt` strings** using `{P:dialogue|...}` spans (same composition model as other `{P:...}` placeholders).

## Example (book-concert)

```
{P:dialogue|dialogue-set=book-concert}"What books do you think should play?"
{P:dialogue|answer=windy-man|speaker=fox}"The windy man."
{P:dialogue|answer=long-mover|speaker=fox}"The Long Mover: The Python"
  {P:dialogue|answer=handcuff-python|speaker=prince}"Oh, that's the one where they handcuff the python!?"
{P:dialogue|end-block=book-concert}
```

Indentation defines nesting. Each line with `answer=` becomes a branch under the parent at the previous indent level.

## Properties (v1)

| Property | Meaning |
|----------|---------|
| `dialogue-set` / `end-block` | Open/close named set |
| `answer` | Branch option id |
| `speaker` | `NarrativeBindings` key for actor speech + DSP |
| `vis` | `auto`, `jaw`, `wobble`, `bobble`, `none` |
| `presentation` | `text`, `ui`, `audio` |
| `goal` | Dialogue goal constant (`DialogueGoalNames`) |
| `predicate4d` | Branch visible when goal flag true |
| `completion4d` | 4D node completion unlock |
| `options=[a,b]` | Filter visible choices |
| `generate-dialogue-start/end` | LLM outpainting span |

## Unity DSP playback

When `presentation=audio` or an `audioRef` is present:

1. `DialogueRunner` loads clip via `DialogueAudioLoader`.
2. `ActorSpeechPlayback` resolves `speaker` through `NarrativeBindings`.
3. **Auto** order: `RagdollJaw` (jaw open DSP) → `ModulatingSoundComponent` (scale wobble) → fallback source.

### Scene setup

- Bind each character in `NarrativeBindings` (`fox` → ragdoll root, `prince` → ragdoll root).
- Ragdoll actors: ensure head/jaw hierarchy or rely on `RagdollSystem.FindOrAddJaw()`.
- Prop speakers without jaw: Auto adds `ModulatingSoundComponent` on the bound object.

## API

- `POST /api/dialogue/compile` — compile lemma text
- `POST /api/thesaurus/entries/<id>/compile-dialogue` — compile + persist
- `POST /api/dialogue/session/open` — start server session
- Cave messages: `dialogue_session_open`, `dialogue_choose`, `dialogue_advance`, etc.

## Goals

See `DialogueGoalNames.cs` and `dialogue_goals` SQL table. Sync from Unity via `dialogue/goals/sync`.
