Shader "Unlit/OutputCurvaturePicture"
{
    Properties
    {
        _WhiteOffset("White offset", range(0, 10)) = 1
    }
    SubShader
    {
        Tags { 
        "RenderType"="Opaque"
        "LightMode" = "ForwardBase"
        }
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
                float4 vertex : SV_POSITION;
                float3 normal: NORMAL;
                float3 viewDir : TEXCOORD2;
                float4 pdReferencePoint : TEXCOORD3;          
            };

            float _WhiteOffset;
            
            v2f vert (appdata v)
            {
                v2f o;
                fixed4 vertexPosition = UnityObjectToClipPos(v.vertex);
                fixed4 PDirection = fixed4(v.color.xyz, 0);
                o.vertex = vertexPosition;
                o.pdReferencePoint = UnityObjectToClipPos(vertexPosition + PDirection);
                o.normal = normalize(UnityObjectToWorldNormal(normalize(v.normal)));
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));            
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //lighting                
                float ndotl = dot(i.normal, _WorldSpaceLightPos0);
                float ndotv = saturate(dot(i.normal, i.viewDir));     
                int samp = ((1-ndotl)*2 - _WhiteOffset);
                float light = 1 - samp/2.0;
                fixed4 curvDirection4 = i.pdReferencePoint - i.vertex;
                fixed2 curvDirection2 = normalize(fixed4(curvDirection4.xyz*0.5 + 0.5, 0));
                fixed4 col = fixed4(curvDirection2, 0, 1);
                return col;
            }
            ENDCG
        }
    }
}
