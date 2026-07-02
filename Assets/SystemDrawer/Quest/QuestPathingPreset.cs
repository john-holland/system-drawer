using System;
using System.Collections.Generic;
using UnityEngine;

namespace SystemDrawer.Quest
{
    [CreateAssetMenu(fileName = "QuestPathingPreset", menuName = "System Drawer/Quest Pathing Preset")]
    public class QuestPathingPreset : ScriptableObject
    {
        public string presetKey;
        public TravelAgentMultibodySettings multibodySettings = new TravelAgentMultibodySettings();
        public List<TravelAuthoringRow> authoringRows = new List<TravelAuthoringRow>();
    }
}
