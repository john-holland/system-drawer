using UnityEngine;

/// <summary>
/// Keeps stairwell/gambit API on <see cref="MuscularFatigueAdrenalineState"/> in sync with life-systems channels.
/// </summary>
[AddComponentMenu("Locomotion/Muscular Fatigue Adrenaline Facade")]
[RequireComponent(typeof(MuscularFatigueAdrenalineState))]
public sealed class MuscularFatigueAdrenalineFacade : MonoBehaviour
{
    public MuscularFatigueAdrenalineState combatState;
    public LifeSystemsSheet sheet;
    public bool pushToSheet = true;
    public bool pullFromSheet;

    void Awake()
    {
        if (combatState == null)
            combatState = GetComponent<MuscularFatigueAdrenalineState>();
        if (sheet == null)
            sheet = GetComponent<LifeSystemsSheet>();
    }

    void LateUpdate()
    {
        if (combatState == null) return;
        if (sheet == null)
        {
            var svc = LifeSystemsServices.Instance;
            if (svc != null)
                sheet = svc.GetOrCreate(gameObject);
            else
                sheet = GetComponent<LifeSystemsSheet>();
        }
        if (sheet == null) return;
        sheet.EnsureDefaults();

        if (pullFromSheet)
        {
            combatState.adrenaline01 = sheet.Get01(LifeSystemsChannelCatalog.Adrenaline);
            combatState.fatigue01 = sheet.Get01(LifeSystemsChannelCatalog.Fatigue);
        }
        else if (pushToSheet)
        {
            sheet.Set01(LifeSystemsChannelCatalog.Adrenaline, combatState.adrenaline01);
            sheet.Set01(LifeSystemsChannelCatalog.Fatigue, combatState.fatigue01);
        }
    }
}
