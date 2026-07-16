# Font Family → SDF Max

Pipeline for chiclet legend cavities:

1. **`FontFamilyGlyphMesher`** — extrude character stamp meshes (procedural silhouette; TMP/FontEngine outlines can replace later).
2. **`GlyphConvexTreeBaker`** — convex `MeshCollider` + `ConvexTreeMeshColliderService` cache.
3. **`GlyphSdfMaxComposer`** — chiclet `Box` **Subtract** glyph `MeshBounds` → `SdfMaxCompositionAsset`.

Used by `ComputerKeyboardBuilder` when a key has a unicode legend.
