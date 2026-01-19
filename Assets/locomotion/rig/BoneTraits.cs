using System;
using UnityEngine;

namespace Locomotion.Rig
{
    /// <summary>
    /// A first-class bone trait: a semantic identity for a "part" that can exist on any rig.
    /// Humanoid bones are one implementation; cars/buildings can define their own traits too.
    /// </summary>
    public interface IBoneTrait
    {
        /// <summary>
        /// Stable ID used for dictionary keys / persistence (e.g. "Human:Head" or "Generic:WheelFL").
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable name (e.g. "Head", "WheelFL").
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Category label (e.g. "Human", "Generic", "Vehicle", "Building").
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Optional: if this trait corresponds to a Unity Humanoid bone.
        /// </summary>
        HumanBodyBones? HumanoidBone { get; }
    }

    [Serializable]
    public sealed class HumanBoneTrait : IBoneTrait, IEquatable<HumanBoneTrait>
    {
        [SerializeField] private HumanBodyBones bone;

        public HumanBoneTrait(HumanBodyBones bone)
        {
            this.bone = bone;
        }

        public string Id => $"Human:{bone}";
        public string DisplayName => bone.ToString();
        public string Category => "Human";
        public HumanBodyBones? HumanoidBone => bone;

        public bool Equals(HumanBoneTrait other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return bone == other.bone;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is HumanBoneTrait other && Equals(other);
        public override int GetHashCode() => (int)bone;
    }

    [Serializable]
    public sealed class GenericBoneTrait : IBoneTrait, IEquatable<GenericBoneTrait>
    {
        [SerializeField] private string category;
        [SerializeField] private string name;

        public GenericBoneTrait(string category, string name)
        {
            this.category = string.IsNullOrWhiteSpace(category) ? "Generic" : category.Trim();
            this.name = string.IsNullOrWhiteSpace(name) ? "Bone" : name.Trim();
        }

        public string Id => $"{category}:{name}";
        public string DisplayName => name;
        public string Category => category;
        public HumanBodyBones? HumanoidBone => null;

        public bool Equals(GenericBoneTrait other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(category, other.category, StringComparison.Ordinal) &&
                   string.Equals(name, other.name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is GenericBoneTrait other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(category, name);
    }
}

