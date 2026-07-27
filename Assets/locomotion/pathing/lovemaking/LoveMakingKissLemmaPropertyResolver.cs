using System.Collections.Generic;

/// <summary>Resolves kiss lemma props and paints onto LoveCards (Locomotion side).</summary>
public static class LoveMakingKissLemmaPropertyResolver
{
    public static LoveMakingKissLemmaProperties Resolve(
        Dictionary<string, string> parameters,
        string placeholderName = "kiss") =>
        LoveMakingKissLemmaProperties.ResolveFromParams(parameters, placeholderName);

    public static LoveCard PaintCard(
        LoveCard card,
        Dictionary<string, string> parameters,
        string placeholderName = "kiss")
    {
        if (card == null) return null;
        var props = Resolve(parameters, placeholderName);
        card.loveMoveKind = LoveMakingMoveKind.Kiss;
        if (props.kissAnimationIntensity < 0f)
            props.kissAnimationIntensity = LoveMakingAnimationGroup.DefaultIntensityForLemma(placeholderName);
        card.ApplyKissLemma(props);
        return card;
    }

    public static bool IsKissLemma(string placeholderName)
    {
        if (string.IsNullOrEmpty(placeholderName)) return false;
        string n = placeholderName.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
        for (int i = 0; i < LoveMakingKissLemmaPropertyKeys.LemmaPlaceholders.Length; i++)
        {
            string p = LoveMakingKissLemmaPropertyKeys.LemmaPlaceholders[i].Replace(' ', '-');
            if (n == p || n.Contains(p))
                return true;
        }
        return false;
    }
}
