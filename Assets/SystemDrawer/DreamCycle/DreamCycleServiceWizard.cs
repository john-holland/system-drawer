using UnityEngine;

namespace SystemDrawer.DreamCycle
{
    /// <summary>Registers dream cycle services with SystemDrawerService.</summary>
    public class DreamCycleServiceWizard : MonoBehaviour
    {
        public const string DayRunnerKey = "DreamDayCycleRunner";
        public const string SleepRendererKey = "SleepWaveStatRenderer";

        public DreamDayCycleRunner dayRunner;
        public DreamNightCycleRunner nightRunner;
        public SleepWaveStatRenderer sleepRenderer;
        public MonoBehaviour dreamMemoryLstm;

        void OnEnable() => RegisterAll();
        void OnDisable() => UnregisterAll();

        public void RegisterAll()
        {
            if (SystemDrawerService.Instance == null)
                return;
            if (dayRunner != null)
                SystemDrawerService.Instance.Register(DayRunnerKey, dayRunner);
            if (nightRunner != null)
                SystemDrawerService.Instance.Register("DreamNightCycleRunner", nightRunner);
            if (sleepRenderer != null)
                SystemDrawerService.Instance.Register(SleepRendererKey, sleepRenderer);
            if (dreamMemoryLstm != null)
                SystemDrawerService.Instance.Register("DreamMemoryLSTM", dreamMemoryLstm);
        }

        void UnregisterAll()
        {
            if (SystemDrawerService.Instance == null)
                return;
            SystemDrawerService.Instance.Unregister(DayRunnerKey);
            SystemDrawerService.Instance.Unregister("DreamNightCycleRunner");
            SystemDrawerService.Instance.Unregister(SleepRendererKey);
            SystemDrawerService.Instance.Unregister("DreamMemoryLSTM");
        }
    }
}
