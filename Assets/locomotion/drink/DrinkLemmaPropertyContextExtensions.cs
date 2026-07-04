namespace Locomotion.Drink
{
    /// <summary>Extension helpers for resolving drink properties from playback policy context.</summary>
    public static class DrinkLemmaPropertyContextExtensions
    {
        public static DrinkLemmaProperties GetDrinkProperties(this AnimationPlaybackPolicyContext ctx, int charStart = -1, int charEnd = -1)
        {
            if (ctx == null)
                return DrinkLemmaProperties.Defaults;
            return DrinkLemmaPropertyResolver.Resolve(
                ctx.GetSegmentsForActivePhrase(),
                ctx.GetBindingsForActivePhrase(),
                ctx.LemmaProperties,
                charStart,
                charEnd);
        }
    }
}
