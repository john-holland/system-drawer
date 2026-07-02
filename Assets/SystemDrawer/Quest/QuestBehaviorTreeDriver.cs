using Locomotion.Narrative;
using UnityEngine;

namespace SystemDrawer.Quest
{
    /// <summary>Ticks quest UI / map / animation behavior trees and writes map profile overrides.</summary>
    public class QuestBehaviorTreeDriver : MonoBehaviour
    {
        public QuestRunner questRunner;
        public QuestBehaviorTreeBundle bundle;
        public QuestMapProfile mapProfileOverride;
        public QuestMapRenderer mapRenderer;

        void Awake()
        {
            if (questRunner == null)
                questRunner = FindAnyObjectByType<QuestRunner>();
            if (mapRenderer == null)
                mapRenderer = FindAnyObjectByType<QuestMapRenderer>();
        }

        void Update()
        {
            if (bundle == null || mapProfileOverride == null || mapRenderer == null)
                return;
            if (mapRenderer.profile != mapProfileOverride)
                mapRenderer.profile = mapProfileOverride;
            mapRenderer.RenderSlice();
        }

        public void ApplyPathingPreset(QuestPathingPreset preset, TravelAgent agent)
        {
            if (preset == null || agent == null)
                return;
            agent.multibody = preset.multibodySettings;
        }
    }
}
