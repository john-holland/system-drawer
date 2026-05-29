using UnityEngine;

namespace SdfMax
{
    [CreateAssetMenu(fileName = "SdfMaxSkinBinding", menuName = "SDF Max/Skin Binding")]
    public sealed class SdfMaxSkinBindingAsset : ScriptableObject
    {
        public Transform rootBone;
        public Transform[] bones = System.Array.Empty<Transform>();
        public Matrix4x4[] bindposes = System.Array.Empty<Matrix4x4>();
        public BoneWeight[] boneWeights = System.Array.Empty<BoneWeight>();
    }
}
