Shader "NullProtocol/FlatWireframeEdge"
{
    Properties
    {
        [Header(Flat Shading)]
        _BaseColor ("Base Color", Color) = (0.15, 0.17, 0.2, 1.0)
        _FlatLightIntensity ("Flat Light Intensity", Range(0.0, 1.0)) = 0.85

        [Header(Emissive Wireframe Edges)]
        [HDR] _WireframeColor ("Wireframe Edge Color", Color) = (0.0, 0.9, 1.0, 1.0)
        _WireframeThickness ("Wireframe Thickness", Range(0.001, 0.1)) = 0.02
        _WireframeGloss ("Wireframe Intensity", Range(1.0, 10.0)) = 3.0

        [Header(Localized Chromatic Glitch)]
        _GlitchIntensity ("Glitch Intensity", Range(0.0, 1.0)) = 0.0
        _GlitchBlockScale ("Glitch Block Scale", Range(1.0, 100.0)) = 25.0
        _GlitchColorShift ("Glitch Color Shift", Range(0.0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 uv : TEXCOORD0;
                float4 barycentric : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _WireframeColor;
                float _FlatLightIntensity;
                float _WireframeThickness;
                float _WireframeGloss;
                float _GlitchIntensity;
                float _GlitchBlockScale;
                float _GlitchColorShift;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Localized chromatic glitch displacement
                float3 posOS = input.positionOS.xyz;
                if (_GlitchIntensity > 0.001)
                {
                    float blockNoise = sin(floor(posOS.y * _GlitchBlockScale) + _Time.y * 30.0);
                    if (frac(blockNoise * 43758.5453) > 0.75)
                    {
                        posOS.x += (frac(blockNoise * 12.9898) - 0.5) * _GlitchColorShift * _GlitchIntensity;
                    }
                }

                output.positionCS = TransformObjectToHClip(posOS);
                output.worldPos = TransformObjectToWorld(posOS);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv.xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Flat-shaded geometric normal calculation via screen derivatives
                float3 dX = ddx(input.worldPos);
                float3 dY = ddy(input.worldPos);
                float3 flatNormal = normalize(cross(dY, dX));

                // Directional pseudo-lighting for flat geometric shading
                float3 lightDir = normalize(float3(0.5, 0.8, 0.3));
                float NdotL = max(0.2, dot(flatNormal, lightDir) * _FlatLightIntensity);
                half3 flatColor = _BaseColor.rgb * NdotL;

                // High-contrast emissive wireframe edge detection using screen-space UV derivatives
                float2 uvDeriv = fwidth(input.uv);
                float2 edgeDist = min(input.uv, 1.0 - input.uv);
                float2 edgeFactor = smoothstep(uvDeriv * 0.5, uvDeriv * 1.5, edgeDist - _WireframeThickness);
                float isEdge = 1.0 - min(edgeFactor.x, edgeFactor.y);

                half3 wireColor = _WireframeColor.rgb * _WireframeGloss;

                // Localized chromatic glitch color split
                if (_GlitchIntensity > 0.001)
                {
                    float glitchNoise = sin(floor(input.uv.y * _GlitchBlockScale) + _Time.y * 40.0);
                    if (frac(glitchNoise * 78.233) > 0.65)
                    {
                        flatColor.r += _GlitchColorShift * _GlitchIntensity;
                        flatColor.b -= _GlitchColorShift * _GlitchIntensity;
                        isEdge = max(isEdge, _GlitchIntensity * 0.8);
                    }
                }

                half3 finalRGB = lerp(flatColor, wireColor, isEdge);
                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}
