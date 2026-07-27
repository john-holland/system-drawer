# Inventory loadouts

SQL-backed loadouts for PCs/NPCs with Continuuuum authoring page and Unity runtime gate.

## Schema

Table `loadouts` (`Scripts/continuuuum_loadouts_schema.sql`): name, icon_asset, prefab_id, takeout/putaway anim flags, ownedby/heldby actor ids, onground xyz, `loadout_set_id`.

API: `/api/loadouts` CRUD + `/transfer` + `/sets` + `/ensure`  
Page: `/inventory-loadouts` (nav: **Inventory Loadouts**)

## Unity

| Type | Role |
|------|------|
| `InventoryManager` | Sync mirror; **scriptMentionGate** (no silent pickups) |
| `ActorInventory` | Per-actor bag |
| `InventoryLemmaResolver` | `{P:have|…}` give/take/transfer; missing name → silent tool-use |
| `TradePanel` | Yours / your offer / their offer; preselect + disable dropdown |
| `NarrativeTradeAction` | Approach → conversation → accept; transfer only after converse |

## Lemmas

```
{P:have|item=drink|op=assert}
{P:have|op=give|item=radio|from=tim|to=sara}
```
