Shader "Unlit/OutputCurvaturePicture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            };

            struct v2f
            {
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                if (v.color.x < 0) v.color.xyz *= -1;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = UnityObjectToClipPos(v.color);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 curv_direction = normalize(i.color.xyz/i.color.w);
                fixed4 col = fixed4((normalize(curv_direction) + 1)/2, 1);
                return col;
            }
            ENDCG
        }
    }
}
