using UnityEngine;

namespace SystemDrawer.Quest
{
    public enum QuestMapProjectionAxis
    {
        XZ,
        XY,
        YZ
    }

    [CreateAssetMenu(fileName = "QuestMapProfile", menuName = "System Drawer/Quest Map Profile")]
    public class QuestMapProfile : ScriptableObject
    {
        public QuestMapProjectionAxis projectionAxis = QuestMapProjectionAxis.XZ;
        [Range(1, 8)] public int emergenceLayerCount = 3;
        public Color occupancyColor = new Color(0.2f, 0.5f, 0.9f, 0.85f);
        public Color causalColor = new Color(1f, 0.85f, 0.2f, 0.6f);
        public Color emergenceTint = new Color(0.6f, 0.3f, 0.9f, 0.35f);
        public int textureWidth = 256;
        public int textureHeight = 256;
        [Min(0.1f)] public float refreshHz = 4f;
        public float orthoSize = 20f;
    }
}
