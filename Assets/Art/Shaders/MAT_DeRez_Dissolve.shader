Shader "NullProtocol/VFX/MAT_DeRez_Dissolve"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (0.10, 0.12, 0.15, 1.0)
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.5
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.3

        [Header(De-Rez Voxel Dissolve)]
        _DissolveAmount ("Dissolve Amount (0-1)", Range(0.0, 1.0)) = 0.45
        _VoxelScale ("3D Voxel Scale", Range(0.5, 25.0)) = 3.5
        _Cutoff ("Alpha Cutoff Threshold", Range(0.0, 1.0)) = 0.5

        [Header(Glowing Wireframe Border)]
        [HDR] _GlowColor ("Cyan Glow Color", Color) = (0.0, 0.898, 1.0, 1.0)
        _GlowStrength ("Border Glow Strength", Range(1.0, 20.0)) = 6.0
        _EdgeWidth ("Border Edge Width", Range(0.01, 0.25)) = 0.08
        _WireframeDensity ("Wireframe Grid Density", Range(1.0, 50.0)) = 15.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "Queue" = "AlphaTest" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Cull Back
        Blend One Zero
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 positionOS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float2 uv           : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _Metallic;
                float _Smoothness;
                float _DissolveAmount;
                float _VoxelScale;
                float _Cutoff;
                float _GlowStrength;
                float _EdgeWidth;
                float _WireframeDensity;
            CBUFFER_END

            // Fast 3D Chebyshev Voronoi Cell Distance
            float3 Hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float Voronoi3D_Chebyshev(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float minDist = 1.0;

                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 pointOffset = Hash33(i + neighbor);
                            float3 diff = neighbor + pointOffset - f;
                            // Chebyshev metric: max(|dx|, |dy|, |dz|) yields box/voxel shapes
                            float d = max(max(abs(diff.x), abs(diff.y)), abs(diff.z));
                            minDist = min(minDist, d);
                        }
                    }
                }
                return saturate(minDist);
            }

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_Target
            {
                // 1. Calculate 3D Voronoi Noise in Object Space (prevents UV seam shearing)
                float3 voxelCoords = input.positionOS * _VoxelScale;
                float noise = Voronoi3D_Chebyshev(voxelCoords);

                // 2. Alpha Cutoff Clipping
                // Cutoff threshold compares noise against dissolve progression
                float dissolveCutoff = _DissolveAmount;
                clip(noise - dissolveCutoff);

                // 3. Glowing Cyan Wireframe Boundary Border
                // Border band between dissolve threshold and (dissolve + edgeWidth)
                float outerBand = step(dissolveCutoff, noise);
                float innerBand = step(dissolveCutoff + _EdgeWidth, noise);
                float borderMask = outerBand - innerBand;

                // Procedural high-frequency voxel wireframe grid along dissolving boundary
                float3 grid = abs(frac(voxelCoords * _WireframeDensity) - 0.5);
                float wireframe = step(min(min(grid.x, grid.y), grid.z), 0.08) * borderMask;

                // Combine boundary band and wireframe
                float finalBorderGlow = saturate(borderMask + wireframe);

                // 4. Lighting Calculation (URP Simple PBR)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = _BaseColor.rgb * (mainLight.color * (NdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation) + 0.15);

                // 5. Final Composite with High-Intensity Cyan Emission (strength 6.0)
                float3 emission = _GlowColor.rgb * (_GlowStrength * finalBorderGlow);
                float3 finalColor = diffuse + emission;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionOS   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _Metallic;
                float _Smoothness;
                float _DissolveAmount;
                float _VoxelScale;
                float _Cutoff;
                float _GlowStrength;
                float _EdgeWidth;
                float _WireframeDensity;
            CBUFFER_END

            float3 Hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float Voronoi3D_Chebyshev(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float minDist = 1.0;
                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 pointOffset = Hash33(i + neighbor);
                            float3 diff = neighbor + pointOffset - f;
                            float d = max(max(abs(diff.x), abs(diff.y)), abs(diff.z));
                            minDist = min(minDist, d);
                        }
                    }
                }
                return saturate(minDist);
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = GetShadowPositionHClip(posInputs);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_Target
            {
                float noise = Voronoi3D_Chebyshev(input.positionOS * _VoxelScale);
                clip(noise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionOS   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _Metallic;
                float _Smoothness;
                float _DissolveAmount;
                float _VoxelScale;
                float _Cutoff;
                float _GlowStrength;
                float _EdgeWidth;
                float _WireframeDensity;
            CBUFFER_END

            float3 Hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float Voronoi3D_Chebyshev(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float minDist = 1.0;
                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 pointOffset = Hash33(i + neighbor);
                            float3 diff = neighbor + pointOffset - f;
                            float d = max(max(abs(diff.x), abs(diff.y)), abs(diff.z));
                            minDist = min(minDist, d);
                        }
                    }
                }
                return saturate(minDist);
            }

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            float4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                float noise = Voronoi3D_Chebyshev(input.positionOS * _VoxelScale);
                clip(noise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
