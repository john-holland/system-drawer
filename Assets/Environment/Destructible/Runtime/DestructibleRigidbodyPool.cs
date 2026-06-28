using UnityEngine;

namespace DestructibleEnvironment
{
    public sealed class DestructiblePoolSlot
    {
        public GameObject Root;
        public Transform Transform;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public MeshCollider MeshCollider;
        public Rigidbody Rigidbody;
        public int PieceId = -1;
        public bool IsActive;
    }

    /// <summary>Fixed pool of piece rigidbodies for destructible activation.</summary>
    public class DestructibleRigidbodyPool
    {
        readonly Transform _root;
        readonly DestructiblePoolSlot[] _slots;

        public DestructibleRigidbodyPool(Transform root, int slotCount, PhysicsMaterial defaultMaterial)
        {
            _root = root;
            _slots = new DestructiblePoolSlot[Mathf.Max(1, slotCount)];
            for (int i = 0; i < _slots.Length; i++)
            {
                var go = new GameObject($"DestructiblePiece_slot{i}");
                go.transform.SetParent(_root, false);
                go.SetActive(false);

                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var mc = go.AddComponent<MeshCollider>();
                mc.convex = true;
                if (defaultMaterial != null)
                    mc.sharedMaterial = defaultMaterial;

                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                _slots[i] = new DestructiblePoolSlot
                {
                    Root = go,
                    Transform = go.transform,
                    MeshFilter = mf,
                    MeshRenderer = mr,
                    MeshCollider = mc,
                    Rigidbody = rb
                };
            }
        }

        public int SlotCount => _slots.Length;

        public DestructiblePoolSlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                return null;
            return _slots[index];
        }

        public bool AssignPiece(int slot, Mesh mesh, Material[] materials, Pose worldPose, int pieceId)
        {
            DestructiblePoolSlot s = GetSlot(slot);
            if (s == null || mesh == null)
                return false;

            s.PieceId = pieceId;
            s.MeshFilter.sharedMesh = mesh;
            s.MeshRenderer.sharedMaterials = materials != null && materials.Length > 0 ? materials : s.MeshRenderer.sharedMaterials;
            s.MeshCollider.sharedMesh = mesh;
            s.Transform.SetPositionAndRotation(worldPose.position, worldPose.rotation);
            s.Rigidbody.isKinematic = true;
            s.Rigidbody.useGravity = false;
            s.Rigidbody.linearVelocity = Vector3.zero;
            s.Rigidbody.angularVelocity = Vector3.zero;
            s.IsActive = true;
            s.Root.SetActive(true);
            return true;
        }

        public void SetKinematic(int slot, bool kinematic)
        {
            DestructiblePoolSlot s = GetSlot(slot);
            if (s?.Rigidbody == null)
                return;
            s.Rigidbody.isKinematic = kinematic;
        }

        public void HandoffToDynamic(int slot)
        {
            DestructiblePoolSlot s = GetSlot(slot);
            if (s?.Rigidbody == null)
                return;
            s.Rigidbody.isKinematic = false;
            s.Rigidbody.useGravity = true;
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].IsActive = false;
                _slots[i].PieceId = -1;
                _slots[i].Root.SetActive(false);
            }
        }
    }
}
