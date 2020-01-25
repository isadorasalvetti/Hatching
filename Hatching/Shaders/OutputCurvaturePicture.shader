Shader "Unlit/OutputCurvaturePicture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _WhiteOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color =  UnityObjectToClipPos(v.color);
                o.normal = normalize(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
                //o.vertex = UnityObjectToClipPos(float4((v.uv-0.5f)*2, 1, 1));                

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //lighting                
                float ndotl = dot(i.normal, _WorldSpaceLightPos0);
                float ndotv = saturate(dot(i.normal, i.viewDir));     
                int samp = ((1-ndotl)*2 - _WhiteOffset);
                float light = 1 - samp/2.0;
                
                fixed2 curv_direction = i.color.xy/i.color.w;
                fixed4 col = fixed4(curv_direction*0.5 + 0.5, i.color.z, light);
                return col;
            }
            ENDCG
        }
    }
}
