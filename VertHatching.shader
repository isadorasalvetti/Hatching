Shader "Unlit/Hatching_vert"
{
    Properties
    {
        _Hatching ("Texture", 2DArray) = "" {}
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD1;
                fixed3 hatchWeights0 : TEXCOORD2;
				fixed3 hatchWeights1 : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
            };

            UNITY_DECLARE_TEX2DARRAY(_Hatching);
            float4 _Hatching_ST;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
                UNITY_TRANSFER_FOG(o,o.vertex);

                //lighting - chose tone per vertex
                float3 normal = normalize(v.normal);
                float hatchFactor = ((dot(normal, _WorldSpaceLightPos0) + 1)/ 2) * 7;

                o.hatchWeights0 = fixed3(0, 0, 0);
				o.hatchWeights1 = fixed3(0, 0, 0);

                if (hatchFactor > 6.0) {
					// Pure white, do nothing
				} else if (hatchFactor > 5.0) {
					o.hatchWeights0.x = hatchFactor - 5.0;
				} else if (hatchFactor > 4.0) {
					o.hatchWeights0.x = hatchFactor - 4.0;
					o.hatchWeights0.y = 1.0 - o.hatchWeights0.x;
				} else if (hatchFactor > 3.0) {
					o.hatchWeights0.y = hatchFactor - 3.0;
					o.hatchWeights0.z = 1.0 - o.hatchWeights0.y;
				} else if (hatchFactor > 2.0) {
					o.hatchWeights0.z = hatchFactor - 2.0;
					o.hatchWeights1.x = 1.0 - o.hatchWeights0.z;
				} else if (hatchFactor > 1.0) {
					o.hatchWeights1.x = hatchFactor - 1.0;
					o.hatchWeights1.y = 1.0 - o.hatchWeights1.x;
				} else {
					o.hatchWeights1.y = hatchFactor;
					o.hatchWeights1.z = 1.0 - o.hatchWeights1.y;
				}

                o.uv = TRANSFORM_TEX(v.uv, _Hatching);               
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {   
                float hatchTex[6];
                float ndotv = saturate(dot(i.normal, i.viewDir));
                for (int a = 0; a < 3; a++) {
                    hatchTex[a] = UNITY_SAMPLE_TEX2DARRAY(_Hatching, fixed3(i.uv, a)).x * i.hatchWeights0[a];
                }
                for (a = 3; a < 6; a++) {
                    hatchTex[a] = UNITY_SAMPLE_TEX2DARRAY(_Hatching, fixed3(i.uv, a)).x * i.hatchWeights1[a-3];
                }
				float whiteColor = (1 - i.hatchWeights0.x - i.hatchWeights0.y - i.hatchWeights0.z - i.hatchWeights1.x - i.hatchWeights1.y - i.hatchWeights1.z);                    
                float hatchColor = hatchTex[0] + hatchTex[1] + hatchTex[2] + hatchTex[3] + hatchTex[4] + hatchTex[5] + whiteColor;
                hatchColor = hatchColor * 1-pow(1-ndotv, 4);
                return fixed4(hatchColor, hatchColor, hatchColor, 1);
            }
            ENDCG
        }
    }
}
