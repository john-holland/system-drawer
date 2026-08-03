using System.Collections.Generic;
using UnityEngine;

/// <summary>Phone/utility pole: wood cylinder, crossarms, transformer, guy, steps.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Utility Pole Assembly")]
public sealed class UtilityPoleAssembly : MonoBehaviour
{
    public float heightM = 9f;
    public float radiusM = 0.18f;
    public Color woodBase = new Color(0.45f, 0.28f, 0.12f);
    public Color woodGrain = new Color(0.25f, 0.14f, 0.06f);
    public Texture2D woodTexture;
    public bool useHairFluxWoodFallback = true;
    public Transform crossarm;
    public Transform transformer;
    public Transform insulatorRoot;
    public Transform fuse;
    public Transform groundWire;
    public Transform eyePlate;
    public Transform guyAnchor;
    public Transform guyMarker;
    public Transform secondaryRack;
    public Transform stepsRoot;
    public PowerLineTensionLemma tensionLemma;
    public List<Transform> climbStepAnchors = new List<Transform>();
    [Range(0f, 1f)] public float lean01;

    public void EnsureVisuals()
    {
        if (transform.childCount == 0)
            BuildDefaultHierarchy();
        ApplyWoodLook();
    }

    void BuildDefaultHierarchy()
    {
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "PoleShaft";
        pole.transform.SetParent(transform, false);
        pole.transform.localScale = new Vector3(radiusM * 2f, heightM * 0.5f, radiusM * 2f);
        pole.transform.localPosition = Vector3.up * (heightM * 0.5f);

        crossarm = new GameObject("Crossarm").transform;
        crossarm.SetParent(transform, false);
        crossarm.localPosition = Vector3.up * (heightM * 0.85f);

        insulatorRoot = new GameObject("Insulators").transform;
        insulatorRoot.SetParent(crossarm, false);

        transformer = new GameObject("Transformer").transform;
        transformer.SetParent(transform, false);
        transformer.localPosition = Vector3.up * (heightM * 0.7f) + Vector3.right * 0.35f;

        fuse = new GameObject("Fuse").transform;
        fuse.SetParent(crossarm, false);

        groundWire = new GameObject("GroundWireCopper").transform;
        groundWire.SetParent(transform, false);

        eyePlate = new GameObject("EyePlate").transform;
        eyePlate.SetParent(transform, false);
        eyePlate.localPosition = Vector3.up * (heightM * 0.65f);

        guyAnchor = new GameObject("GuyGroundAnchor").transform;
        guyAnchor.SetParent(transform, false);
        guyAnchor.localPosition = new Vector3(2.5f, 0f, 0f);

        guyMarker = new GameObject("GuyMarkerSheath").transform;
        guyMarker.SetParent(guyAnchor, false);

        secondaryRack = new GameObject("SecondaryRack").transform;
        secondaryRack.SetParent(transform, false);

        stepsRoot = new GameObject("PoleSteps").transform;
        stepsRoot.SetParent(transform, false);
        climbStepAnchors.Clear();
        for (int i = 0; i < 8; i++)
        {
            var step = new GameObject($"Step_{i}").transform;
            step.SetParent(stepsRoot, false);
            step.localPosition = Vector3.up * (1.2f + i * 0.85f);
            climbStepAnchors.Add(step);
            // Pathfinding / IK tag
            step.gameObject.tag = "Untagged";
            var col = step.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.12f;
        }
    }

    void ApplyWoodLook()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;
        var mat = rend.sharedMaterial != null ? new Material(rend.sharedMaterial) : new Material(Shader.Find("Standard"));
        mat.color = woodBase;
        if (woodTexture != null)
            mat.mainTexture = woodTexture;
        else if (useHairFluxWoodFallback)
        {
            // Solid fallback tint; HairRadialTextureCache can replace at bake time
            mat.SetColor("_Color", Color.Lerp(woodBase, woodGrain, 0.35f));
        }
        rend.sharedMaterial = mat;
    }

    public void ApplyTension(float tension01)
    {
        if (tensionLemma == null)
            tensionLemma = GetComponent<PowerLineTensionLemma>() ?? gameObject.AddComponent<PowerLineTensionLemma>();
        lean01 = tensionLemma.LeanAmount(tension01);
        transform.localRotation = Quaternion.Euler(0f, 0f, lean01 * 12f);
        if (tensionLemma.ShouldBreakPole(tension01))
            gameObject.SetActive(false);
    }

    public Vector3 TopAttachmentWorld => transform.position + Vector3.up * heightM * 0.92f;
}
