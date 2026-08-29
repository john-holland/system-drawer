using System;
using UnityEngine;

/// <summary>
/// Courtroom gallery seat BT: occupant anchors follow <see cref="AngleBase3D"/> gallery cells.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Courtroom Seat BT")]
public sealed class CourtroomSeatBt : MonoBehaviour
{
    public AngleBase3D[] galleryBases = Array.Empty<AngleBase3D>();
    public Transform[] occupantAnchors = Array.Empty<Transform>();
    public VehicleSeating seating;

    void Awake()
    {
        RebuildAnchors();
    }

    public void RebuildAnchors()
    {
        if (galleryBases == null || galleryBases.Length == 0)
            galleryBases = GetComponentsInChildren<AngleBase3D>(true);
        if (galleryBases == null)
            galleryBases = Array.Empty<AngleBase3D>();
        occupantAnchors = new Transform[galleryBases.Length];
        for (int i = 0; i < galleryBases.Length; i++)
        {
            var b = galleryBases[i];
            if (b == null) continue;
            occupantAnchors[i] = b.transform;
            b.ApplyTo(b.transform);
        }
        if (seating == null)
            seating = GetComponent<VehicleSeating>();
        if (seating != null)
            seating.occupantAnchors = occupantAnchors;
    }

    public AngleBase3D FirstGalleryBase()
    {
        if (galleryBases == null) return null;
        for (int i = 0; i < galleryBases.Length; i++)
            if (galleryBases[i] != null)
                return galleryBases[i];
        return null;
    }
}
