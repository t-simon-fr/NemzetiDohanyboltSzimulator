Shader "Shop/PET"
{
    Properties { [MainColor] _BaseColor ("PET tint", Color) = (.68,.85,.96,.18) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1-saturate(dot(normal,view)),2);
                half light = .70h+.30h*saturate(dot(normal,normalize(half3(-.4h,.8h,-.6h))));
                return half4(_BaseColor.rgb*light+rim*.2h,saturate(_BaseColor.a+rim*.65h));
            }
            ENDHLSL
        }
    }
}
