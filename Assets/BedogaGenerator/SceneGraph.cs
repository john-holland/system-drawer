using UnityEngine;

// Scene graph component for organizational purposes under the SpatialGenerator game object
// This is a marker/organizational component for the SceneGraph GameObject
public class SceneGraph : MonoBehaviour
{
    [Header("Scene Graph Organization")]
    public bool organizeOnStart = true;
    
    void Start()
    {
        if (organizeOnStart)
        {
            // Organize child objects if needed
            // This can be extended to provide organizational functionality
        }
    }
}
