using UnityEngine;

namespace SystemDrawer.Quest
{
    [CreateAssetMenu(fileName = "QuestSetAsset", menuName = "System Drawer/Quest Set Asset")]
    public class QuestSetAsset : ScriptableObject
    {
        public string setId;
        public string title;
        [TextArea(8, 24)] public string compiledJson;
        public QuestMapProfile defaultMapProfile;
    }
}
