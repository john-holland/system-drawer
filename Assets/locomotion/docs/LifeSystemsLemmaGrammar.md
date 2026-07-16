# Life Systems Lemma Grammar

Prompt painting for vitals, organs, mood, buffs, and authored illness.

## Placeholder

`{P:life|key=value|...}`

## Operations (`op`)

| Op | Purpose |
|----|---------|
| `set` | Set channel to `value`, or set `difficulty=easy\|normal` |
| `adjust` | Add `delta` to `channel` for `duration` seconds |
| `query` | Read mood / organ / channel (`q=mood`, `q=organ`, `id=heart`) |
| `buff` | Supplement: `lifeForce`, optional `bioRhythm`, `duration` |
| `illness` | Authored only: channel `delta` for `duration` |
| `organ` | Organ raw health `delta` (`id=heart`); `raw=1` for raw readout |

## Examples

```text
{P:life|op=set|channel=depression|value=0.2|duration=120}
{P:life|op=adjust|channel=mania|delta=0.15|duration=60}
{P:life|op=query|q=mood}
{P:life|op=buff|lifeForce=0.1|duration=300|label=supplement}
{P:life|op=illness|channel=immune|delta=-0.35|duration=600|label=authored-flu}
{P:life|op=organ|id=heart|delta=-0.4|duration=0|raw=1}
{P:life|op=query|q=organ|id=liver}
{P:life|op=set|difficulty=easy}
```

## Defaults

- Organs spawn **Great** (`rawHealth=1.05`).
- Characters do not spontaneously fall ill; only `illness` / explicit organ trauma.
- Queries return normalized organ health unless `raw=1`.

## Runtime

`LifeSystemsLemmaResolver.Execute` / `ExecuteFromScript` → `LifeSystemsServices`.
