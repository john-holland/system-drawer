namespace Locomotion.Narrative
{
    /// <summary>
    /// Registry of dialogue goal string constants; mirrored in dialogue_goals SQL table.
    /// </summary>
    public static class DialogueGoalNames
    {
        public const string ZoanUnderstanding = "zoan-understanding";
        public const string LongMoverDiscussed = "long-mover-discussed";
        public const string WindyManDiscussed = "windy-man-discussed";
        public const string BookConcertComplete = "book-concert-complete";

        public static readonly string[] All =
        {
            ZoanUnderstanding,
            LongMoverDiscussed,
            WindyManDiscussed,
            BookConcertComplete
        };
    }
}
