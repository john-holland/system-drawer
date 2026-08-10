using UnityEngine;

/// <summary>Soft AABB/sphere: TravelAgent may land anywhere inside.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Helicopter/Helipad Anywhere Bounds")]
public sealed class HelipadAnywhereBounds : MonoBehaviour
{
    public enum Shape
    {
        Box = 0,
        Sphere = 1
    }

    public Shape shape = Shape.Box;
    public Vector3 boxSize = new Vector3(60f, 20f, 60f);
    public float sphereRadius = 40f;
    public RoadLot preferredRoadLot;
    public ParkingLot preferredParkingLot;

    public Bounds GetWorldBounds()
    {
        if (shape == Shape.Sphere)
            return new Bounds(transform.position, Vector3.one * sphereRadius * 2f);
        return new Bounds(transform.position, Vector3.Scale(boxSize, transform.lossyScale));
    }

    public bool Contains(Vector3 world)
    {
        if (shape == Shape.Sphere)
            return (world - transform.position).sqrMagnitude <= sphereRadius * sphereRadius;
        return GetWorldBounds().Contains(world);
    }

    public Vector3 PickLandingPoint(Vector3 near)
    {
        Bounds b = GetWorldBounds();
        Vector3 p = near;
        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
        p.y = b.center.y;
        if (preferredRoadLot != null)
            p.y = preferredRoadLot.SampleHeight(p);
        return p;
    }
}
