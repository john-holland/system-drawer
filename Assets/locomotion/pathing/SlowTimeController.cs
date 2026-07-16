using UnityEngine;

/// <summary>Restore-safe gameplay time scale (slow / stop). Not convoy TravelPaceMode.</summary>
public sealed class SlowTimeController : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = stop time, 1 = full speed.")]
    public float timeScaleCoefficient = 0.25f;

    float _savedScale = 1f;
    float _savedFixed = 0.02f;
    bool _active;

    public bool IsActive => _active;

    public void Enter(float coefficient)
    {
        timeScaleCoefficient = Mathf.Clamp01(coefficient);
        if (!_active)
        {
            _savedScale = Time.timeScale;
            _savedFixed = Time.fixedDeltaTime;
            _active = true;
        }
        Apply();
    }

    public void Enter() => Enter(timeScaleCoefficient);

    public void Exit()
    {
        if (!_active) return;
        Time.timeScale = _savedScale > 0f ? _savedScale : 1f;
        Time.fixedDeltaTime = _savedFixed > 0f ? _savedFixed : 0.02f;
        _active = false;
    }

    public void SetCoefficient(float coefficient)
    {
        timeScaleCoefficient = Mathf.Clamp01(coefficient);
        if (_active) Apply();
    }

    void Apply()
    {
        Time.timeScale = timeScaleCoefficient;
        // Keep fixed step proportional so physics doesn't explode when slowing.
        Time.fixedDeltaTime = Mathf.Max(0.0001f, 0.02f * Mathf.Max(timeScaleCoefficient, 0.0001f));
    }

    void OnDisable()
    {
        Exit();
    }
}
