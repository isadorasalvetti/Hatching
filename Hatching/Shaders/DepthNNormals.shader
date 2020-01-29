Shader "Unlit/DepthNNormals"
{

    Properties
        {
            _CameraFar("Camera Far", float) = 1000
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };
            
            float _CameraFar;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float depth = i.vertex.z/ _CameraFar;
                fixed4 col = fixed4(i.normal*0.5 + 0.5f, 1-depth);
                return col;
            }
            ENDCG
        }
    }
}
