using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Routes paint instrument channel pulses into brush tip / wrist / tool targets.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Instrument Proxy")]
public sealed class PaintInstrumentProxy : MonoBehaviour
{
    public PaintInstrumentMap sourceMap;
    public Transform brushTip;
    public Transform brushShaft;
    public Transform wristTarget;
    public Transform canvasPlane;
    [Range(0.01f, 2f)] public float tipMoveGain = 0.35f;
    [Range(0.1f, 180f)] public float rotateDegPerUnit = 45f;

    readonly Dictionary<string, float> _channels = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public float GetChannel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0f;
        return _channels.TryGetValue(id, out float v) ? v : 0f;
    }

    public void SetChannel(string id, float value)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (sourceMap != null && !sourceMap.ChannelIsAllowed(id))
            return;
        _channels[id] = value;
    }

    public bool TryFirePulse(string channelId, float amount)
    {
        if (sourceMap != null && !sourceMap.ChannelIsAllowed(channelId))
            return false;
        float cur = GetChannel(channelId);
        SetChannel(channelId, Mathf.Clamp(cur + amount, -1f, 1f));
        return true;
    }

    public void RouteAxes(float yaw, float pitch, float roll, float press, float twist)
    {
        SetChannel(PaintInstrumentMap.BrushYaw, yaw);
        SetChannel(PaintInstrumentMap.BrushPitch, pitch);
        SetChannel(PaintInstrumentMap.BrushRoll, roll);
        SetChannel(PaintInstrumentMap.BrushPress, press);
        SetChannel(PaintInstrumentMap.BrushTwist, twist);
    }

    void LateUpdate()
    {
        ApplyToTargets(Time.deltaTime);
    }

    public void ApplyToTargets(float dt)
    {
        float yaw = GetChannel(PaintInstrumentMap.BrushYaw);
        float pitch = GetChannel(PaintInstrumentMap.BrushPitch);
        float roll = GetChannel(PaintInstrumentMap.BrushRoll);
        float press = GetChannel(PaintInstrumentMap.BrushPress);
        float twist = GetChannel(PaintInstrumentMap.BrushTwist);

        if (brushTip != null && canvasPlane != null)
        {
            Vector3 right = canvasPlane.right;
            Vector3 up = canvasPlane.up;
            Vector3 delta = (right * yaw + up * pitch) * tipMoveGain;
            brushTip.position += delta;
            // Press pushes along -canvas normal
            brushTip.position += -canvasPlane.forward * press * tipMoveGain * 0.25f;
        }

        if (brushShaft != null)
        {
            float deg = rotateDegPerUnit * dt;
            brushShaft.Rotate(brushShaft.forward, twist * deg, Space.World);
            brushShaft.Rotate(brushShaft.right, pitch * deg * 0.25f, Space.World);
            brushShaft.Rotate(brushShaft.up, yaw * deg * 0.25f, Space.World);
            if (Mathf.Abs(roll) > 1e-4f)
                brushShaft.Rotate(brushShaft.forward, roll * deg, Space.World);
        }

        if (wristTarget != null && brushShaft != null)
            wristTarget.rotation = Quaternion.Slerp(wristTarget.rotation, brushShaft.rotation, 1f - Mathf.Exp(-8f * dt));
    }

    public void DecayChannels(float dt, float rate = 2f)
    {
        if (_channels.Count == 0) return;
        var keys = new List<string>(_channels.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string k = keys[i];
            float v = _channels[k];
            _channels[k] = Mathf.MoveTowards(v, 0f, rate * dt);
        }
    }
}
