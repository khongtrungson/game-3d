Shader "NullProtocol/PostProcessing/MAT_Glitch_Post"
{
    Properties
    {
        [Header(Screen Glitch Controls)]
        _GlitchIntensity ("Glitch Intensity (0-1)", Range(0.0, 1.0)) = 0.0
        _RGBSplit ("RGB Split Offset", Range(0.0, 0.08)) = 0.025
        _HorizontalDisplacement ("Horizontal Displacement", Range(0.0, 0.1)) = 0.04
        _ScanlineFrequency ("Scanline Frequency", Range(10.0, 1000.0)) = 360.0
        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 1.0)) = 0.45
        _DeRefCollapse ("De-Reference Blackout (0-1)", Range(0.0, 1.0)) = 0.0
        _FlashIntensity ("Flash Intensity", Range(0.0, 5.0)) = 0.0

        [Header(Textures)]
        _MainTex ("Source / Fallback Screen", 2D) = "white" {}
        _NoiseTex ("Glitch Displacement Noise", 2D) = "gray" {}
        _TerminalOverlayTex ("Terminal Memory Dump Text", 2D) = "black" {}
        [HDR] _GlitchTint ("Glitch Tint Color", Color) = (0.0, 0.95, 1.0, 1.0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Overlay+100" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GlitchPostPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_TerminalOverlayTex);
            SAMPLER(sampler_TerminalOverlayTex);

            CBUFFER_START(UnityPerMaterial)
                float _GlitchIntensity;
                float _RGBSplit;
                float _HorizontalDisplacement;
                float _ScanlineFrequency;
                float _ScanlineIntensity;
                float _DeRefCollapse;
                float _FlashIntensity;
                float4 _GlitchTint;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. Sample horizontal displacement from noise map
                float2 noiseUV = float2(uv.y * 3.5 + _Time.y * 12.0, _Time.x * 18.0);
                float4 noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);
                float xOffset = (noiseSample.r - 0.5) * 2.0 * _HorizontalDisplacement * _GlitchIntensity;

                // Intermittent line jump
                float jump = step(0.92, sin(uv.y * 70.0 + _Time.y * 30.0)) * 0.05 * _GlitchIntensity;
                uv.x += xOffset + jump;

                // 2. Chromatic Aberration RGB split
                float split = _RGBSplit * _GlitchIntensity;
                float rCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(split, 0.0)).r;
                float gCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                float bCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(split, 0.0)).b;
                float alphaBase = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                float3 sceneCol = float3(rCol, gCol, bCol);

                // 3. Scanlines
                float scanline = sin(uv.y * _ScanlineFrequency + _Time.y * 80.0) * 0.5 + 0.5;
                sceneCol -= scanline * _ScanlineIntensity * _GlitchIntensity;

                // 4. Terminal Text Readout
                float4 termCol = SAMPLE_TEXTURE2D(_TerminalOverlayTex, sampler_TerminalOverlayTex, input.uv);
                sceneCol = lerp(sceneCol, termCol.rgb * _GlitchTint.rgb * 1.5, termCol.a * min(1.0, _GlitchIntensity * 1.6));

                // 5. Digital Flash & De-Reference Blackout
                sceneCol += _FlashIntensity * float3(0.1, 0.85, 1.0);
                sceneCol = lerp(sceneCol, float3(0.0, 0.0, 0.0), _DeRefCollapse);

                // Combined Alpha
                float finalAlpha = saturate(_GlitchIntensity + _DeRefCollapse);

                return float4(sceneCol, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
