using UnityEngine;

namespace Planetary.Rendering
{
    public sealed class LiquidShearBakeTexture3D : ScriptableObject
    {
        public Texture3D shearField;
        public Vector3Int resolution = new Vector3Int(32, 32, 32);
        public Bounds worldBounds;
    }
}
