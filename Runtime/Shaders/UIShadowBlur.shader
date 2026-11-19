Shader "Hidden/ProceduralUI/UIShadowBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _BlurDir;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 step = _BlurDir;

                fixed4 color = tex2D(_MainTex, uv) * 0.2270270270;
                color += tex2D(_MainTex, uv + step * 1.0) * 0.1945945946;
                color += tex2D(_MainTex, uv - step * 1.0) * 0.1945945946;
                color += tex2D(_MainTex, uv + step * 2.0) * 0.1216216216;
                color += tex2D(_MainTex, uv - step * 2.0) * 0.1216216216;
                color += tex2D(_MainTex, uv + step * 3.0) * 0.0540540541;
                color += tex2D(_MainTex, uv - step * 3.0) * 0.0540540541;
                color += tex2D(_MainTex, uv + step * 4.0) * 0.0162162162;
                color += tex2D(_MainTex, uv - step * 4.0) * 0.0162162162;
                return color;
            }
            ENDCG
        }
    }
}
