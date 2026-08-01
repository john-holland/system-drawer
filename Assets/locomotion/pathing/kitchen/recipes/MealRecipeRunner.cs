using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs meal recipes in order: resolve commodities → steps/ChefCards → tray serve → taste → optional tear-down.
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Meal Recipe Runner")]
public sealed class MealRecipeRunner : MonoBehaviour
{
    public MealRecipeBehaviorTreeAsset meal;
    public InventoryManager inventory;
    public LifeSystemsSheet dinerSheet;
    public GameObject dinerActor;
    public DishWashingStation dishStation;
    public PlaceBuildTopologyAsset tableLayout;
    public bool runTearDownAfterServe = true;

    public readonly List<ChefCard> emittedCards = new List<ChefCard>();
    public readonly List<TrayBinAllocator.Batch> batches = new List<TrayBinAllocator.Batch>();
    public readonly List<string> lastDialogHints = new List<string>();
    public TrayServeBailReason lastBailReason = TrayServeBailReason.None;
    public int resolvedCommodityCount;

    public bool RunMeal()
    {
        if (meal == null) return false;
        inventory = inventory != null ? inventory : InventoryManager.Instance;
        emittedCards.Clear();
        batches.Clear();
        lastDialogHints.Clear();
        lastBailReason = TrayServeBailReason.None;
        resolvedCommodityCount = 0;

        ResolveCommodities(meal.commodities);
        if (meal.recipes != null)
        {
            for (int r = 0; r < meal.recipes.Count; r++)
            {
                var entry = meal.recipes[r];
                if (entry?.recipe == null) continue;
                ResolveCommodities(entry.recipe.commodities);
                if (entry.recipe.ingredients != null)
                {
                    for (int i = 0; i < entry.recipe.ingredients.Count; i++)
                    {
                        if (entry.recipe.ingredients[i]?.commodity != null)
                            SpecialCommodityResolver.Resolve(entry.recipe.ingredients[i].commodity, inventory);
                    }
                }
                EmitSteps(entry.recipe);
            }
        }

        int actors = meal.serveActorKeys != null && meal.serveActorKeys.Count > 0
            ? meal.serveActorKeys.Count
            : 1;
        batches.AddRange(TrayBinAllocator.BuildBatches(
            meal.TotalServes(), actors, meal.tray, meal.platesPerActor));

        if (tableLayout != null || !string.IsNullOrEmpty(meal.tableLayoutBranchKey))
            MealTableLayoutBranch.Build(tableLayout, meal.assignedActorKey, meal.tray?.placeWaypoint);

        ApplyTaste(CollectTasteNotes());

        if (runTearDownAfterServe && (meal.tearDown == null || meal.tearDown.enableTearDown))
            KitchenTearDownBranch.Run(meal.tearDown ?? new KitchenTearDownSettings(), dishStation, dinerActor);

        return true;
    }

    public BailResultHandle Bail(bool trayDropped, bool alreadyEaten, bool waypointCovered, int remainingPlates)
    {
        var bail = TrayServeBailout.Evaluate(trayDropped, alreadyEaten, waypointCovered, remainingPlates, meal?.tray);
        lastBailReason = bail.reason;
        if (bail.reason != TrayServeBailReason.None)
        {
            batches.Clear();
            batches.AddRange(bail.reducedBatches);
        }
        return new BailResultHandle { result = bail };
    }

    public sealed class BailResultHandle
    {
        public TrayServeBailout.BailResult result;
    }

    void ResolveCommodities(List<RecipeCommoditySpec> specs)
    {
        if (specs == null) return;
        resolvedCommodityCount += SpecialCommodityResolver.ResolveAll(specs, inventory);
    }

    void EmitSteps(RecipeBehaviorTreeAsset recipe)
    {
        if (recipe?.steps == null) return;
        for (int i = 0; i < recipe.steps.Count; i++)
            emittedCards.Add(NarrativeMealPrepAction.MakeChefCard(recipe.steps[i]));
    }

    List<TasteNoteEntry> CollectTasteNotes()
    {
        var notes = new List<TasteNoteEntry>();
        if (meal.tasteNotes != null) notes.AddRange(meal.tasteNotes);
        if (meal.recipes == null) return notes;
        for (int r = 0; r < meal.recipes.Count; r++)
        {
            var recipe = meal.recipes[r]?.recipe;
            if (recipe?.tasteNotes == null) continue;
            notes.AddRange(recipe.tasteNotes);
        }
        return notes;
    }

    void ApplyTaste(List<TasteNoteEntry> notes)
    {
        if (dinerSheet == null && dinerActor != null)
            dinerSheet = dinerActor.GetComponent<LifeSystemsSheet>();
        var applied = TasteNotesApplicator.Apply(dinerSheet, notes, meal.tasteNotes != null && meal.tasteNotes.Count > 0 ? 1f : 0.75f);
        lastDialogHints.AddRange(applied.dialogSuggestions);
        TasteDialogHints.SeedSendThought(dinerActor != null ? dinerActor : gameObject, applied.dialogSuggestions);
    }
}
