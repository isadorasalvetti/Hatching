Shader "Hidden/ImageSpaceOutline"
{
    Properties
    {
        _Scale ("Scale", range(1, 10)) = 1
        _DepthThreshold ("Depth Threshold", range(0, 2)) = 0.1
        _NormalThreshold ("Normal Threshold", range(0, 2)) = 0.1
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        //Cull Off ZWrite Off ZTest Always

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
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD2;
                float3 normal: TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                o.uv = v.uv;
                return o;
            }

            float _Scale;
            float _DepthThreshold;
            float _NormalThreshold;
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag (v2f i) : SV_Target
            {
                // Deptth
                float halfScaleFloor = floor(_Scale * 0.5);
                float halfScaleCeil = ceil(_Scale * 0.5); 
                
                float2 bottomLeftUV = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * halfScaleFloor;
                float2 topRightUV = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * halfScaleCeil;  
                float2 bottomRightUV = i.uv + float2(_MainTex_TexelSize.x * halfScaleCeil, -_MainTex_TexelSize.y * halfScaleFloor);
                float2 topLeftUV = i.uv + float2(-_MainTex_TexelSize.x * halfScaleFloor, _MainTex_TexelSize.y * halfScaleCeil);
               
                float depth0 = tex2D(_MainTex, bottomLeftUV).a;
                float depth1 = tex2D(_MainTex, topRightUV).a;
                float depth2 = tex2D(_MainTex, bottomRightUV).a;
                float depth3 = tex2D(_MainTex, topLeftUV).a;
               
                float depthFiniteDifference0 = depth1 - depth0;
                float depthFiniteDifference1 = depth3 - depth2;
                
                float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
                edgeDepth = edgeDepth > _DepthThreshold ? 1 : 0;
                
                // Normal
                float3 normal0 = tex2D(_MainTex, bottomLeftUV).rgb;
                float3 normal1 = tex2D(_MainTex, topRightUV).rgb;
                float3 normal2 = tex2D(_MainTex, bottomRightUV).rgb;
                float3 normal3 = tex2D(_MainTex, topLeftUV).rgb;
                
                float3 normalFiniteDifference0 = normal1 - normal0;
                float3 normalFiniteDifference1 = normal3 - normal2;
                
                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;
                
                float edge = 1 - max(edgeDepth, edgeNormal);
                
                float ndotv = saturate(dot(i.normal, i.viewDir)*4);
                ndotv = pow(ndotv, 16);     

                float4 col = float4(edge, edge, edge, 1);
                return col;
            }
            ENDCG
        }
    }
}