# Recipe Behavior Tree + Meal Tray Serving

Authoring: **Window → System Drawer → Recipes**. Assets live under `Assets/Recipes/` (`RecipeBehaviorTreeAsset`, `MealRecipeBehaviorTreeAsset`).

## Runtime spine

1. Resolve special commodities (`SpecialCommodityResolver`)
2. Emit ChefCards from recipe steps (`NarrativeMealPrepAction` / `MealRecipeRunner`)
3. Tray batches (`TrayBinAllocator`) → place BT ids (`TrayPlacementBtBuilder`)
4. Optional table layout (`MealTableLayoutBranch` / PlaceBuild topology)
5. Taste notes → life-systems + dialog (`TasteNotesApplicator`)
6. Optional tear-down → season pans + dishwashing Hanoi

Bailouts (`TrayServeBailout`): tray dropped, already eaten, place waypoint covered → single-person / sans-tray requeue.

## Taste notes

Lemma: `{P:taste|notes=sour,spicy|intensity=0.5}`

| Note | Chemical mood |
|------|----------------|
| sour | blood_pressure_sys, hypertensive_load |
| spicy | endorphin, adrenaline tint |
| sweet | blood_sugar, morale |
| bitter | attention, clear_thought |
| umami | morale, lipids tint |
| salty | hydration↓, blood pressure mild |

## Special commodities

`RecipeCommoditySpec`: `displayName` (e.g. `nanas classic sauce`), `specialOf` (`nanas`), **`supplementable = true` by default**. Missing special materializes from base stock.

## CardPlan actions

`CookDuty`, `PrepPlate`, `PrepServe`, `TearDown`, `WashDish` on `CardPlanActionKind`.

See also [KitchenTearDownDishwashing.md](KitchenTearDownDishwashing.md).
