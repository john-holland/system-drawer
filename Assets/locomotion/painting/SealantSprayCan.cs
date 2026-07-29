using UnityEngine;

/// <summary>
/// Spray can: conical sealant layer in front of the player / wrist aim.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Sealant Spray Can")]
public sealed class SealantSprayCan : MonoBehaviour
{
    public Transform nozzle;
    public PaintCanvas canvas;
    public PaintPileLiquidDriver piles;
    [Range(5f, 90f)] public float coneAngleDeg = 25f;
    [Min(0.05f)] public float range = 0.55f;
    [Min(0.001f)] public float sealCoatThickness = 0.01f;
    [Range(0f, 1f)] public float dryRateBoost = 0.4f;
    public LayerMask hitMask = ~0;

    void Update()
    {
        var proxy = GetComponentInParent<PaintInstrumentProxy>();
        float spray = proxy != null ? proxy.GetChannel(PaintInstrumentMap.SealantSpray) : 0f;
        if (Input.GetKey(KeyCode.F))
            spray = Mathf.Max(spray, 1f);
        if (spray > 0.1f)
            Spray(spray * Time.deltaTime);
    }

    public void Spray(float amount)
    {
        if (nozzle == null) nozzle = transform;
        Vector3 origin = nozzle.position;
        Vector3 dir = nozzle.forward;
        float half = coneAngleDeg * 0.5f * Mathf.Deg2Rad;

        // Raycast center + ring samples into canvas
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            Vector3 offset = (nozzle.right * Mathf.Cos(ang) + nozzle.up * Mathf.Sin(ang)) * Mathf.Tan(half);
            Vector3 sampleDir = (dir + offset * 0.35f).normalized;
            if (Physics.Raycast(origin, sampleDir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (canvas != null && canvas.WorldToCanvasUv(hit.point, out Vector2 uv))
                {
                    Color coat = new Color(0f, dryRateBoost * amount * 4f, 0f, sealCoatThickness);
                    canvas.Viscosity?.Stamp(uv, coat, sealCoatThickness * 2f);
                    var wet = canvas.layerStack != null ? canvas.layerStack.TopWetLayer() : null;
                    if (wet != null)
                        wet.dry01 = Mathf.Clamp01(wet.dry01 + dryRateBoost * amount);
                }
            }
        }
        canvas?.Viscosity?.Apply();
        canvas?.BindMaterials();
    }
}
