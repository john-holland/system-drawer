using System.Globalization;
using UnityEngine;

/// <summary>Applies lemma placeholders onto pen/ink instruments without referencing Open.Runtime.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Painting/Pen Ink Lemma Resolver")]
public sealed class PenInkLemmaResolver : MonoBehaviour
{
    public string placeholderName = PenInkLemmaPropertyKeys.Ink;
    public PenInkInstrument instrument;
    public PaintCanvas canvas;
    public PenInkIkGoals ikGoals;

    public bool Apply(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (instrument == null)
            instrument = GetComponent<PenInkInstrument>();
        if (canvas == null)
            canvas = GetComponent<PaintCanvas>();
        if (ikGoals == null)
            ikGoals = GetComponent<PenInkIkGoals>();

        switch (key)
        {
            case PenInkLemmaPropertyKeys.Paintlike:
                if (instrument != null)
                    instrument.ResolveInk().paintlikeInk = value != "false";
                return true;
            case PenInkLemmaPropertyKeys.Dilution:
                if (instrument != null && TryFloat(value, out float dil))
                    instrument.ResolveInk().dilution = Mathf.Clamp01(dil);
                return instrument != null;
            case PenInkLemmaPropertyKeys.SingleLayerMix:
                if (instrument != null)
                    instrument.ResolveInk().singleLayerMixing = value != "false";
                return true;
            case PenInkLemmaPropertyKeys.MaxBendDeg:
                if (instrument != null && TryFloat(value, out float bend))
                    instrument.ResolveNib().maxBendDeg = Mathf.Clamp(bend, 0f, 45f);
                return instrument != null;
            case PenInkLemmaPropertyKeys.SeeThroughSec:
                if (instrument != null && TryFloat(value, out float sec))
                    instrument.ResolveInk().seeThroughDrySeconds = Mathf.Max(0f, sec);
                return instrument != null;
            case PenInkLemmaPropertyKeys.Aperture:
                if (instrument != null && TryFloat(value, out float ap))
                    instrument.ExpandAperture(ap);
                return instrument != null;
            case PenInkLemmaPropertyKeys.CapOpen:
            case PenInkLemmaPropertyKeys.Open:
                SetCapOpen(value != "false");
                return true;
            case PenInkLemmaPropertyKeys.Close:
                SetCapOpen(false);
                return true;
            case PenInkLemmaPropertyKeys.Wet:
                ikGoals?.ResolveWetGoal();
                return true;
            case PenInkLemmaPropertyKeys.Dry:
                ikGoals?.ResolveDryGoal();
                return true;
            default:
                return false;
        }
    }

    void SetCapOpen(bool open)
    {
        if (instrument != null)
            instrument.capOpen = open;
        SendMessage("OnPenCapOpen", open, SendMessageOptions.DontRequireReceiver);
    }

    static bool TryFloat(string value, out float f) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
}
