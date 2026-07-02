# Lemma-native quest grammar

Authors define player-facing quests in **lemma `lemmaPrompt` strings** using `{P:quest|...}` spans.

## Example (Little Prince tour)

```
{P:quest|quest-set=little-prince-tour}"Explore the asteroid belt"
  {P:quest|objective=meet-fox|spatial4d=s4d-fox-vol|predicate4d=fox-met|completion4d=fox-dialogue-done}
    {P:quest|summary=Meet the fox on the equator|style=watercolor-storybook}
    {P:quest|generate-summary-start}A wise fox waits where the sunset repeats...{P:quest|generate-summary-end}
    {P:quest|travel-binding=fox-approach|map-layer=emergence|ui-bt=quest-journal-minimal}
{P:quest|end-block=little-prince-tour}
```

Indentation defines objective nesting. Each line with `objective=` becomes a quest node under the parent at the previous indent level.

## Properties (v1)

| Property | Meaning |
|----------|---------|
| `quest-set` / `end-block` | Open/close named quest set |
| `objective` | Stable objective id |
| `spatial4d` | Bind to `spatial_4d.id` |
| `bounds3d` / `bounds4d` | Override AABB (`xMin..tMax` comma form) |
| `predicate4d` / `completion4d` | Same semantics as dialogue |
| `summary` | Bespoke objective summary |
| `style` / `style-suggest` | Style profile key or inline hint |
| `generate-summary-start/end` | Out-painting span for summary text |
| `generate-art-start/end` | Out-painting span for quest art prompt |
| `inpaint-region` | Mask rect or spatial leaf ids for art inpaint |
| `travel-binding` | Key into `quest_pathing` preset |
| `map-layer` | `occupancy`, `causal`, `emergence`, `composite` |
| `ui-bt`, `map-bt`, `anim-bt` | Behavior tree asset refs |
| `audio-cue`, `ambient-loop` | Sound refs |

## API

- `POST /api/quest/compile` — compile lemma text
- `POST /api/thesaurus/entries/<id>/compile-quest` — compile + persist
- `POST /api/quest/session/open` — start server session
- `GET /api/quest/spatial-nodes` — spatial tree for map editor
- Cave messages: `quest_session_open`, `quest_objective_activate`, `quest_objective_complete`, etc.

## Unity

- `QuestSpanParser.cs` — offline compile mirror
- `QuestRunner.cs` — server-authoritative session client
- `QuestMapRenderer.cs` — orthographic map from `SpatialGenerator4D.TryGetSliceAtT`
- `QuestMapEditorWindow` — editor authoring window
