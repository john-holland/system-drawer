using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>Tracks consumed narrative phrases so drink beats do not re-run.</summary>
    [Serializable]
    public sealed class LemmaConsumptionRegistry
    {
        [SerializeField] List<string> consumedKeys = new List<string>();

        public static string MakeKey(string phrase, int eventIndex) =>
            $"{eventIndex}:{phrase ?? ""}".Trim();

        public bool IsConsumed(string phrase, int eventIndex)
        {
            string key = MakeKey(phrase, eventIndex);
            return consumedKeys != null && consumedKeys.Contains(key);
        }

        public void MarkConsumed(string phrase, int eventIndex)
        {
            if (consumedKeys == null)
                consumedKeys = new List<string>();
            string key = MakeKey(phrase, eventIndex);
            if (!consumedKeys.Contains(key))
                consumedKeys.Add(key);
        }

        public void Clear() => consumedKeys?.Clear();
    }
}
