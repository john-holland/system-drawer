namespace Locomotion.Open
{
    public static class OpenCloseLemmaPropertyContextExtensions
    {
        public static OpenCloseLemmaProperties ResolveOpenCloseLemmaProperties(this AnimationPlaybackPolicyContext ctx, int charStart = -1, int charEnd = -1)
        {
            if (ctx == null)
                return OpenCloseLemmaProperties.Defaults;
            return OpenCloseLemmaPropertyResolver.Resolve(
                ctx.GetSegmentsForActivePhrase(),
                ctx.GetBindingsForActivePhrase(),
                ctx.LemmaProperties,
                charStart,
                charEnd);
        }
    }
}
