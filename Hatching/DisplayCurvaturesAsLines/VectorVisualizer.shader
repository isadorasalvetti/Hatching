// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ColorAsLines"
{
    Properties
    {
        _MeshColor("Mesh Color", Color) = (0,0,1,1)
        _LineColor("Line Color", Color) = (1,1,0,1)
        _Size("Line Size", Float) = 0.1
    }
    SubShader
    {
    Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
 
            fixed4 _MeshColor;
           
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
           
            fixed4 frag (v2f i) : SV_Target
            {
                return _MeshColor;
            }
            ENDCG
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #include "UnityCG.cginc"
 
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float normal : NORMAL;
            };
            
            struct v2g
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
 
            struct g2f
            {
                float4 pos: POSITION;
            };
 
            fixed4 _LineColor;
            float _Size;
            float _ImageSpace;
           
            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.color = v.color;
                o.color.w = 0;
                return o;
            }
 
            [maxvertexcount(6)]
            void geo(triangle v2g v[3], inout LineStream<g2f> ls)
            {
                for(int i = 0; i < 3; i++)
                {              
                    float4 p1 = UnityObjectToClipPos(v[i].vertex);
                    float4 p2 = UnityObjectToClipPos(v[i].vertex + _Size*v[i].color);

                    g2f o1;
                    g2f o2;
                    o1.pos = p1/p1.w;
                    o2.pos = p2/p2.w;
                    ls.Append(o1);
                    ls.Append(o2);
                    ls.RestartStrip();
                }
            }
           
            fixed4 frag (g2f i) : SV_Target
            {
                return _LineColor;
            }
            ENDCG
        }
    }
}