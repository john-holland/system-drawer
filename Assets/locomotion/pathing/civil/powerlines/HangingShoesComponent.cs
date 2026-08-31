using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class Shoe
{
    public List<MonoBehaviour> laces = new List<MonoBehaviour>();
    public RopeSystem laceRope;
    public Transform eyelet;
    public MeshFilter meshFilter;
    public SkinnedMeshRenderer skinned;
    public Texture2D laceTexture;
    public Texture2D shoeTexture;
    public Material upperMaterial;
    public Material soleMaterial;
    public Material laceMaterial;
    public GameObject effectsPrefab;
    public PixelLightRig reflectiveStrip;
    public UnityEvent onDrape = new UnityEvent();
    public UnityEvent onCinch = new UnityEvent();
    public UnityEvent onSnap = new UnityEvent();
    public UnityEvent onHitGround = new UnityEvent();
    public AudioClip drapeClip;
    public AudioClip slapClip;
    public AudioClip scrapeClip;
    public BehaviorTree behaviorTree;
    public ParticleSystem slapDust;
}

/// <summary>Two shoes knotted over a street wire. Uses RopeSystem; stub Shoe fields only.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Powerlines/Hanging Shoes")]
public sealed class HangingShoesComponent : MonoBehaviour
{
    public Rigidbody leftShoeBody;
    public Rigidbody rightShoeBody;
    public Shoe leftShoe = new Shoe();
    public Shoe rightShoe = new Shoe();
    public RopeSystem laceRope;
    [Min(0.1f)] public float knotLengthM = 0.6f;
    public List<MonoBehaviour> laceSplines = new List<MonoBehaviour>();
    public StreetWireEnd wireEnd;
    [Range(0f, 1f)] public float drapeT01 = 0.5f;

    void Awake()
    {
        EnsureBodies();
        EnsureLaceRope();
    }

    public void BindWire(StreetWireEnd end)
    {
        wireEnd = end;
        EnsureBodies();
        EnsureLaceRope();
        Vector3 drape = end != null ? end.AttachWorld() : transform.position;
        if (leftShoeBody != null)
            leftShoeBody.transform.position = drape + Vector3.left * 0.15f + Vector3.down * 0.2f;
        if (rightShoeBody != null)
            rightShoeBody.transform.position = drape + Vector3.right * 0.15f + Vector3.down * 0.2f;
        leftShoe?.onDrape?.Invoke();
        rightShoe?.onDrape?.Invoke();
    }

    public void EnsureBodies()
    {
        if (leftShoeBody == null)
            leftShoeBody = EnsureChildBody("LeftShoe");
        if (rightShoeBody == null)
            rightShoeBody = EnsureChildBody("RightShoe");
        if (leftShoe.eyelet == null && leftShoeBody != null)
            leftShoe.eyelet = leftShoeBody.transform;
        if (rightShoe.eyelet == null && rightShoeBody != null)
            rightShoe.eyelet = rightShoeBody.transform;
    }

    Rigidbody EnsureChildBody(string name)
    {
        Transform t = transform.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        if (t == null)
            go.transform.SetParent(transform, false);
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.4f;
        if (go.GetComponent<Collider>() == null)
        {
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.08f;
        }
        return rb;
    }

    public RopeSystem EnsureLaceRope()
    {
        if (laceRope == null)
            laceRope = GetComponent<RopeSystem>();
        if (laceRope == null)
            laceRope = gameObject.AddComponent<RopeSystem>();
        laceRope.Config.mode = RopeMode.Grapple;
        laceRope.Config.totalLengthM = knotLengthM;
        Transform head = leftShoeBody != null ? leftShoeBody.transform : transform;
        Transform tail = rightShoeBody != null ? rightShoeBody.transform : transform;
        laceRope.BindAnchors(head, tail, knotLengthM);
        return laceRope;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.9f);
        DrawLaceGizmos(laceSplines);
        if (leftShoe?.laces != null) DrawLaceGizmos(leftShoe.laces);
        if (rightShoe?.laces != null) DrawLaceGizmos(rightShoe.laces);
    }

    static void DrawLaceGizmos(List<MonoBehaviour> splines)
    {
        if (splines == null) return;
        for (int i = 0; i < splines.Count; i++)
        {
            var s = splines[i];
            if (s == null) continue;
            var field = s.GetType().GetField("controlPoints");
            if (field == null) continue;
            var pts = field.GetValue(s) as System.Collections.IList;
            if (pts == null) continue;
            for (int c = 0; c < pts.Count; c++)
            {
                if (!(pts[c] is Vector3 local)) continue;
                Vector3 p = s.transform.TransformPoint(local);
                Gizmos.DrawSphere(p, 0.04f);
            }
        }
    }
}
