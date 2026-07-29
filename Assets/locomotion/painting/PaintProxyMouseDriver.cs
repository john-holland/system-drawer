using UnityEngine;

/// <summary>
/// Samples desk MousePeripheralDriver or a handheld proxy-mouse transform into paint instrument channels.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Proxy Mouse Driver")]
public sealed class PaintProxyMouseDriver : MonoBehaviour
{
    public PaintInstrumentProxy proxy;
    public MousePeripheralDriver mousePeripheral;
    public Transform proxyMouse;
    public Transform canvasPlane;
    [Tooltip("When true, use Input axes in addition to peripheral/proxy transforms.")]
    public bool useKeyboardAxes = true;
    public float keyboardGain = 1f;
    public float twistScrollGain = 0.15f;

    Vector3 _lastMouseWorld;
    bool _hasLast;

    void Awake()
    {
        if (proxy == null)
            proxy = GetComponent<PaintInstrumentProxy>();
        if (proxy != null && proxy.sourceMap != null)
            proxy.sourceMap.EnsureDefaults();
    }

    void Update()
    {
        if (proxy == null) return;
        float yaw = 0f, pitch = 0f, roll = 0f, press = 0f, twist = 0f;

        if (mousePeripheral != null && mousePeripheral.mouseAnchor != null)
        {
            Vector3 w = mousePeripheral.mouseAnchor.position;
            if (_hasLast)
            {
                Vector3 d = w - _lastMouseWorld;
                if (canvasPlane != null)
                {
                    yaw += Vector3.Dot(d, canvasPlane.right);
                    pitch += Vector3.Dot(d, canvasPlane.up);
                }
                else
                {
                    yaw += d.x;
                    pitch += d.y;
                }
            }
            _lastMouseWorld = w;
            _hasLast = true;
            mousePeripheral.worldPoint = w;
        }
        else if (proxyMouse != null)
        {
            Vector3 local = proxyMouse.localPosition;
            yaw += local.x;
            pitch += local.y;
            roll += proxyMouse.localEulerAngles.z / 180f - 1f;
            twist += proxyMouse.localEulerAngles.y / 180f - 1f;
        }

        if (useKeyboardAxes)
        {
            yaw += Input.GetAxis("Horizontal") * keyboardGain * Time.deltaTime;
            pitch += Input.GetAxis("Vertical") * keyboardGain * Time.deltaTime;
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * keyboardGain * 0.05f;
                pitch += Input.GetAxis("Mouse Y") * keyboardGain * 0.05f;
            }
            twist += Input.GetAxis("Mouse ScrollWheel") * twistScrollGain;
            if (Input.GetKey(KeyCode.Q)) roll -= keyboardGain * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) roll += keyboardGain * Time.deltaTime;
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
                press = 1f;
        }

        proxy.RouteAxes(
            Mathf.Clamp(yaw, -1f, 1f),
            Mathf.Clamp(pitch, -1f, 1f),
            Mathf.Clamp(roll, -1f, 1f),
            Mathf.Clamp01(press),
            Mathf.Clamp(twist, -1f, 1f));
    }
}
