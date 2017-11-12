// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Tunnelling" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _AV ("Angular Velocity", Float) = 0
        _Feather ("Feather", Float) = 0.1
        _CageCubeMap ("Cage CubeMap", Cube) = "" {}
    }
    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AV;
            float _Feather;
            samplerCUBE _CageCubeMap;
            float4x4 _EyeProjection[2];
            float4x4 _EyeToWorld[2];

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
                fixed4 col = tex2D(_MainTex, uv);
                fixed4 fadeCol = fixed4(0,0,0,0);
                float2 coords = (i.uv - 0.5) * 2;
                float4 viewPos = mul(_EyeProjection[unity_StereoEyeIndex], float4(coords, 0, 1));
                viewPos.xyz /- viewPos.w;

                float radius = length(viewPos.xy / (_ScreenParams.xy / 2)) / 2;
                float avMin = (1 - _AV) - _Feather;
                float avMax = (1 - _AV) + _Feather;
                float t = saturate((radius - avMin) / (avMax - avMin));

                float rayDir = mul(_EyeToWorld[unity_StereoEyeIndex], viewPos).xyz;
                half4 cageData = texCUBE(_CageCubeMap, normalize(rayDir));
                return lerp(col, cageData * fadeCol, t);
            }
            ENDCG
        }
    }
}
