using UnityEngine;

/// <summary>Facilitator wizard for MenuRagdoll main menu.</summary>
[AddComponentMenu("System Drawer/Networking/Menu Ragdoll Service Wizard")]
public sealed class MenuRagdollServiceWizard : MonoBehaviour
{
    public const string ServiceKey = SystemDrawerServiceKeys.MenuRagdoll;

    public MenuRagdollBase menuRagdoll;
    public MainMenuSpatialGenerator menuGenerator;

    public bool TryCompleteFromService()
    {
        var svc = SystemDrawerService.Instance;
        if (svc == null)
            return false;
        if (menuRagdoll == null)
            menuRagdoll = svc.Get<MenuRagdollBase>(ServiceKey);
        return menuRagdoll != null;
    }

    void Awake()
    {
        if (menuRagdoll == null)
            menuRagdoll = GetComponentInChildren<MenuRagdollBase>();
        if (menuGenerator == null)
            menuGenerator = GetComponentInChildren<MainMenuSpatialGenerator>();
    }

    void OnEnable()
    {
        if (menuRagdoll != null && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(ServiceKey, menuRagdoll);
    }

    void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(ServiceKey);
    }
}
