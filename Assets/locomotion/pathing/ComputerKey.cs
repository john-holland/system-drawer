using System;
using UnityEngine;

/// <summary>One chiclet / knob on the computer keyboard.</summary>
[AddComponentMenu("Locomotion/Periphery/Computer Key")]
public sealed class ComputerKey : MonoBehaviour
{
    public ComputerKeyId id;
    public ComputerKeySection section;
    public string legend;
    public char unicode;
    public float unitWidth = 1f;
    public float unitHeight = 1f;
    public float travelMin;
    public float travelMax;
    public float minPressImpulse = 0.55f;
    public bool isKnob;
    public bool opensContextMenu;
    public Transform pressPoint;
    public Vector3 travelAxisLocal = Vector3.down;
    public Light legendLight;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public Vector3 WorldPressPoint =>
        pressPoint != null ? pressPoint.position : transform.position + transform.up * 0.005f;

    public Vector3 WorldTravelAxis => transform.TransformDirection(travelAxisLocal).normalized;

    public void ApplyPressDepth(float depth01)
    {
        float d = Mathf.Lerp(travelMin, travelMax, Mathf.Clamp01(depth01));
        transform.localPosition = _restLocal + travelAxisLocal.normalized * d;
        if (legendLight != null)
            legendLight.enabled = depth01 > 0.15f;
    }

    public void ResetPress()
    {
        transform.localPosition = _restLocal;
        if (legendLight != null)
            legendLight.enabled = false;
    }

    Vector3 _restLocal;

    void Awake()
    {
        _restLocal = transform.localPosition;
        if (pressPoint == null)
        {
            var go = new GameObject("PressPoint");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.002f;
            pressPoint = go.transform;
        }
        opensContextMenu = id == ComputerKeyId.Option;
    }

    public void CaptureRest() => _restLocal = transform.localPosition;
}
