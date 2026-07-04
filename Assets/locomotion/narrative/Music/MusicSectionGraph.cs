using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>A* pathing over music sections (mirrors TemporalGraph pattern).</summary>
    public sealed class MusicSectionGraph
    {
        readonly Dictionary<string, List<string>> _edges = new Dictionary<string, List<string>>();
        readonly Dictionary<(string, string), float> _weights = new Dictionary<(string, string), float>();
        readonly Dictionary<string, MusicSectionAsset> _nodes = new Dictionary<string, MusicSectionAsset>();

        public void AddNode(MusicSectionAsset section)
        {
            if (section == null) return;
            string id = section.StableId;
            if (!_nodes.ContainsKey(id))
            {
                _nodes[id] = section;
                _edges[id] = new List<string>();
            }
        }

        public void AddEdge(string fromId, string toId, float weight = 1f)
        {
            if (!_edges.ContainsKey(fromId))
                _edges[fromId] = new List<string>();
            if (!_edges[fromId].Contains(toId))
                _edges[fromId].Add(toId);
            _weights[(fromId, toId)] = weight;
        }

        public List<MusicSectionAsset> FindPath(
            string startId,
            string goalId,
            TransitionScorer scorer,
            RhythmMeterTemplate rhythm,
            ModulationSavingsBank bank)
        {
            if (!_nodes.TryGetValue(startId, out _) || !_nodes.TryGetValue(goalId, out _))
                return new List<MusicSectionAsset>();

            var open = new List<(string id, float cost, List<string> path)>();
            var closed = new HashSet<string>();
            open.Add((startId, 0f, new List<string> { startId }));

            while (open.Count > 0)
            {
                var current = open.OrderBy(x => x.cost).First();
                open.Remove(current);

                if (current.id == goalId)
                    return current.path.Select(id => _nodes[id]).ToList();

                closed.Add(current.id);

                if (!_edges.TryGetValue(current.id, out List<string> neighbors))
                    continue;

                MusicSectionAsset fromSection = _nodes[current.id];
                foreach (string neighbor in neighbors)
                {
                    if (closed.Contains(neighbor)) continue;
                    MusicSectionAsset toSection = _nodes[neighbor];
                    float edgeW = _weights.TryGetValue((current.id, neighbor), out float w) ? w : 1f;
                    float h = scorer != null
                        ? scorer.Score(fromSection, toSection, rhythm, rhythm, bank)
                        : 1f;
                    float total = current.cost + edgeW + h;

                    var existing = open.FirstOrDefault(x => x.id == neighbor);
                    if (existing.id != null && existing.cost <= total) continue;
                    if (existing.id != null) open.Remove(existing);

                    var newPath = new List<string>(current.path) { neighbor };
                    open.Add((neighbor, total, newPath));
                }
            }

            return new List<MusicSectionAsset>();
        }

        public MusicSectionAsset PickBestNext(
            MusicSectionAsset current,
            IEnumerable<MusicSectionAsset> candidates,
            IEnumerable<string> excludeIds,
            RhythmMeterTemplate rhythmFrom,
            RhythmMeterTemplate rhythmTo,
            TransitionScorer scorer,
            ModulationSavingsBank bank)
        {
            var exclude = new HashSet<string>();
            if (excludeIds != null)
                foreach (string id in excludeIds)
                    if (!string.IsNullOrEmpty(id)) exclude.Add(id);

            MusicSectionAsset best = null;
            float bestCost = float.MaxValue;

            foreach (MusicSectionAsset c in candidates)
            {
                if (c == null || exclude.Contains(c.StableId)) continue;
                float cost = scorer.Score(current, c, rhythmFrom, rhythmTo, bank);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = c;
                }
            }

            return best;
        }

        bool _libraryConnected;

        public void ConnectLibrary(MusicSectionLibrary library, float defaultWeight = 1f)
        {
            if (library?.sections == null || _libraryConnected) return;
            _libraryConnected = true;
            for (int i = 0; i < library.sections.Count; i++)
            {
                MusicSectionAsset a = library.sections[i];
                if (a == null) continue;
                AddNode(a);
                for (int j = 0; j < library.sections.Count; j++)
                {
                    MusicSectionAsset b = library.sections[j];
                    if (b == null || a == b) continue;
                    if (a.stemRole == b.stemRole)
                        AddEdge(a.StableId, b.StableId, defaultWeight);
                }
            }
        }
    }
}
