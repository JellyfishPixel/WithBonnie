Shader "WithBonnie/URP Base Water"
{
    Properties
    {
        _SurfaceColor ("SurfaceColor", Color) = (0.28, 0.78, 0.68, 0.45)
        _DeepColor ("DeepColor", Color) = (0.0, 0.36, 1.0, 0.65)
        _FoamColor ("FoamColor", Color) = (1, 1, 1, 1)
        _RefractionNormal ("RefractionNormal", 2D) = "bump" {}
        _Distance ("Distance", Float) = 1.4
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85
        _NormalStrength ("NormalStrength", Range(0, 2)) = 0.25
        _RefractionSpeed ("RefractionSpeed", Float) = 0.1
        _RefractionStrength ("RefractionStrength", Range(0, 0.25)) = 0.05
        _FoamAmount ("FoamAmount", Range(0, 2)) = 1
        _FoamCuttoff ("FoamCutoff", Float) = 2
        _FoamSpeed ("FoamSpeed", Float) = 1
        _FoamScale ("FoamScale", Float) = 30
        _HightFrequency ("HighFrequency", Float) = 12.8
        _WaveSpeed ("WaveSpeed", Float) = 0.87
        _WaveAmplitude ("WaveAmplitude", Float) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SurfaceColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float4 _RefractionNormal_ST;
                half _Distance;
                half _Smoothness;
                half _NormalStrength;
                half _RefractionSpeed;
                half _RefractionStrength;
                half _FoamAmount;
                half _FoamCuttoff;
                half _FoamSpeed;
                half _FoamScale;
                half _HightFrequency;
                half _WaveSpeed;
                half _WaveAmplitude;
            CBUFFER_END

            TEXTURE2D(_RefractionNormal);
            SAMPLER(sampler_RefractionNormal);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float wave = sin((positionOS.x + positionOS.z) * _HightFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                positionOS.y += wave;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _RefractionNormal);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 movingUV = input.uv + _Time.y * _RefractionSpeed * float2(0.08, 0.05);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_RefractionNormal, sampler_RefractionNormal, movingUV), _NormalStrength);

                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half fresnel = pow(1.0 - saturate(dot(normalWS, normalize(GetWorldSpaceViewDir(input.positionWS)))), 3.0);

                half depthBlend = saturate(input.uv.y * max(_Distance, 0.001h));
                half3 waterColor = lerp(_SurfaceColor.rgb, _DeepColor.rgb, depthBlend);

                half foamWave = sin((input.uv.x + input.uv.y) * _FoamScale + _Time.y * _FoamSpeed);
                half foam = smoothstep(1.0h - saturate(_FoamAmount) * 0.5h, 1.0h, foamWave);
                foam *= saturate(_FoamCuttoff * 0.25h);

                half3 litColor = waterColor * (0.45 + ndotl * 0.55) * mainLight.color;
                litColor += fresnel * saturate(_Smoothness) * 0.35;
                litColor = lerp(litColor, _FoamColor.rgb, foam);

                half alpha = saturate(lerp(_SurfaceColor.a, _DeepColor.a, depthBlend) + fresnel * 0.12h);
                alpha = lerp(alpha, _FoamColor.a, foam * 0.65);
                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }
}
