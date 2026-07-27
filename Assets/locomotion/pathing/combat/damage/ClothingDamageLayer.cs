using UnityEngine;

/// <summary>Permanent clothing tear/burn field companion to ClothUvStretchLayer.</summary>
[AddComponentMenu("Locomotion/Combat/Clothing Damage Layer")]
public sealed class ClothingDamageLayer : MonoBehaviour
{
    public Renderer targetRenderer;
    [Range(0f, 1f)] public float tearField01;
    [Range(0f, 1f)] public float burnField01;
    public string damageTextureProperty = "_DamageMap";

    public void ApplyTear(float amount01) =>
        tearField01 = Mathf.Clamp01(tearField01 + Mathf.Max(0f, amount01));

    public void ApplyBurn(float amount01) =>
        burnField01 = Mathf.Clamp01(burnField01 + Mathf.Max(0f, amount01));

    void LateUpdate()
    {
        if (targetRenderer == null) return;
        var block = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(block);
        block.SetFloat("_Tear01", tearField01);
        block.SetFloat("_Burn01", burnField01);
        targetRenderer.SetPropertyBlock(block);
    }
}
