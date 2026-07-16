using System;
using UnityEngine;

/// <summary>
/// Optional bio-rhythm / BP seated walk reminder. Timer starts only while player input is idle;
/// any input resets debounce and pauses. Min/max random range (set equal for stable expectation).
/// </summary>
[Serializable]
public sealed class SeatedWalkReminderPolicy
{
    public bool enabled = true;
    public float timerMinSeconds = 600f;
    public float timerMaxSeconds = 600f;
    public float idleDebounceSeconds = 2f;
    public float hypertensiveSysThreshold = 135f;
    public float hypertensiveDiaThreshold = 85f;
    public bool requireHypertensiveLoad;

    float _idleAccum;
    float _timer;
    float _chosenDuration = -1f;
    bool _fired;

    public float IdleAccum => _idleAccum;
    public float TimerElapsed => _timer;
    public float ChosenDuration => _chosenDuration;
    public bool Fired => _fired;

    public void Reset()
    {
        _idleAccum = 0f;
        _timer = 0f;
        _chosenDuration = -1f;
        _fired = false;
    }

    public void NotifyPlayerInput()
    {
        _idleAccum = 0f;
        _timer = 0f;
        _chosenDuration = -1f;
    }

    /// <summary>
    /// Tick policy. Returns true once when reminder should fire (stand → walk → re-sit).
    /// </summary>
    public bool Tick(float dt, bool playerInputActive, bool isSeated, LifeSystemsSheet sheet)
    {
        if (!enabled || !isSeated || _fired)
            return false;

        if (playerInputActive)
        {
            NotifyPlayerInput();
            return false;
        }

        _idleAccum += dt;
        if (_idleAccum < idleDebounceSeconds)
            return false;

        if (_chosenDuration < 0f)
            _chosenDuration = PickDuration();

        if (!PassesBloodPressure(sheet))
            return false;

        _timer += dt;
        if (_timer < _chosenDuration)
            return false;

        _fired = true;
        return true;
    }

    public void AcknowledgeHandled()
    {
        _fired = false;
        _timer = 0f;
        _chosenDuration = -1f;
        _idleAccum = idleDebounceSeconds;
    }

    float PickDuration()
    {
        float min = Mathf.Min(timerMinSeconds, timerMaxSeconds);
        float max = Mathf.Max(timerMinSeconds, timerMaxSeconds);
        if (Mathf.Approximately(min, max))
            return min;
        return UnityEngine.Random.Range(min, max);
    }

    bool PassesBloodPressure(LifeSystemsSheet sheet)
    {
        if (sheet == null)
            return !requireHypertensiveLoad;
        float sys = sheet.BloodPressureSys;
        float dia = sheet.BloodPressureDia;
        bool elevated = sys >= hypertensiveSysThreshold || dia >= hypertensiveDiaThreshold;
        if (requireHypertensiveLoad)
        {
            float load = sheet.Get01(LifeSystemsChannelCatalog.HypertensiveLoad);
            return elevated || load > 0.15f;
        }
        // Optional bio-rhythm path: timer + idle gate; BP available for authoring but not required.
        return true;
    }
}
