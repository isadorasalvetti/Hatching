// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/Hatching"
{
    Properties
    {
        _Hatching ("Texture", 2DArray) = "" {}
        _WhiteOffset("White offset", range(0, 10)) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityShaderVariables.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            UNITY_DECLARE_TEX2DARRAY(_Hatching);
            float4 _Hatching_ST;
            float _WhiteOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Hatching);
                o.normal = normalize(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                //lighting                
                float ndotl = dot(i.normal, _WorldSpaceLightPos0);
                float ndotv = saturate(dot(i.normal, i.viewDir));     
                float samp = (1-ndotl)*6 - _WhiteOffset;
                
                float col1 = 1;
                float col2 = 1;
                if (samp+1 > 0) {
                    col2 = UNITY_SAMPLE_TEX2DARRAY(_Hatching, fixed3(i.uv, int(samp+1))).x;
                    if (samp > 0) col1 = UNITY_SAMPLE_TEX2DARRAY(_Hatching, fixed3(i.uv, int(samp))).x;
                }

                fixed3 col = lerp(col1, col2, frac(samp));
            
                return fixed4(col * 1-pow(1-ndotv, 4), 1);
            }
            ENDCG
        }
    }
}
