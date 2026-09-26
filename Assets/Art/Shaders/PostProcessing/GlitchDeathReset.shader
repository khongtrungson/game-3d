Shader "Hidden/PostProcessing/GlitchDeathReset"
{
    Properties
    {
        [HideInInspector] _MainTex ("Source Texture", 2D) = "white" {}
        [Header(Death Loop Timeline)]
        _GlitchProgress ("Glitch Progress (0-1 over 2s)", Range(0.0, 1.0)) = 0.0
        _GlitchIntensity ("Overall Intensity", Range(0.0, 2.0)) = 1.0
        
        [Header(Horizontal Block Displacement)]
        _DisplacementStrength ("Max Displacement", Range(0.0, 0.15)) = 0.04
        _BlockJitterFrequency ("Jitter Frequency", Range(1.0, 100.0)) = 35.0
        _SliceResolution ("Scanline Slices", Range(10.0, 300.0)) = 80.0

        [Header(RGB Chromatic Aberration)]
        _ChromaticAberration ("RGB Split Spread", Range(0.0, 0.08)) = 0.02
        _RadialChromaticWeight ("Radial Distortion Weight", Range(0.0, 2.0)) = 1.0

        [Header(De-referencing Flash and Inversion)]
        _FlashColor ("De-referencing Tint", Color) = (0.05, 0.85, 1.0, 1.0)
        _FlashIntensity ("Flash Brightness", Range(0.0, 5.0)) = 1.5
        _NoiseGrain ("Digital Noise Grain", Range(0.0, 1.0)) = 0.25
        _ScanlineDensity ("Scanline Density", Range(50.0, 1000.0)) = 400.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        LOD 100
        ZTest Always 
        ZWrite Off 
        Cull Off

        Pass
        {
            Name "GlitchPostPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Include full-screen blit utilities for URP
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _GlitchProgress;
                float _GlitchIntensity;
                float _DisplacementStrength;
                float _BlockJitterFrequency;
                float _SliceResolution;
                float _ChromaticAberration;
                float _RadialChromaticWeight;
                float4 _FlashColor;
                float _FlashIntensity;
                float _NoiseGrain;
                float _ScanlineDensity;
            CBUFFER_END

            // Pseudo-random hashing helpers
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float time = _Time.y;

                // 2.0s death reset loop progression curve
                // Glitch starts subtle, escalates exponentially towards ~1.6s, climaxes in hard digital corruption, then resets
                float loopProgress = saturate(_GlitchProgress);
                float escalation = pow(loopProgress, 2.2);
                float effectiveIntensity = _GlitchIntensity * (escalation + 0.1 * step(0.01, loopProgress));

                // 1. Horizontal Glitch Displacement (Block/Stripe tearing)
                float blockTime = floor(time * _BlockJitterFrequency);
                float blockY = floor(uv.y * _SliceResolution) / _SliceResolution;
                float sliceNoise = Hash21(float2(blockY, blockTime));
                
                // Trigger glitch displacement bands conditionally based on noise threshold
                float glitchGate = step(0.65 - 0.4 * loopProgress, sliceNoise);
                float displacement = (Hash11(blockTime + blockY * 137.1) - 0.5) * 2.0;
                displacement *= _DisplacementStrength * effectiveIntensity * glitchGate;

                float2 displacedUV = uv;
                displacedUV.x += displacement;

                // 2. Chromatic Aberration (RGB Split)
                float2 screenCenter = float2(0.5, 0.5);
                float2 fromCenter = displacedUV - screenCenter;
                float radialFactor = length(fromCenter) * _RadialChromaticWeight;
                
                float chromaOffset = (_ChromaticAberration * effectiveIntensity) * (1.0 + radialFactor) + abs(displacement) * 0.75;
                float2 uvR = displacedUV + float2(chromaOffset, 0.0);
                float2 uvG = displacedUV;
                float2 uvB = displacedUV - float2(chromaOffset, 0.0);

                // Sample Blit input (or _MainTex)
                #if defined(USING_STEREO_MATRICES)
                    float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uvR).r;
                    float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uvG).g;
                    float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uvB).b;
                #else
                    float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvR).r;
                    float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvG).g;
                    float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvB).b;
                #endif

                float3 sceneCol = float3(r, g, b);

                // 3. Scanline rasterization
                float scanline = sin(displacedUV.y * _ScanlineDensity + time * 20.0) * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, 0.35 * effectiveIntensity);
                sceneCol *= scanline;

                // 4. Digital Noise / Static Grain
                float grain = Hash21(uv * 1000.0 + float2(time, -time));
                float grainFactor = (grain - 0.5) * _NoiseGrain * effectiveIntensity;
                sceneCol += grainFactor;

                // 5. De-referencing Flash / Screen Inversion Spike at peak of death loop (progress > 0.85)
                float climaxPhase = smoothstep(0.80, 0.96, loopProgress);
                float derefStrobe = step(0.4, Hash11(floor(time * 30.0)));
                
                // Color negation / digital solarization during de-reference
                float3 invertedCol = 1.0 - sceneCol;
                float3 corruptedTint = lerp(sceneCol, invertedCol, climaxPhase * derefStrobe * 0.85);
                corruptedTint += _FlashColor.rgb * (_FlashIntensity * climaxPhase * (0.6 + 0.4 * derefStrobe));

                // Fade out to terminal whiteout/blackout right at cycle completion (0.96 - 1.0)
                float postResetFade = smoothstep(0.96, 1.0, loopProgress);
                float3 finalCol = lerp(corruptedTint, _FlashColor.rgb * 2.0, postResetFade * 0.7);

                return float4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
