using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Piano/keyboard: key sets, pedals, fallboard/lid open state, optional case topology.
    /// Assign OpenCloseTopologyAsset via <see cref="keyboardOpenCloseTopology"/>; bake from Open bridge.
    /// </summary>
    public sealed class KeyboardInstrumentSim : PhysicalInstrumentBase
    {
        public int keyCount = 88;
        [Range(0f, 1f)] public float sustainPedal01;
        [Range(0f, 1f)] public float softPedal01;
        public bool fallboardOpen = true;
        public bool lidOpen = true;
        /// <summary>Optional OpenCloseTopologyAsset (kept untyped to avoid Audio→Open assembly cycle).</summary>
        public ScriptableObject keyboardOpenCloseTopology;
        public bool usePercussionPrebakeForHammers = true;

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.Keyboard;
        }

        public void SetFallboardOpen(bool open) => fallboardOpen = open;
        public void SetLidOpen(bool open) => lidOpen = open;

        public void SyncLidFromOpen01(float open01) => lidOpen = open01 >= 0.5f;
        public void SyncFallboardFromOpen01(float open01) => fallboardOpen = open01 >= 0.5f;

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            if (!fallboardOpen && controlId == "key")
                raw01 *= 0.15f;
            var dsp = base.BuildVoice(controlId, raw01, bpm);
            dsp.amplitudeEnvelope = new Vector4(
                dsp.amplitudeEnvelope.x,
                dsp.amplitudeEnvelope.y,
                Mathf.Clamp01(dsp.amplitudeEnvelope.z + sustainPedal01 * 0.3f),
                dsp.amplitudeEnvelope.w);
            if (lidOpen)
                dsp.reverbAmount = Mathf.Clamp01(dsp.reverbAmount + 0.15f);
            else
                dsp.filterCutoff *= 0.85f;
            if (usePercussionPrebakeForHammers)
                dsp.amplitudeEnvelope = new Vector4(0.01f, dsp.amplitudeEnvelope.y, dsp.amplitudeEnvelope.z, dsp.amplitudeEnvelope.w);
            _ = softPedal01;
            _ = keyboardOpenCloseTopology;
            return dsp;
        }
    }
}
