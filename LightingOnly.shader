Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Hatching_Array ("Hatching", 2DArray) = "" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        CGPROGRAM
        #pragma surface surf Stepped
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Hatching[5];
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
        };

        //lighting function. Will be called once per light
        float4 LightingStepped(SurfaceOutput s, float3 lightDir, half3 viewDir) {
            float towardsLight = dot(s.Normal, lightDir);
            //float towardsLightChange = fwidth(towardsLight);
            float3 shadowColor = s.Albedo;
            float4 color;
            color.rgb = towardsLight;
            color.a = s.Alpha;
            return color;
        }

        void surf(Input IN, inout SurfaceOutput o) {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            //fixed3 c = tex2D (_Hatching[LIGHTRESULT*4], IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
