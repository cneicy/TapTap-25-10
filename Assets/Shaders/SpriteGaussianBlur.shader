Shader "Custom/SpriteGaussianBlur"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,0.3)
        _BlurSize ("Blur Size", Range(0, 10)) = 2
        _EdgeHighlight ("Edge Highlight", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SpriteGaussianBlur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraOpaqueTexture;
            float4 _MainTex_ST;
            float4 _TintColor;
            float _BlurSize;
            float _EdgeHighlight;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float2 pixel = _BlurSize / _ScreenParams.xy;

                // --- Gaussian Blur Sample ---
                fixed4 blur = 0;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(-pixel.x, -pixel.y)) * 0.0625;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(0, -pixel.y)) * 0.125;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(pixel.x, -pixel.y)) * 0.0625;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(-pixel.x, 0)) * 0.125;
                blur += tex2D(_CameraOpaqueTexture, uv) * 0.25;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(pixel.x, 0)) * 0.125;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(-pixel.x, pixel.y)) * 0.0625;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(0, pixel.y)) * 0.125;
                blur += tex2D(_CameraOpaqueTexture, uv + float2(pixel.x, pixel.y)) * 0.0625;

                // --- Tint + light bloom effect ---
                fixed4 color = blur;
                color = lerp(color, _TintColor, _TintColor.a);

                // --- Edge highlight for glass depth ---
                float edge = smoothstep(0.0, 0.1, abs(i.uv.x - 0.5)) + smoothstep(0.0, 0.1, abs(i.uv.y - 0.5));
                color.rgb += edge * _EdgeHighlight * 0.1;

                // --- Sprite alpha mask ---
                fixed4 mask = tex2D(_MainTex, i.uv);
                color.a = mask.a * _TintColor.a;

                return color;
            }
            ENDHLSL
        }
    }
}
