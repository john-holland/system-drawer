using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Elemental
{
    public sealed class ElementalCompositionRulesEngine
    {
        readonly List<ElementalRule> _rules = new List<ElementalRule>();

        public void SetRules(IEnumerable<ElementalRule> rules)
        {
            _rules.Clear();
            if (rules == null)
                return;
            foreach (var r in rules)
            {
                if (r != null)
                    _rules.Add(r);
            }
        }

        public MineralStack RegressToMinerals(MaterialSpec spec)
        {
            var accum = new Dictionary<string, float>();
            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule == null || !rule.Matches(spec))
                    continue;
                var outs = rule.outputWeights;
                if (outs == null)
                    continue;
                for (int j = 0; j < outs.Length; j++)
                {
                    string id = outs[j].mineralId;
                    if (string.IsNullOrEmpty(id))
                        continue;
                    accum.TryGetValue(id, out float w);
                    accum[id] = w + outs[j].weight;
                }
            }
            if (accum.Count == 0)
            {
                accum["silicate"] = 1f;
            }
            float sum = 0f;
            foreach (var kv in accum)
                sum += kv.Value;
            var list = new List<MineralWeight>();
            foreach (var kv in accum)
                list.Add(new MineralWeight { mineralId = kv.Key, weight = sum > 0f ? kv.Value / sum : 0f });
            return new MineralStack { weights = list.ToArray() };
        }
    }
}
