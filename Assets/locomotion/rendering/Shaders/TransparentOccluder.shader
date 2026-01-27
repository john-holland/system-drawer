Shader "Locomotion/TransparentOccluder"
{
    Properties
    {
        _DitherIntensity ("Dither Intensity", Range(0, 1)) = 0.5
        _GradientStrength ("Gradient Strength", Range(0, 1)) = 0.7
        _RandomSampleCount ("Random Sample Count", Int) = 4
        _BackfaceDarkness ("Backface Darkness", Range(0, 1)) = 0.8
        _FadeZoneSize ("Fade Zone Size", Float) = 0.1
        _LeadFaceFadeAngle ("Lead Face Fade Angle", Range(0, 90)) = 45
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        LOD 100
        
        // Stencil setup for occlusion marking
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
            ZFail Keep
        }
        
        // Render both front and back faces
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float2 uv : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };
            
            float4 _BoundsCenter;
            float4 _BoundsSize;
            float _DitherIntensity;
            float _GradientStrength;
            int _RandomSampleCount;
            float _BackfaceDarkness;
            float _FadeZoneSize;
            float _LeadFaceFadeAngle;
            float4x4 _ObjectToWorld;
            float4x4 _WorldToObject;
            
            // Simple noise function for dithering
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            // Gradient noise
            float gradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Multi-sample random dithering
            float randomDither(float3 worldPos, float3 localPos)
            {
                float dither = 0.0;
                float scale = 1.0;
                
                for (int i = 0; i < _RandomSampleCount && i < 16; i++)
                {
                    float2 samplePos = (localPos.xy + localPos.z * 0.1) * scale;
                    dither += gradientNoise(samplePos + float2(i * 0.1, i * 0.2));
                    scale *= 2.0;
                }
                
                return dither / float(_RandomSampleCount);
            }
            
            // Gradient-based dithering based on distance from bounds edges
            float gradientDither(float3 localPos)
            {
                float3 center = _BoundsCenter.xyz;
                float3 size = _BoundsSize.xyz;
                float3 halfSize = size * 0.5;
                
                // Calculate distance from each face
                float3 distFromCenter = abs(localPos - center);
                float3 distFromEdge = halfSize - distFromCenter;
                
                // Find minimum distance to any edge
                float minDist = min(min(distFromEdge.x, distFromEdge.y), distFromEdge.z);
                
                // Create gradient fade
                float fade = smoothstep(0.0, _FadeZoneSize, minDist);
                
                // Add some variation based on position
                float variation = gradientNoise(localPos.xy * 10.0 + localPos.z * 5.0);
                
                return lerp(fade, variation, 0.3);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i, bool facing : SV_IsFrontFace) : SV_Target
            {
                // Calculate dithering
                float gradDither = gradientDither(i.localPos);
                float randDither = randomDither(i.worldPos, i.localPos);
                
                // Combine dithering methods
                float combinedDither = lerp(randDither, gradDither, _GradientStrength);
                
                // Apply dither intensity
                float alpha = combinedDither * _DitherIntensity;
                
                // Handle backfaces
                if (!facing)
                {
                    // Darken backfaces
                    alpha = lerp(alpha, 1.0 - _BackfaceDarkness, _BackfaceDarkness);
                }
                
                // Lead face handling - faces that would be culled but are visible
                float3 viewDir = normalize(i.viewDir);
                float3 normal = normalize(i.normal);
                float viewAngle = abs(dot(viewDir, normal));
                float angleThreshold = cos(radians(_LeadFaceFadeAngle));
                
                if (viewAngle < angleThreshold)
                {
                    // This is a "lead face" - apply additional dithering
                    float leadFaceFactor = smoothstep(angleThreshold, 0.0, viewAngle);
                    alpha = lerp(alpha, 0.0, leadFaceFactor * 0.5);
                }
                
                // Calculate color - darken based on backface and dithering
                fixed3 color = fixed3(0, 0, 0);
                if (!facing)
                {
                    color = lerp(color, fixed3(0.1, 0.1, 0.1), _BackfaceDarkness);
                }
                
                // Final alpha with smooth fade
                alpha = smoothstep(0.0, 1.0, alpha);
                
                fixed4 col = fixed4(color, alpha);
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}
