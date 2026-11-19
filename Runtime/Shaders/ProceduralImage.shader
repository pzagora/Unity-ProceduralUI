Shader "UI/Procedural Image"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("Sprite Alpha", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float3 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 radius : TEXCOORD0;
                float4 texcoord : TEXCOORD1;
                float2 size : TEXCOORD2;
                float3 border : TEXCOORD3;
                float4 mask : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float2 DecodeRadii(float value)
            {
                float2 result = frac(float2(1.0, 65535.0) * value);
                result.x -= result.y / 65535.0;
                return result;
            }

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.size = input.uv1.xy;
                output.texcoord = float4(TRANSFORM_TEX(input.uv0, _MainTex), input.uv1.zw);
                float minimumSide = min(output.size.x, output.size.y);
                output.border = float3(input.uv3.x * minimumSide * 0.5, clamp(input.uv3.y, 1.0 / 2048.0, 2048.0), input.uv2.z);
                output.radius = float4(DecodeRadii(input.uv2.x), DecodeRadii(input.uv2.y)) * minimumSide;
                output.color = input.color * _Color;
                float2 pixelSize = output.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clipRect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = float4(input.vertex.xy * 2 - clipRect.xy - clipRect.zw,
                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return output;
            }

            float EdgeDistance(float2 position, float4 radius, float2 size)
            {
                float4 edges = float4(position, size - position);
                float distanceToEdge = min(min(edges.x, edges.y), min(edges.z, edges.w));
                if (all(edges.xw < radius.x))
                    return radius.x - length(edges.xw - radius.x);
                if (all(edges.zw < radius.y))
                    return radius.y - length(edges.zw - radius.y);
                if (all(edges.zy < radius.z))
                    return radius.z - length(edges.zy - radius.z);
                if (all(edges.xy < radius.w))
                    return radius.w - length(edges.xy - radius.w);
                return distanceToEdge;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 sampled = tex2D(_MainTex, input.texcoord.xy) + _TextureSampleAdd;
                if (input.border.z > 0.5)
                    sampled.a = tex2D(_AlphaTex, input.texcoord.xy).r;
                fixed4 color = sampled * input.color;
                float distanceToEdge = EdgeDistance(input.texcoord.zw * input.size, input.radius, input.size);
                float halfBorder = (input.border.x + 1.0 / input.border.y) * 0.5;
                color.a *= saturate((halfBorder - abs(distanceToEdge - halfBorder)) * input.border.y);

                #ifdef UNITY_UI_CLIP_RECT
                float2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                color.a *= mask.x * mask.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
