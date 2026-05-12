using UnityEngine;

/// <summary>
/// Registers a <see cref="BaseAmbulatingActor"/> on the same GameObject with <see cref="SystemDrawerService"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class AmbulatingActorRegistrar : MonoBehaviour
{
    [Tooltip("Key passed to SystemDrawerService.Register")]
    public string serviceRegistrationKey;

    BaseAmbulatingActor _actor;

    void Awake()
    {
        _actor = GetComponent<BaseAmbulatingActor>();
    }

    void OnEnable()
    {
        if (string.IsNullOrEmpty(serviceRegistrationKey) || _actor == null)
            return;
        SystemDrawerService svc = SystemDrawerService.Instance != null ? SystemDrawerService.Instance : SystemDrawerService.FindInScene();
        svc?.Register(serviceRegistrationKey, _actor);
    }

    void OnDisable()
    {
        if (string.IsNullOrEmpty(serviceRegistrationKey))
            return;
        SystemDrawerService svc = SystemDrawerService.Instance != null ? SystemDrawerService.Instance : SystemDrawerService.FindInScene();
        svc?.Unregister(serviceRegistrationKey);
    }
}
