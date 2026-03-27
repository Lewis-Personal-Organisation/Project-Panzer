Shader "Custom/URP/TessellatedLit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color Tint", Color) = (1,1,1,1)

        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        _NormalMap ("Normal Map", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4 _BaseColor;
            float _Metallic;
            float _Smoothness;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS = TransformWorldToHClip(worldPos);

                OUT.positionWS = worldPos;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);

                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));

                float3 N = normalize(IN.normalWS);
                float3 T = normalize(IN.tangentWS.xyz);
                float3 B = cross(N, T) * IN.tangentWS.w;
                float3x3 TBN = float3x3(T, B, N);
                float3 normalWS = normalize(mul(normalTS, TBN));

                InputData lightingInput;
                lightingInput.positionWS = IN.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceViewDir(IN.positionWS);
                lightingInput.shadowCoord = float4(0,0,0,0);
                lightingInput.fogCoord = 0;
                lightingInput.vertexLighting = float3(0,0,0);
                lightingInput.bakedGI = float3(0,0,0);

                SurfaceData surface;
                surface.albedo = albedo.rgb;
                surface.alpha = albedo.a;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = normalTS;
                surface.emission = 0;
                surface.occlusion = 1;
                surface.specular = float3(0,0,0);
                surface.clearCoatMask = 0;
                surface.clearCoatSmoothness = 0;

                return UniversalFragmentPBR(lightingInput, surface);
            }

            ENDHLSL
        }
    }
}