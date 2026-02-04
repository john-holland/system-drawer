using System;

namespace Locomotion.Narrative
{
    /// <summary>Binding status for a phrase/event after ORM resolution.</summary>
    public enum BindingStatus
    {
        NotUnderstood,
        UnderstoodNoOrmMatch,
        OrmMatched,
        MarkedGenerate
    }

    /// <summary>One binding: phrase or event title resolved (or not) to an ORM key; status for examination.</summary>
    [Serializable]
    public struct InterpretedEventBinding
    {
        /// <summary>Event index in lastInterpretedEvents, or -1 if phrase-only.</summary>
        public int eventIndex;
        /// <summary>Phrase or title that was resolved.</summary>
        public string phrase;
        /// <summary>Resolved ORM key when status is OrmMatched.</summary>
        public string resolvedOrmKey;
        public BindingStatus status;

        public static InterpretedEventBinding NoMatch(int eventIndex, string phrase)
        {
            return new InterpretedEventBinding
            {
                eventIndex = eventIndex,
                phrase = phrase ?? "",
                resolvedOrmKey = "",
                status = BindingStatus.UnderstoodNoOrmMatch
            };
        }

        public static InterpretedEventBinding Matched(int eventIndex, string phrase, string ormKey)
        {
            return new InterpretedEventBinding
            {
                eventIndex = eventIndex,
                phrase = phrase ?? "",
                resolvedOrmKey = ormKey ?? "",
                status = BindingStatus.OrmMatched
            };
        }

        public static InterpretedEventBinding Generate(int eventIndex, string phrase)
        {
            return new InterpretedEventBinding
            {
                eventIndex = eventIndex,
                phrase = phrase ?? "",
                resolvedOrmKey = "",
                status = BindingStatus.MarkedGenerate
            };
        }
    }
}
