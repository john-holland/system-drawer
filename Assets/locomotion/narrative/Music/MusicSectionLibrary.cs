using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [CreateAssetMenu(fileName = "MusicSectionLibrary", menuName = "Locomotion/Narrative/Music Section Library", order = 11)]
    public sealed class MusicSectionLibrary : ScriptableObject
    {
        public List<MusicSectionAsset> sections = new List<MusicSectionAsset>();

        readonly Dictionary<string, MusicSectionAsset> _byId = new Dictionary<string, MusicSectionAsset>();

        public void RebuildIndex()
        {
            _byId.Clear();
            for (int i = 0; i < sections.Count; i++)
            {
                MusicSectionAsset s = sections[i];
                if (s == null) continue;
                _byId[s.StableId] = s;
            }
        }

        public bool TryGet(string sectionId, out MusicSectionAsset asset)
        {
            if (_byId.Count == 0) RebuildIndex();
            return _byId.TryGetValue(sectionId, out asset);
        }

        public List<MusicSectionAsset> Query(MusicStemRole role, int harmonicHue, float energy, float energyBand = 0.25f)
        {
            var results = new List<MusicSectionAsset>();
            for (int i = 0; i < sections.Count; i++)
            {
                MusicSectionAsset s = sections[i];
                if (s == null || s.stemRole != role) continue;
                int hueDiff = Mathf.Abs(s.harmonicHue - harmonicHue);
                hueDiff = Mathf.Min(hueDiff, 12 - hueDiff);
                if (hueDiff > 2) continue;
                if (Mathf.Abs(s.energy - energy) > energyBand) continue;
                results.Add(s);
            }
            return results;
        }

        public List<MusicSectionAsset> AllExcept(IEnumerable<string> excludeIds)
        {
            var exclude = new HashSet<string>();
            if (excludeIds != null)
            {
                foreach (string id in excludeIds)
                    if (!string.IsNullOrEmpty(id)) exclude.Add(id);
            }
            var list = new List<MusicSectionAsset>();
            for (int i = 0; i < sections.Count; i++)
            {
                MusicSectionAsset s = sections[i];
                if (s != null && !exclude.Contains(s.StableId))
                    list.Add(s);
            }
            return list;
        }
    }
}
