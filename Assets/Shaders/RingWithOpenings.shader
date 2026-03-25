Shader "2D/Ring With Openings"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _OpeningCount ("Opening Count", Float) = 0
        [HideInInspector] _Opening0 ("Opening 0 (start,end)", Vector) = (0,0,0,0)
        [HideInInspector] _Opening1 ("Opening 1 (start,end)", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _OpeningCount;
                float4 _Opening0;
                float4 _Opening1;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.positionOS = v.positionOS.xyz;
                o.color = v.color;
                return o;
            }

            // Returns true if angle (0..360) lies inside the opening [start, end] (degrees).
            bool IsInOpening(float angle, float start, float end)
            {
                if (start <= end)
                    return angle >= start && angle <= end;
                return angle >= start || angle <= end;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color * _Color;
                if (tex.a < 0.01) return half4(0,0,0,0);

                // Angle from center in degrees [0, 360); Unity: right=0, counter-clockwise positive
                float angle = atan2(i.positionOS.y, i.positionOS.x) * 57.29577951;
                if (angle < 0) angle += 360;

                int count = (int)_OpeningCount;
                if (count > 0)
                {
                    if (IsInOpening(angle, _Opening0.x, _Opening0.y)) discard;
                    if (count > 1 && IsInOpening(angle, _Opening0.z, _Opening0.w)) discard;
                    if (count > 2 && IsInOpening(angle, _Opening1.x, _Opening1.y)) discard;
                    if (count > 3 && IsInOpening(angle, _Opening1.z, _Opening1.w)) discard;
                }
                return tex;
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
