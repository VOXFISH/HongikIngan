Shader "HongikIngan/UI/StudentTransformation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Flash ("White flash", Range(0,1)) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex: POSITION; float4 color: COLOR; float2 uv: TEXCOORD0; };
            struct v2f { float4 vertex: SV_POSITION; fixed4 color: COLOR; float2 uv: TEXCOORD0; float4 position: TEXCOORD1; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _ClipRect;
            fixed4 _Color, _TextureSampleAdd;
            float _Flash;
            v2f vert(appdata v)
            {
                v2f o; o.position = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i): SV_Target
            {
                fixed4 s = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float glow = 0;
                // A soft, low-opacity halo follows the PNG silhouette, never a rectangle.
                for (int n = 0; n < 8; n++)
                {
                    float angle = n * .78539816;
                    float2 offset = float2(cos(angle), sin(angle)) * _MainTex_TexelSize.xy;
                    glow += tex2D(_MainTex, i.uv + offset * 3).a;
                    glow += tex2D(_MainTex, i.uv + offset * 7).a * .5;
                }
                glow = glow / 12 * .25 * _Flash * (1 - s.a);
                float alpha = s.a + glow;
                fixed3 rgb = (lerp(s.rgb, fixed3(1,1,1), _Flash) * s.a + glow) / max(alpha, .0001);
                fixed4 color = fixed4(rgb, alpha) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.position.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
