using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Generates drink-specific tool usage cards respecting lemma property flags.</summary>
    public static class DrinkToolUsageCardGenerator
    {
        public static List<GoodSection> Generate(
            Consider consider,
            GameObject tool,
            string task,
            RagdollState state,
            DrinkLemmaProperties props)
        {
            var cards = new List<GoodSection>();
            if (consider == null || tool == null)
                return cards;

            cards.Add(consider.GenerateApproachCard(tool));
            cards.Add(consider.GenerateGraspCard(tool));
            cards.Add(consider.GenerateOrientCard(tool));

            if (props.partiallyRaiseAmount < 1f - 1e-4f)
            {
                var raise = consider.GenerateUseCard(tool, task);
                raise.sectionName = $"raise_{tool.name}";
                raise.description = $"Raise {tool.name} ({props.partiallyRaiseAmount:P0})";
                cards.Add(raise);
            }

            bool suppressSips = props.SuppressDispense ||
                                (props.partiallyRaiseAmount < 1f - 1e-4f && props.closureMode == DrinkClosureMode.Stalled);

            if (!suppressSips)
            {
                int sipCount = props.sipCount > 0 ? props.sipCount : 1;
                for (int i = 0; i < sipCount; i++)
                {
                    var sip = consider.GenerateUseCard(tool, task);
                    sip.sectionName = $"sip_{tool.name}_{i + 1}";
                    sip.description = $"Sip {i + 1}/{sipCount} from {tool.name}";
                    sip.repeatCount = 1;
                    cards.Add(sip);
                }
            }

            if (!props.putWithoutRelease)
            {
                var release = consider.GenerateReleaseCard(tool);
                release.skipRelease = false;
                cards.Add(release);
            }

            if (props.holdWithoutReturn)
            {
                foreach (var c in cards)
                    c.skipReturn = true;
            }

            return cards;
        }

        public static int CountSipCards(IReadOnlyList<GoodSection> cards)
        {
            if (cards == null) return 0;
            int n = 0;
            foreach (var c in cards)
            {
                if (c != null && c.sectionName != null && c.sectionName.StartsWith("sip_"))
                    n++;
            }
            return n;
        }
    }
}
