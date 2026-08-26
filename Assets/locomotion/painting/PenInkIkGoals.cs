using UnityEngine;

/// <summary>Wet / dry IK goals for pen-ink: hydro film, paint pile, drink stream, towel, canvas.</summary>
[AddComponentMenu("Locomotion/Painting/Pen Ink IK Goals")]
public sealed class PenInkIkGoals : MonoBehaviour
{
    public PenInkInstrument instrument;
    public PaintCanvas canvas;
    public PaintPileLiquidDriver pile;
    public Transform drinkStreamTip;
    public Component drinkNozzle;
    public Component drinkFlow;
    public Transform towel;
    public PaintSmudgeCollider blotSmudge;
    public PhysicsIKTrainingCategory wetCategory = PhysicsIKTrainingCategory.Drink;
    public PhysicsIKTrainingCategory dryCategory = PhysicsIKTrainingCategory.ToolUse;
    public PhysicsIKTrainingCategory capOpenCategory = PhysicsIKTrainingCategory.Open;
    public PhysicsIKTrainingCategory capCloseCategory = PhysicsIKTrainingCategory.Close;

    public Vector3 ResolveWetGoal()
    {
        if (canvas == null)
            canvas = GetComponent<PaintCanvas>();
        if (canvas != null)
        {
            var hydro = canvas.GetComponent<PaintCanvasHydroSolver>();
            if (hydro != null && hydro.TryGetFilmCentroid(out Vector3 film))
                return film;
        }
        if (pile == null)
            pile = GetComponent<PaintPileLiquidDriver>();
        if (pile != null && pile.totalMass > 1e-5f)
            return pile.pileCenter;
        if (drinkFlow != null)
            return PenInkNozzleAccess.GetStreamTip(drinkFlow);
        if (drinkStreamTip != null)
            return drinkStreamTip.position;
        if (drinkNozzle != null && PenInkNozzleAccess.GetLoopPourActive(drinkNozzle))
            return PenInkNozzleAccess.GetStreamTip(drinkNozzle);
        return instrument != null ? instrument.TipWorld : transform.position;
    }

    public Vector3 ResolveDryGoal()
    {
        if (towel != null)
            return towel.position;
        if (canvas != null)
            return canvas.transform.position;
        return transform.position;
    }

    public void BlotDry(Vector3 worldPoint, Vector3 normal)
    {
        if (blotSmudge == null)
            blotSmudge = GetComponent<PaintSmudgeCollider>();
        var col = GetComponent<Collider>();
        if (blotSmudge != null && col != null)
            blotSmudge.ApplySmudge(worldPoint, normal, Vector3.right, col);
        if (canvas != null && canvas.layerStack != null)
        {
            var layer = canvas.layerStack.TopWetLayer();
            if (layer != null)
                layer.dry01 = Mathf.Clamp01(layer.dry01 + 0.25f);
        }
    }

    public PhysicsIKTrainingCategory CategoryForId(string id)
    {
        switch (id)
        {
            case "pen_dip": return wetCategory;
            case "cap_open": return capOpenCategory;
            case "cap_close": return capCloseCategory;
            case "blot_dry": return dryCategory;
            default: return PhysicsIKTrainingCategory.ToolUse;
        }
    }
}
