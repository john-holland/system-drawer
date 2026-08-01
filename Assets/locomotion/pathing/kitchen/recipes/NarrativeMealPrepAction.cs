using System.Collections.Generic;
using UnityEngine;

/// <summary>Maps narrative meal-prep kinds to ChefCards / CardPlan action kinds.</summary>
public static class NarrativeMealPrepAction
{
    public static CardPlanActionKind ToCardPlanAction(NarrativeMealPrepActionKind kind)
    {
        switch (kind)
        {
            case NarrativeMealPrepActionKind.PrepPlate: return CardPlanActionKind.PrepPlate;
            case NarrativeMealPrepActionKind.PrepServe: return CardPlanActionKind.PrepServe;
            case NarrativeMealPrepActionKind.TearDown: return CardPlanActionKind.TearDown;
            case NarrativeMealPrepActionKind.WashDish: return CardPlanActionKind.WashDish;
            case NarrativeMealPrepActionKind.PrepCook:
            case NarrativeMealPrepActionKind.PrepFetch:
            case NarrativeMealPrepActionKind.PrepWash:
                return CardPlanActionKind.CookDuty;
            default:
                return CardPlanActionKind.CookDuty;
        }
    }

    public static ChefCard MakeChefCard(RecipeStepSpec step, GameObject station = null)
    {
        if (step == null)
            return ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Place, station);
        var card = ChefCard.Generate(ChefDutyMode.Line, step.chefActivity, station);
        if (step.dutyChecklistOverride != null && step.dutyChecklistOverride.Count > 0)
            card.dutyChecklist = new List<string>(step.dutyChecklistOverride);
        card.sectionName = string.IsNullOrEmpty(step.label) ? card.sectionName : step.label;
        return card;
    }

    public static List<CardPlanActionKind> PreviewPlan(RecipeBehaviorTreeAsset recipe)
    {
        var list = new List<CardPlanActionKind>();
        if (recipe?.steps == null) return list;
        for (int i = 0; i < recipe.steps.Count; i++)
        {
            if (recipe.steps[i] == null) continue;
            list.Add(ToCardPlanAction(recipe.steps[i].narrativeAction));
        }
        return list;
    }
}
