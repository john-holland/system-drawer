using UnityEngine;

// Component for objects that can stretch to fit bounds
// Used for objects like closet bars, super tall grandfather clocks, etc.
// Allows specifying which dimension stretches and texture repeat information
public class StretchObject : MonoBehaviour
{
    public enum StretchAxis
    {
        X,
        Y,
        Z,
        XY,
        XZ,
        YZ,
        XYZ
    }
    
    [Header("Stretch Configuration")]
    public StretchAxis stretchAxis = StretchAxis.Y;
    public bool maintainAspectRatio = false;
    
    [Header("Texture Repeat")]
    public Vector2 textureRepeat = Vector2.one;
    public bool tileTexture = true;
    
    [Header("Bounds")]
    public Vector3 targetSize = Vector3.one;
    
    private Vector3 originalScale;
    private Vector3 originalSize;
    
    void Start()
    {
        originalScale = transform.localScale;
        
        // Get original size from renderer or collider
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            originalSize = renderer.bounds.size;
        }
        else
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                originalSize = collider.bounds.size;
            }
            else
            {
                originalSize = transform.localScale;
            }
        }
    }
    
    public void StretchToSize(Vector3 size)
    {
        targetSize = size;
        ApplyStretch();
    }
    
    public void ApplyStretch()
    {
        Vector3 scale = originalScale;
        
        // Calculate scale factors for each axis
        Vector3 scaleFactor = new Vector3(
            targetSize.x / originalSize.x,
            targetSize.y / originalSize.y,
            targetSize.z / originalSize.z
        );
        
        // Apply stretching based on stretch axis
        switch (stretchAxis)
        {
            case StretchAxis.X:
                scale.x *= scaleFactor.x;
                break;
            case StretchAxis.Y:
                scale.y *= scaleFactor.y;
                break;
            case StretchAxis.Z:
                scale.z *= scaleFactor.z;
                break;
            case StretchAxis.XY:
                scale.x *= scaleFactor.x;
                scale.y *= scaleFactor.y;
                break;
            case StretchAxis.XZ:
                scale.x *= scaleFactor.x;
                scale.z *= scaleFactor.z;
                break;
            case StretchAxis.YZ:
                scale.y *= scaleFactor.y;
                scale.z *= scaleFactor.z;
                break;
            case StretchAxis.XYZ:
                scale.x *= scaleFactor.x;
                scale.y *= scaleFactor.y;
                scale.z *= scaleFactor.z;
                break;
        }
        
        transform.localScale = scale;
        
        // Apply texture repeat if needed
        ApplyTextureRepeat();
    }
    
    private void ApplyTextureRepeat()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Vector2 tiling = textureRepeat;
            if (tileTexture)
            {
                // Calculate tiling based on stretched size
                Vector3 stretchedSize = targetSize;
                tiling.x *= stretchedSize.x / originalSize.x;
                tiling.y *= stretchedSize.y / originalSize.y;
            }
            
            renderer.material.mainTextureScale = tiling;
        }
    }
}
