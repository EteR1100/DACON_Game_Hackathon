Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0, 10)) = 1
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
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                float thickness = _OutlineThickness;
                float upAlpha = tex2D(_MainTex, IN.texcoord + float2(0, _MainTex_TexelSize.y * thickness)).a;
                float downAlpha = tex2D(_MainTex, IN.texcoord - float2(0, _MainTex_TexelSize.y * thickness)).a;
                float rightAlpha = tex2D(_MainTex, IN.texcoord + float2(_MainTex_TexelSize.x * thickness, 0)).a;
                float leftAlpha = tex2D(_MainTex, IN.texcoord - float2(_MainTex_TexelSize.x * thickness, 0)).a;

                float outline = max(max(upAlpha, downAlpha), max(rightAlpha, leftAlpha));
                outline = saturate(outline - c.a);

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= outline * IN.color.a;

                c.rgb = lerp(c.rgb, outlineColor.rgb, outlineColor.a);
                c.a = max(c.a, outlineColor.a);

                return c;
            }
            ENDCG
        }
    }
}
