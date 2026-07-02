using System;
using UnityEngine;

namespace SystemDrawer.DreamCycle
{
    [Serializable]
    public struct NeedAspectEntry
    {
        public string aspectId;
        public string displayName;
        public int tier;
        public string[] societyFeatures;
        public string[] zoneIds;
        public string[] propertyClasses;
        public string[] deviceKinds;
        public string spatialSlotId;
        public string lemmaEntryHint;
    }

    [CreateAssetMenu(fileName = "NeedAspectRegistry", menuName = "System Drawer/Need Aspect Registry")]
    public sealed class NeedAspectRegistry : ScriptableObject
    {
        public NeedAspectEntry[] aspects = DefaultAspects();

        public bool TryGet(string aspectId, out NeedAspectEntry entry)
        {
            for (int i = 0; i < aspects.Length; i++)
            {
                if (aspects[i].aspectId == aspectId)
                {
                    entry = aspects[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        public static NeedAspectEntry[] DefaultAspects() => new[]
        {
            Entry("need_physiological", "Physiological", 1, new[] { "healthcare_coverage", "water", "power" },
                new[] { "residential_low", "public_services" }, new[] { "private", "public" },
                new[] { "home_terminal", "cctv" }, "need_physiological", "dream.physiological"),
            Entry("need_safety", "Safety", 2, new[] { "tax_burden", "congress_stability" },
                new[] { "public_services", "public" }, new[] { "public" },
                new[] { "security_alarm" }, "need_safety", "dream.safety"),
            Entry("need_belonging", "Belonging", 3, new[] { "civic_trust", "religious_attendance" },
                new[] { "religious", "hobby_venue" }, new[] { "religious", "hobby_venue" },
                new[] { "social_chat" }, "need_belonging", "dream.belonging"),
            Entry("need_esteem", "Esteem", 4, new[] { "hobby_participation", "commercial_activity" },
                new[] { "commercial_core", "commercial" }, new[] { "commercial", "private" },
                new[] { "work_webtop" }, "need_esteem", "dream.esteem"),
            Entry("need_self_actualization", "Self-Actualization", 5, new[] { "spirituality_index", "creative_lemma" },
                new[] { "hobby_venue", "commercial_core" }, new[] { "hobby_venue", "commercial" },
                new[] { "creative_terminal", "lemma_terminal" }, "need_self_actualization", "dream.self_actualization"),
        };

        static NeedAspectEntry Entry(
            string id, string name, int tier, string[] features, string[] zones, string[] props,
            string[] devices, string slot, string lemma) => new NeedAspectEntry
        {
            aspectId = id,
            displayName = name,
            tier = tier,
            societyFeatures = features,
            zoneIds = zones,
            propertyClasses = props,
            deviceKinds = devices,
            spatialSlotId = slot,
            lemmaEntryHint = lemma
        };
    }
}
