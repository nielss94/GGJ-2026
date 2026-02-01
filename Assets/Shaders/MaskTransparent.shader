Shader "Unlit/MaskTransparent"
{
    Properties
    {
        [Header(Mask)]
        _MaskTex ("Mask Texture (black = transparent, white = opaque)", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MaskTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MaskTex, i.uv);
                // Luminance: black = 0 (transparent), white = 1 (opaque)
                fixed alpha = dot(mask.rgb, fixed3(0.299, 0.587, 0.114));
                alpha *= mask.a;
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
