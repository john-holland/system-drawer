using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// All English (default) built-in vocabulary entries for local Continuuuum-aligned tooling.
/// </summary>
public static class VocabularyBuiltInRegistry
{
    private static readonly VocabularyBuiltInDescriptor[] AllDescriptors = BuildAll();

    public static IReadOnlyList<VocabularyBuiltInDescriptor> All => AllDescriptors;

    public static int Count => AllDescriptors.Length;

    public static VocabularyBuiltInDescriptor? TryGetById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        for (int i = 0; i < AllDescriptors.Length; i++)
        {
            if (AllDescriptors[i].Id == id)
                return AllDescriptors[i];
        }
        return null;
    }

    private static VocabularyBuiltInDescriptor[] BuildAll()
    {
        var list = new List<VocabularyBuiltInDescriptor>(320);
        const string en = "en";
        string[] nsmPrimeTags = { "nsm", "prime" };

        void Add(
            string segment,
            string term,
            string posTag,
            VocabularyBuiltInCategory category,
            string[] tags = null)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn(en, segment, term);
            list.Add(new VocabularyBuiltInDescriptor(id, en, term, posTag, category, tags));
        }

        void TagOrAddPrime(string segment, string term, string posTag)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn(en, segment, term);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id)
                    continue;
                var old = list[i];
                var merged = new List<string>(old.Tags ?? Array.Empty<string>());
                foreach (var t in nsmPrimeTags)
                {
                    if (!merged.Exists(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)))
                        merged.Add(t);
                }
                list[i] = new VocabularyBuiltInDescriptor(
                    old.Id, old.LanguageCode, old.Term, old.PosTag, old.Category, merged);
                return;
            }
            Add(segment, term, posTag, VocabularyBuiltInCategory.SemanticPrime, nsmPrimeTags);
        }

        // Articles / determiners
        string[] detNone = null;
        Add("det", "the", "determiner", VocabularyBuiltInCategory.Article, detNone);
        Add("det", "a", "determiner", VocabularyBuiltInCategory.Article, detNone);
        Add("det", "an", "determiner", VocabularyBuiltInCategory.Article, detNone);
        Add("det", "this", "determiner", VocabularyBuiltInCategory.Determiner, detNone);
        Add("det", "that", "determiner", VocabularyBuiltInCategory.Determiner, detNone);
        Add("det", "these", "determiner", VocabularyBuiltInCategory.Determiner, detNone);
        Add("det", "those", "determiner", VocabularyBuiltInCategory.Determiner, detNone);

        // Prepositions
        string[] prepSpatial = { "spatial" };
        foreach (var w in new[]
                 {
                     "in", "on", "at", "to", "from", "with", "by", "near", "between", "through",
                     "across", "around", "inside", "outside", "along"
                 })
            Add("prep", w, "preposition", VocabularyBuiltInCategory.Preposition, prepSpatial);

        // Discourse / causality
        foreach (var w in new[]
                 {
                     "if", "then", "else", "but", "because", "when", "while", "although", "therefore", "unless",
                     "and", "or", "nor", "yet", "so"
                 })
            Add("conj", w, "conjunction", VocabularyBuiltInCategory.DiscourseCausality, new[] { "causality" });
        Add("adv", "not", "adverb", VocabularyBuiltInCategory.DiscourseCausality, new[] { "causality" });

        // Spatial / gateway vocabulary
        foreach (var w in new[] { "back", "forward", "pause", "center" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "gateway", "spatial" });
        foreach (var w in new[] { "north", "south", "east", "west" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "spatial", "cardinal" });
        foreach (var w in new[] { "volume", "region", "path", "boundary", "time", "slice" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "spatial" });

        foreach (var w in new[] { "roads", "road", "buildings", "building", "town-hall", "highway", "street" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.Subject, new[] { "world", "spatial" });
        foreach (var w in new[] { "to-the-left-of", "to-the-right-of", "in-front-of", "through-there", "over-here", "along-the-road", "left-of", "right-of" })
            Add("prep", w, "preposition", VocabularyBuiltInCategory.Preposition, prepSpatial);
        foreach (var w in new[] { "there", "here", "here-here", "there-there" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "spatial", "deictic" });

        // Actions
        foreach (var w in new[] { "go", "move", "open", "close", "take", "use", "place", "connect", "set", "run", "drink" })
            Add("verb", w, "verb", VocabularyBuiltInCategory.Action, null);

        // Kiss / romance lemmas ({P:kiss|kiss-animation=...})
        string[] kissTags = { "romance", "kiss", "lovemaking" };
        foreach (var w in new[] { "kiss", "peck", "smooch", "smooching", "making-out", "make-out" })
            Add("verb", w, "verb", VocabularyBuiltInCategory.Action, kissTags);

        // Stuntman / Safety Warden / parkour lemmas
        string[] stuntTags = { "stunt", "parkour", "travel" };
        string[] safetyTags = { "safety", "travel", "warden" };
        foreach (var w in new[] { "stunt", "parkour", "mantle", "scale", "vault", "crash-through" })
            Add("verb", w, "verb", VocabularyBuiltInCategory.Action, stuntTags);
        foreach (var w in new[] { "wall-run", "fall-roll", "spring-landing" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.Subject, stuntTags);
        Add("adv", "safely", "adverb", VocabularyBuiltInCategory.DiscourseCausality, safetyTags);
        Add("adj", "safe", "adjective", VocabularyBuiltInCategory.DiscourseCausality, safetyTags);
        Add("noun", "runway", "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "stunt", "spatial", "travel" });
        Add("noun", "terminus", "noun", VocabularyBuiltInCategory.SpatialGateway, new[] { "stunt", "spatial", "travel" });

        foreach (var w in new[] { "unlock", "latch", "drawer", "lid", "hinge", "guard" })
            Add("verb", w, "verb", VocabularyBuiltInCategory.Action, new[] { "open-close" });

        // Drink / comedy liquid lemmas
        foreach (var w in new[] { "almost" })
            Add("adv", w, "adverb", VocabularyBuiltInCategory.DiscourseCausality, new[] { "liquid", "comedy" });
        foreach (var w in new[] { "lips", "mouth", "coffee", "turbulence", "tray" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.Subject, new[] { "liquid", "comedy" });
        foreach (var w in new[] { "stalled", "spilled", "empty-handed", "endless" })
            Add("adj", w, "adjective", VocabularyBuiltInCategory.DiscourseCausality, new[] { "liquid", "comedy" });

        // Subjects / world
        foreach (var w in new[] { "object", "scene", "world", "door", "target", "source", "effect" })
            Add("noun", w, "noun", VocabularyBuiltInCategory.Subject, new[] { "world" });
        string[] playerControllerTags = { "controller", "spatial", "player" };
        Add("noun", "player", "noun", VocabularyBuiltInCategory.Subject, new[] { "controller", "spatial", "player", "world" });
        Add("noun", "first-person", "noun", VocabularyBuiltInCategory.Subject, playerControllerTags);
        Add("noun", "third-person", "noun", VocabularyBuiltInCategory.Subject, playerControllerTags);

        // Literal types — pos_tag type_name avoids collision with natural-language homonyms (e.g. time).
        void Lit(string term, string tag)
        {
            Add("literal", term, "type_name", VocabularyBuiltInCategory.LiteralType, new[] { "literal", tag });
        }

        Lit("string", "primitive");
        Lit("integer", "primitive");
        Lit("number", "primitive");
        Lit("float", "primitive");
        Lit("boolean", "primitive");
        Lit("null", "primitive");
        Lit("vector2", "unity");
        Lit("vector3", "unity");
        Lit("vector4", "unity");
        Lit("quaternion", "unity");
        Lit("color", "unity");
        Lit("rect", "unity");
        Lit("bounds", "unity");
        Lit("time", "time");
        Lit("guid", "identity");
        Lit("uuid", "identity");
        Lit("uri", "identity");

        // NSM semantic primes (65). Overlaps tag existing URNs; new terms use SemanticPrime.
        // Substantives
        TagOrAddPrime("pron", "I", "pronoun");
        TagOrAddPrime("pron", "you", "pronoun");
        TagOrAddPrime("noun", "someone", "noun");
        TagOrAddPrime("noun", "something", "noun");
        TagOrAddPrime("noun", "people", "noun");
        TagOrAddPrime("noun", "body", "noun");
        // Relational substantives
        TagOrAddPrime("noun", "kind", "noun");
        TagOrAddPrime("noun", "part", "noun");
        // Determiners
        TagOrAddPrime("det", "this", "determiner");
        TagOrAddPrime("det", "the-same", "determiner");
        TagOrAddPrime("det", "other", "determiner");
        // Quantifiers
        TagOrAddPrime("num", "one", "numeral");
        TagOrAddPrime("num", "two", "numeral");
        TagOrAddPrime("det", "some", "determiner");
        TagOrAddPrime("det", "all", "determiner");
        TagOrAddPrime("det", "much", "determiner");
        TagOrAddPrime("det", "little", "determiner");
        // Evaluators / descriptors
        TagOrAddPrime("adj", "good", "adjective");
        TagOrAddPrime("adj", "bad", "adjective");
        TagOrAddPrime("adj", "big", "adjective");
        TagOrAddPrime("adj", "small", "adjective");
        // Mental predicates
        TagOrAddPrime("verb", "know", "verb");
        TagOrAddPrime("verb", "think", "verb");
        TagOrAddPrime("verb", "want", "verb");
        TagOrAddPrime("verb", "dont-want", "verb");
        TagOrAddPrime("verb", "feel", "verb");
        TagOrAddPrime("verb", "see", "verb");
        TagOrAddPrime("verb", "hear", "verb");
        // Speech
        TagOrAddPrime("verb", "say", "verb");
        TagOrAddPrime("noun", "words", "noun");
        TagOrAddPrime("adj", "true", "adjective");
        // Actions / events / movement / contact
        TagOrAddPrime("verb", "do", "verb");
        TagOrAddPrime("verb", "happen", "verb");
        TagOrAddPrime("verb", "move", "verb");
        TagOrAddPrime("verb", "touch", "verb");
        // Existence / possession
        TagOrAddPrime("verb", "be-somewhere", "verb");
        TagOrAddPrime("verb", "there-is", "verb");
        TagOrAddPrime("verb", "be-someone", "verb");
        TagOrAddPrime("verb", "have", "verb");
        TagOrAddPrime("verb", "give", "verb");
        TagOrAddPrime("verb", "take", "verb");
        TagOrAddPrime("verb", "transfer", "verb");
        TagOrAddPrime("noun", "waypoint", "noun");
        TagOrAddPrime("noun", "formation", "noun");
        TagOrAddPrime("noun", "triangle", "noun");
        TagOrAddPrime("noun", "pineapple", "noun");
        // Life and death
        TagOrAddPrime("verb", "live", "verb");
        TagOrAddPrime("verb", "die", "verb");
        // Time
        TagOrAddPrime("conj", "when", "conjunction");
        TagOrAddPrime("adv", "now", "adverb");
        TagOrAddPrime("adv", "before", "adverb");
        TagOrAddPrime("adv", "after", "adverb");
        TagOrAddPrime("noun", "a-long-time", "noun");
        TagOrAddPrime("noun", "a-short-time", "noun");
        TagOrAddPrime("noun", "for-some-time", "noun");
        TagOrAddPrime("noun", "moment", "noun");
        // Space
        TagOrAddPrime("noun", "where", "noun");
        TagOrAddPrime("noun", "here", "noun");
        TagOrAddPrime("prep", "above", "preposition");
        TagOrAddPrime("prep", "below", "preposition");
        TagOrAddPrime("adj", "far", "adjective");
        TagOrAddPrime("prep", "near", "preposition");
        TagOrAddPrime("noun", "side", "noun");
        TagOrAddPrime("prep", "inside", "preposition");
        // Logical
        TagOrAddPrime("adv", "not", "adverb");
        TagOrAddPrime("adv", "maybe", "adverb");
        TagOrAddPrime("verb", "can", "verb");
        TagOrAddPrime("conj", "because", "conjunction");
        TagOrAddPrime("conj", "if", "conjunction");
        // Intensifier / augmentor / similarity
        TagOrAddPrime("adv", "very", "adverb");
        TagOrAddPrime("adv", "more", "adverb");
        TagOrAddPrime("prep", "like", "preposition");
        // WHEN~TIME overlap: tag existing spatial "time" noun without a second prime row
        TagOrAddPrime("noun", "time", "noun");

        var arr = list.ToArray();
        var dup = arr.GroupBy(x => x.Id).FirstOrDefault(g => g.Count() > 1);
        if (dup != null)
            throw new InvalidOperationException("Duplicate built-in URN: " + dup.Key);

        return arr;
    }
}
