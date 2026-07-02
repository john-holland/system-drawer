using Locomotion.Narrative;
using UnityEngine;

namespace SystemDrawer.Quest
{
    [CreateAssetMenu(fileName = "QuestBehaviorTreeBundle", menuName = "System Drawer/Quest Behavior Tree Bundle")]
    public class QuestBehaviorTreeBundle : ScriptableObject
    {
        public NarrativeTreeAsset uiTree;
        public NarrativeTreeAsset mapDisplayTree;
        public NarrativeTreeAsset animationTree;
        public string uiParamsJson = "{}";
        public string mapParamsJson = "{}";
        public string animParamsJson = "{}";
    }
}
