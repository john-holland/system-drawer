using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// All English (default) built-in vocabulary entries for local Continuum-aligned tooling.
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
        var list = new List<VocabularyBuiltInDescriptor>(256);
        const string en = "en";

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

        var arr = list.ToArray();
        var dup = arr.GroupBy(x => x.Id).FirstOrDefault(g => g.Count() > 1);
        if (dup != null)
            throw new InvalidOperationException("Duplicate built-in URN: " + dup.Key);

        return arr;
    }
}
