using UnityEngine;

/// <summary>Tongue mesh or SDF-max proxy with capsule food-pocket position.</summary>
[AddComponentMenu("Locomotion/Body Interior/Tongue Runtime")]
public sealed class TongueRuntime : MonoBehaviour
{
    public enum TongueRepresentation
    {
        MeshRenderer,
        SdfMax
    }

    public TongueRepresentation representation = TongueRepresentation.MeshRenderer;
    public Renderer meshRenderer;
    public UnityEngine.Object sdfComposition;
    public Transform capsuleAnchor;
    public Vector3 capsuleCenterLocal;
    public float capsuleRadius = 0.02f;
    public float capsuleHeight = 0.06f;

    [Header("Animation")]
    [Range(0f, 1f)] public float curl01;
    [Range(0f, 1f)] public float lift01;
    public Vector3 foodPocketLocal = new Vector3(0f, 0f, 0.02f);

    public Vector3 FoodPocketWorld =>
        capsuleAnchor != null
            ? capsuleAnchor.TransformPoint(foodPocketLocal)
            : transform.TransformPoint(foodPocketLocal);

    public void SetFoodPocketLocal(Vector3 local)
    {
        foodPocketLocal = local;
    }

    /// <summary>Parabola pocket path for cheese: t in 0..1 across mouth width/depth.</summary>
    public void SetPocketParabola(float t, float width = 0.04f, float depth = 0.05f, float height = 0.015f)
    {
        t = Mathf.Clamp01(t);
        float x = Mathf.Lerp(-width, width, t);
        float z = depth * 4f * t * (1f - t);
        float y = height * Mathf.Sin(t * Mathf.PI);
        foodPocketLocal = new Vector3(x, y, z);
    }
}
