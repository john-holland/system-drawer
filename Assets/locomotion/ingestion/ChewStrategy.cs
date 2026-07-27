using System.Collections.Generic;
using UnityEngine;

/// <summary>Ordered chew phases for a food kind.</summary>
public enum ChewPhase
{
    FrontCut,
    BiteToFit,
    ChewFront,
    ChewMolarsProgressive,
    TongueMove,
    TongueParabola,
    DiscardInedible,
    OpenClosePeel,
    Swallow
}

public static class ChewStrategy
{
    public static List<ChewPhase> PhasesFor(FoodKind kind)
    {
        switch (kind)
        {
            case FoodKind.Cheese:
                return new List<ChewPhase>
                {
                    ChewPhase.FrontCut, ChewPhase.BiteToFit, ChewPhase.ChewMolarsProgressive,
                    ChewPhase.TongueParabola, ChewPhase.Swallow
                };
            case FoodKind.FruitVegetable:
                return new List<ChewPhase>
                {
                    ChewPhase.OpenClosePeel, ChewPhase.FrontCut, ChewPhase.ChewFront,
                    ChewPhase.DiscardInedible, ChewPhase.ChewMolarsProgressive,
                    ChewPhase.TongueMove, ChewPhase.Swallow
                };
            default:
                return new List<ChewPhase>
                {
                    ChewPhase.FrontCut, ChewPhase.BiteToFit, ChewPhase.ChewMolarsProgressive,
                    ChewPhase.TongueMove, ChewPhase.Swallow
                };
        }
    }

    /// <summary>Molar progression 0..1 further back; side bias from preferred chew side.</summary>
    public static Vector3 TongueOffsetForMeat(float progress01, bool preferRight, float sideAmp = 0.025f)
    {
        float side = preferRight ? sideAmp : -sideAmp;
        // Side-to-side while moving back.
        float sway = Mathf.Sin(progress01 * Mathf.PI * 2f) * sideAmp * 0.5f;
        return new Vector3(side + sway, 0f, Mathf.Lerp(0.01f, 0.05f, progress01));
    }
}
