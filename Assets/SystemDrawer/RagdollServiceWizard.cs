using UnityEngine;

/// <summary>
/// Service wizard component for Ragdoll Fitting. Add to any GameObject; use slots and buttons to open the ragdoll wizard or create examples.
/// Registers with SystemDrawerService when present. Use TryCompleteFromService to fill slot from the conglomerator.
/// </summary>
public class RagdollServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "RagdollRoot";

    [Tooltip("Ragdoll actor or root this wizard configures.")]
    public Transform ragdollRoot;

    [Tooltip("When set, also register this ragdoll with the drawer under this key (e.g. \"player\" or \"bear\") so narrative position keys resolve to the bear.")]
    public string alsoRegisterAsPlayerKey = "player";

    /// <summary>Assign slot from SystemDrawerService if empty. Returns true if assigned.</summary>
    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        var tr = service.Get<Transform>(ServiceKey);
        if (tr != null)
        {
            ragdollRoot = tr;
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (SystemDrawerService.Instance == null) return;
        if (ragdollRoot != null)
        {
            SystemDrawerService.Instance.Register(ServiceKey, ragdollRoot);
            if (!string.IsNullOrWhiteSpace(alsoRegisterAsPlayerKey))
                SystemDrawerService.Instance.Register(alsoRegisterAsPlayerKey.Trim(), ragdollRoot.gameObject);
        }
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance == null) return;
        SystemDrawerService.Instance.Unregister(ServiceKey);
        if (!string.IsNullOrWhiteSpace(alsoRegisterAsPlayerKey))
            SystemDrawerService.Instance.Unregister(alsoRegisterAsPlayerKey.Trim());
    }
}
