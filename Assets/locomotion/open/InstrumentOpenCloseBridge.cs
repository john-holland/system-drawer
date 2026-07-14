using Locomotion.Audio;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Wires Open plan/driver open01 + beat bus into Audio instrument case / attenuation consumers
    /// without creating an Audio → Open assembly cycle.
    /// </summary>
    public sealed class InstrumentOpenCloseBridge : MonoBehaviour
    {
        public ObjectOpenCloseTopologyPlanNode plan;
        public OpenCloseTopologyAsset topology;
        public OpenableJointDriver lidDriver;
        public OpenableJointDriver fallboardDriver;
        public InstrumentCaseTopology caseTopology;
        public AttenuatedOpenClose attenuatedOpen;
        public KeyboardInstrumentSim keyboard;
        public bool subscribeBeatBus = true;
        public bool syncEveryFrame = true;

        void OnEnable()
        {
            if (subscribeBeatBus)
                OpenCloseBeatMessageBus.Raised += OnBeatMessage;
        }

        void OnDisable()
        {
            if (subscribeBeatBus)
                OpenCloseBeatMessageBus.Raised -= OnBeatMessage;
        }

        void Update()
        {
            if (!syncEveryFrame)
                return;
            SyncFromDrivers();
        }

        public void SyncFromDrivers()
        {
            if (lidDriver != null)
            {
                float open01 = lidDriver.Open01;
                caseTopology?.SyncFromOpen01(open01);
                attenuatedOpen?.SyncFromOpen01(open01);
                keyboard?.SyncLidFromOpen01(open01);
            }

            if (fallboardDriver != null)
                keyboard?.SyncFallboardFromOpen01(fallboardDriver.Open01);
        }

        public void BakePlan()
        {
            var asset = topology;
            if (asset == null && caseTopology != null)
                asset = caseTopology.caseOpenCloseTopology as OpenCloseTopologyAsset;
            if (asset == null && keyboard != null)
                asset = keyboard.keyboardOpenCloseTopology as OpenCloseTopologyAsset;
            if (asset == null || plan == null)
                return;
            plan.topology = asset;
            plan.BakeFromTopology();
        }

        void OnBeatMessage(OpenCloseBeatMessage msg)
        {
            if (caseTopology == null || !caseTopology.enableInstrumentCaseTopology)
                return;
            if (msg.phase == OpenCloseBeatPhase.Open)
                caseTopology.SetCaseOpen(true);
            else if (msg.phase == OpenCloseBeatPhase.Close)
                caseTopology.SetCaseOpen(false);
        }
    }
}
