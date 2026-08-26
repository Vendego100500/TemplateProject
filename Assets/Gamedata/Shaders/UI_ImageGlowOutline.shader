Shader "UI/Image Glow Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 20)) = 4
        _OutlineSoftness ("Outline Softness", Range(0, 1)) = 0.6
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.1
        [Toggle] _OutlineOnly ("Outline Only", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "UI_ImageGlowOutline"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 uvScale : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _AlphaThreshold;
            float _OutlineOnly;
            int _UIVertexColorAlwaysGammaSpace;

            float2 GetExpandDirection(float2 texcoord)
            {
                return float2(
                    texcoord.x < 0.01 ? -1.0 : (texcoord.x > 0.99 ? 1.0 : 0.0),
                    texcoord.y < 0.01 ? -1.0 : (texcoord.y > 0.99 ? 1.0 : 0.0)
                );
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float pad = max(_OutlineWidth, 0.0) * (1.0 + _OutlineSoftness * 0.5);
                float2 halfSize = max(abs(input.vertex.xy), float2(0.001, 0.001));
                float2 expandDir = GetExpandDirection(input.texcoord);

                float4 expandedVertex = input.vertex;
                expandedVertex.xy += expandDir * pad;

                output.worldPosition = expandedVertex;
                output.vertex = UnityObjectToClipPos(expandedVertex);

                output.uvScale = halfSize / (halfSize + pad);
                output.texcoord = (input.texcoord - 0.5) / output.uvScale + 0.5;

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    input.color.rgb = UIGammaToLinear(input.color.rgb);
                }

                output.color = input.color * _Color;
                return output;
            }

            fixed SampleAlpha(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                {
                    return 0.0;
                }

                fixed alpha = tex2D(_MainTex, uv).a;
                return alpha >= _AlphaThreshold ? alpha : 0.0;
            }

            fixed GetOutlineMask(float2 uv, float2 uvScale, fixed centerAlpha)
            {
                const int directionCount = 8;
                const int stepCount = 6;

                float2 texel = _MainTex_TexelSize.xy / max(uvScale, float2(0.001, 0.001));
                float width = max(_OutlineWidth, 0.001);
                float softness = saturate(_OutlineSoftness);
                fixed outline = 0.0;

                [unroll]
                for (int direction = 0; direction < directionCount; direction++)
                {
                    float angle = direction * 6.2831853 / directionCount;
                    float2 dir = float2(cos(angle), sin(angle));

                    [unroll]
                    for (int step = 1; step <= stepCount; step++)
                    {
                        float distance = (float)step / stepCount * width;
                        fixed neighborAlpha = SampleAlpha(uv + dir * texel * distance);
                        fixed falloff = 1.0 - smoothstep(softness, 1.0, distance / width);
                        outline = max(outline, neighborAlpha * falloff);
                    }
                }

                return saturate(outline - centerAlpha);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed spriteAlpha = SampleAlpha(input.texcoord);
                fixed4 sprite = tex2D(_MainTex, saturate(input.texcoord)) + _TextureSampleAdd;
                sprite.a = spriteAlpha;

                fixed centerAlpha = sprite.a * input.color.a;
                fixed outlineMask = GetOutlineMask(input.texcoord, input.uvScale, sprite.a);

                fixed4 outline = _OutlineColor;
                outline.rgb *= outline.a;
                outline.a *= outlineMask * input.color.a;

                fixed4 color;
                if (_OutlineOnly > 0.5)
                {
                    color = outline;
                }
                else
                {
                    color.rgb = lerp(sprite.rgb * input.color.rgb, outline.rgb, outlineMask);
                    color.a = max(centerAlpha, outline.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
