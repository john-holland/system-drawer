using UnityEngine;

/// <summary>Mesh-collider press/hit that fires a lemma and optional pedestrian call.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic/Road Component Mesh Activator")]
public sealed class RoadComponentMeshActivator : MonoBehaviour
{
    public TrafficLightController target;
    public MonoBehaviour boundTarget;
    public bool lastPressed;
    public PixelLightRig flashRig;

    void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.2f, 0.2f, 0.08f);
        }
        if (GetComponent<RoadLaneLemmaResolver>() == null)
            gameObject.AddComponent<RoadLaneLemmaResolver>().placeholderName = RoadLaneLemmaPropertyKeys.WalkButton;
    }

    public bool TryPress()
    {
        lastPressed = true;
        if (target == null && boundTarget is TrafficLightController ctrl)
            target = ctrl;
        if (target != null)
            target.pedestrianCall = true;
        flashRig?.SetEnabledEmission(true);
        SendMessage("OnWalkButtonPressed", this, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    void OnCollisionEnter(Collision _) => TryPress();
    void OnTriggerEnter(Collider _) => TryPress();
}
