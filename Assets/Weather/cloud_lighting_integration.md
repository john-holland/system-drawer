# Cloud Lighting Integration

## Current State

The `Cloud` component currently has no integration with Unity's Global Illumination (GI) system. Clouds are rendered using:
- **Particle System** (optional) - for particle-based cloud rendering
- **Material** (optional) - for shader-based cloud rendering
- **Visual Parameters** - altitude, pressure, coverage, density, type

Clouds receive lighting from:
- **Direct lights** (Directional Light, Point Lights, Spot Lights) - standard Unity lighting
- **Ambient lighting** - from Unity's ambient settings
- **No Light Probes** - clouds don't participate in light probe sampling
- **No Reflection Probes** - clouds don't receive environment reflections
- **No Light Probe Proxy Volumes (LPPV)** - no volumetric light probe support

## Lighting Integration Options

### 1. Realtime Global Illumination (Realtime GI)

**Unity's Realtime GI System**:
- Uses **Enlighten** (legacy) or **Progressive GPU** (newer) for realtime lightmap baking
- Updates lighting in realtime as lights change
- Requires **Lightmap UVs** on meshes
- Works with **Light Probes** for dynamic objects

**Cloud Integration Challenges**:
- Clouds are typically **volumetric** (not solid meshes with UVs)
- Clouds are **dynamic** (move, deform, change density)
- Clouds are **semi-transparent** (affects light transmission)
- Cloud rendering is often **shader-based** (volumetric raymarching, particle systems)

**Possible Approaches**:
1. **Light Probe Sampling**: Add `LightProbeGroup` components to cloud GameObjects
   - Probes sample ambient lighting at cloud positions
   - Works for particle-based clouds
   - Limited to point sampling (not volumetric)
   
2. **Light Probe Proxy Volumes (LPPV)**: For volumetric cloud rendering
   - Provides 3D light probe grid
   - Better for large cloud volumes
   - Requires shader modifications to sample LPPV data
   
3. **Reflection Probes**: For environment reflections on clouds
   - Can capture sky, sun, and environment
   - Useful for cloud-to-cloud reflections
   - Requires shader support for reflection sampling

### 2. Baked Global Illumination

**Baked GI**:
- Pre-computed lightmaps stored in textures
- Static lighting only (lights don't move)
- Fast runtime performance
- Requires **Lightmap UVs** and **Static** flags

**Cloud Integration Challenges**:
- Clouds are **dynamic** - can't be marked as Static
- Clouds **move and deform** - baked lighting would be incorrect
- Clouds are **procedural** - may not have consistent geometry

**Possible Approaches**:
1. **Hybrid Baking**: Bake static cloud "base" lighting, add realtime variations
   - Bake lighting for static cloud positions/densities
   - Apply realtime adjustments for movement/density changes
   - Requires careful shader design
   
2. **Cloud Shadow Baking**: Bake cloud shadows onto terrain/objects
   - Clouds cast shadows that can be baked
   - Realtime clouds add dynamic shadow variations
   - Useful for terrain lighting

3. **Ambient Occlusion Baking**: Bake AO for cloud volumes
   - Pre-compute ambient occlusion for cloud shapes
   - Apply as base lighting, add realtime GI on top
   - Requires cloud geometry to be known at bake time

### 3. Hybrid Approaches (Recommended)

**Best of Both Worlds**:
- Combine baked base lighting with realtime adjustments
- Use light probes for dynamic lighting
- Use reflection probes for environment
- Add custom shader integration for volumetric effects

## Implementation Recommendations

### Option A: Light Probe Integration (Realtime)

**For Particle-Based Clouds**:
```csharp
// Add to Cloud.cs
[Header("Lighting Integration")]
[Tooltip("Use light probes for ambient lighting")]
public bool useLightProbes = true;

[Tooltip("Light probe group for cloud lighting")]
public LightProbeGroup lightProbeGroup;

private Renderer cloudRenderer;

private void Awake()
{
    // ... existing code ...
    
    if (useLightProbes)
    {
        // Ensure renderer uses light probes
        cloudRenderer = GetComponent<Renderer>();
        if (cloudRenderer != null)
        {
            cloudRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            cloudRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        }
        
        // Create light probe group if missing
        if (lightProbeGroup == null)
        {
            lightProbeGroup = GetComponent<LightProbeGroup>();
            if (lightProbeGroup == null)
            {
                lightProbeGroup = gameObject.AddComponent<LightProbeGroup>();
            }
        }
    }
}
```

**For Shader-Based Volumetric Clouds**:
- Pass light probe data to shader via `Shader.SetGlobalVectorArray()` or material properties
- Sample light probes in shader using `SHADERGRAPH_LIGHT_PROBE` or custom sampling
- Update probe positions as clouds move (expensive, consider caching)

### Option B: Light Probe Proxy Volumes (LPPV) - Volumetric

**For Large Cloud Volumes**:
```csharp
[Header("Lighting Integration")]
[Tooltip("Use Light Probe Proxy Volume for volumetric lighting")]
public bool useLPPV = true;

[Tooltip("LPPV component for cloud volume")]
public LightProbeProxyVolume lppv;

private void Awake()
{
    if (useLPPV && lppv == null)
    {
        lppv = GetComponent<LightProbeProxyVolume>();
        if (lppv == null)
        {
            lppv = gameObject.AddComponent<LightProbeProxyVolume>();
            lppv.probeDensity = LightProbeProxyVolume.ProbeDensityMode.Custom;
            lppv.resolutionMode = LightProbeProxyVolume.ResolutionMode.Custom;
            // Set bounds to cover cloud volume
            lppv.boundsMode = LightProbeProxyVolume.BoundsMode.CustomBounds;
        }
    }
}

public void UpdateLPPVBounds()
{
    if (lppv != null)
    {
        // Update LPPV bounds to match cloud coverage area
        Bounds cloudBounds = new Bounds(
            transform.position + Vector3.up * (altitude.x + altitude.y) * 0.5f,
            new Vector3(coverageArea * 2f, altitude.y - altitude.x, coverageArea * 2f)
        );
        lppv.boundsCustom = cloudBounds;
    }
}
```

**Shader Integration**:
- Use `SHADERGRAPH_LIGHT_PROBE_PROXY_VOLUME` in Shader Graph
- Or sample LPPV data manually in custom shader
- Requires shader modifications to sample 3D probe grid

### Option C: Reflection Probe Integration

**For Environment Reflections**:
```csharp
[Header("Lighting Integration")]
[Tooltip("Use reflection probes for environment lighting")]
public bool useReflectionProbes = true;

[Tooltip("Reflection probe for cloud reflections")]
public ReflectionProbe reflectionProbe;

private void Awake()
{
    if (useReflectionProbes && reflectionProbe == null)
    {
        reflectionProbe = GetComponent<ReflectionProbe>();
        if (reflectionProbe == null)
        {
            reflectionProbe = gameObject.AddComponent<ReflectionProbe>();
            reflectionProbe.mode = ReflectionProbeMode.Realtime;
            reflectionProbe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            reflectionProbe.boxProjection = false; // Clouds are typically far from surfaces
        }
    }
}
```

### Option D: Custom Shader Integration (Most Flexible)

**Shader Properties for GI**:
```hlsl
// Cloud shader properties
Properties
{
    _CloudDensity ("Cloud Density", Range(0, 1)) = 0.5
    _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
    
    // GI Integration
    [Toggle] _UseLightProbes ("Use Light Probes", Float) = 1
    [Toggle] _UseReflectionProbes ("Use Reflection Probes", Float) = 1
    [Toggle] _UseLPPV ("Use LPPV", Float) = 0
    
    // Light probe data (passed from C#)
    _LightProbeData ("Light Probe Data", Vector) = (1, 1, 1, 1)
    
    // Reflection probe cubemap
    _ReflectionProbe ("Reflection Probe", Cube) = "" {}
}

// In shader code
#if _USELIGHTPROBES_ON
    // Sample light probes
    float3 lightProbeColor = SampleSH(lightProbeData);
    cloudColor.rgb *= lightProbeColor;
#endif

#if _USEREFLECTIONPROBES_ON
    // Sample reflection probe
    float3 reflectionColor = texCUBE(_ReflectionProbe, viewDir).rgb;
    cloudColor.rgb += reflectionColor * _ReflectionStrength;
#endif
```

**C# Integration**:
```csharp
private void UpdateLighting()
{
    if (cloudMaterial != null)
    {
        // Sample light probes at cloud position
        if (useLightProbes)
        {
            SphericalHarmonicsL2 sh;
            LightProbes.GetInterpolatedProbe(transform.position, null, out sh);
            Vector4[] coefficients = new Vector4[7];
            coefficients[0] = new Vector4(sh[0, 0], sh[0, 1], sh[0, 2], sh[0, 3]);
            coefficients[1] = new Vector4(sh[0, 4], sh[0, 5], sh[0, 6], sh[1, 0], sh[1, 1]);
            // ... (full SH encoding)
            cloudMaterial.SetVectorArray("_LightProbeData", coefficients);
        }
        
        // Update reflection probe
        if (useReflectionProbes && reflectionProbe != null)
        {
            reflectionProbe.RenderProbe();
            cloudMaterial.SetTexture("_ReflectionProbe", reflectionProbe.texture);
        }
    }
}
```

## Performance Considerations

### Light Probe Sampling
- **Cost**: Low to Medium (depends on probe count)
- **Update Frequency**: Per-frame for moving clouds (expensive), cached for static clouds
- **Best For**: Particle-based clouds, small cloud volumes

### Light Probe Proxy Volumes
- **Cost**: Medium to High (3D probe grid sampling)
- **Update Frequency**: When cloud bounds change significantly
- **Best For**: Large volumetric cloud volumes, shader-based rendering

### Reflection Probes
- **Cost**: High (cubemap rendering)
- **Update Frequency**: Realtime (every frame) or on-demand
- **Best For**: High-quality cloud reflections, environment integration

### Baked Lighting
- **Cost**: Runtime cost is very low (pre-computed)
- **Update Frequency**: Never (static)
- **Best For**: Static cloud bases, cloud shadows on terrain

## Recommended Implementation Strategy

### Phase 1: Light Probe Integration (Quick Win)
1. Add `LightProbeGroup` support to `Cloud.cs`
2. Enable light probe usage on cloud renderers
3. Test with existing light probe setup in scene
4. **Result**: Clouds receive ambient lighting from light probes

### Phase 2: Reflection Probe Integration
1. Add `ReflectionProbe` component support
2. Configure realtime or baked reflection probes
3. Pass reflection data to cloud shaders
4. **Result**: Clouds reflect environment (sky, sun, surroundings)

### Phase 3: LPPV for Volumetric Clouds (Advanced)
1. Add `LightProbeProxyVolume` support
2. Update LPPV bounds as clouds move/deform
3. Modify cloud shaders to sample LPPV data
4. **Result**: Volumetric lighting for large cloud volumes

### Phase 4: Hybrid Baked + Realtime (Optional)
1. Bake base lighting for static cloud positions
2. Add realtime GI adjustments for dynamic changes
3. Combine in shader
4. **Result**: Best performance with dynamic lighting

## Shader Graph Integration

If using **Shader Graph** for cloud rendering:

1. **Light Probe Node**: Use `Sample Light Probe` node
   - Automatically samples light probes at object position
   - Works with `LightProbeGroup` components

2. **Reflection Probe Node**: Use `Sample Reflection Probe` node
   - Samples reflection probe cubemap
   - Requires `ReflectionProbe` component

3. **LPPV Node**: Use `Sample Light Probe Proxy Volume` node
   - Samples 3D light probe grid
   - Requires `LightProbeProxyVolume` component

4. **Custom Sampling**: For advanced control
   - Use `Custom Function` nodes
   - Sample probes manually with custom logic

## Unity Lighting Settings

Ensure proper lighting setup:

1. **Lighting Window** → **Mixed Lighting**:
   - Choose **Baked Indirect** or **Subtractive** for baked GI
   - Choose **Realtime** for realtime GI
   - **Shadowmask** for hybrid approach

2. **Light Probe Groups**:
   - Place probes throughout scene
   - Higher density near clouds for better quality
   - Use **Light Probe Proxy Volume** for large volumes

3. **Reflection Probes**:
   - Place probes to capture environment
   - Use **Box Projection** for interior spaces (not needed for clouds)
   - Set **Refresh Mode** to **Realtime** for dynamic updates

4. **Lightmap Settings**:
   - Configure lightmap resolution
   - Set **Lightmap Encoding** (HDR for better quality)
   - **Note**: Clouds typically won't use lightmaps (dynamic)

## Testing Checklist

- [ ] Light probes sample correctly at cloud positions
- [ ] Cloud color responds to ambient lighting changes
- [ ] Reflection probes capture and reflect environment
- [ ] LPPV provides volumetric lighting for large clouds
- [ ] Performance is acceptable with realtime updates
- [ ] Clouds integrate with scene lighting (no harsh transitions)
- [ ] Dynamic cloud movement doesn't break lighting
- [ ] Shader correctly samples all GI data sources

## Future Considerations

1. **Ray-Traced Global Illumination** (Unity DXR/Hybrid Renderer):
   - Real-time ray-traced GI for clouds
   - High quality but requires RTX hardware
   - Consider for high-end platforms

2. **Screen-Space Global Illumination (SSGI)**:
   - Approximate GI from screen-space data
   - Lower cost than ray-tracing
   - Good for real-time cloud lighting

3. **Volumetric Lighting**:
   - God rays through clouds
   - Light scattering in cloud volumes
   - Requires custom shader implementation

4. **Cloud Shadows**:
   - Dynamic cloud shadow casting
   - Shadow maps or shadow volumes
   - Integration with terrain/object shadows

## Conclusion

**Recommended Approach**: Start with **Light Probe Integration** (Phase 1) for immediate realtime GI support. This provides ambient lighting without major shader changes. Then add **Reflection Probe Integration** (Phase 2) for environment reflections. Consider **LPPV** (Phase 3) only if you have large volumetric cloud volumes that need 3D lighting.

**Keep it Realtime**: All recommended approaches support realtime updates, allowing clouds to move and change while maintaining proper lighting integration.

**Baking Consideration**: While baked lighting is possible, it's not recommended for dynamic clouds. However, consider baking cloud shadows onto terrain for performance if cloud positions are relatively static.
