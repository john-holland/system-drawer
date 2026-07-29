using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Painting IK training catalog entries (clip discovery still uses RagdollIKAnimationManager).
/// </summary>
[CreateAssetMenu(fileName = "PaintingIkTrainingCatalog", menuName = "Locomotion/Painting/IK Training Catalog")]
public sealed class PaintingIkTrainingCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string displayName;
        public PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.ToolUse;
        public string suggestedClipFolder;
        [Tooltip("Developer discretion — collision-enabled carry keep-out.")]
        public bool collisionEnabledCarryMode;
        public string notes;
    }

    public bool collisionEnabledCarryMode;
    public List<Entry> entries = new List<Entry>();

    public void EnsureDefaults()
    {
        if (entries != null && entries.Count > 0) return;
        entries = new List<Entry>
        {
            new Entry
            {
                id = "brush_stroke",
                displayName = "Brush stroke",
                category = PhysicsIKTrainingCategory.ToolUse,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/BrushStroke",
                notes = "Tip follows canvas stroke path"
            },
            new Entry
            {
                id = "palette_hold",
                displayName = "Palette hold",
                category = PhysicsIKTrainingCategory.Carry,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/PaletteHold",
                notes = "Non-dominant holds palette (two-hand limb list)"
            },
            new Entry
            {
                id = "tube_dispense",
                displayName = "Tube dispense",
                category = PhysicsIKTrainingCategory.ToolUse,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/TubeDispense",
                notes = "TwoHands: hold + finger squeeze"
            },
            new Entry
            {
                id = "spray_can",
                displayName = "Spray can",
                category = PhysicsIKTrainingCategory.ToolUse,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/SprayCan",
                notes = "Conical sealant aim"
            },
            new Entry
            {
                id = "lean_inspect",
                displayName = "Lean inspect",
                category = PhysicsIKTrainingCategory.Isometric,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/LeanInspect",
                notes = "Lean back, eye to canvas"
            },
            new Entry
            {
                id = "pick_up_painting",
                displayName = "Pick up painting",
                category = PhysicsIKTrainingCategory.Pick,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/PickUpPainting",
                notes = "Pick then Carry; tilt for specular"
            },
            new Entry
            {
                id = "collision_enabled_carry",
                displayName = "Collision-enabled carry",
                category = PhysicsIKTrainingCategory.Carry,
                suggestedClipFolder = "Assets/locomotion/painting/Animations/CollisionCarry",
                collisionEnabledCarryMode = true,
                notes = "Frame grips only; fail if wet paint would smudge. Developer opt-in."
            }
        };
    }
}
