Shader "Hidden/Effects/TVOff"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _LineMin ("Min Line Thickness", Range(0.0005, 0.02)) = 0.003
        _Softness ("Edge Softness", Range(0.0, 0.05)) = 0.01
        _Glow ("Glow Strength", Range(0, 2)) = 0.6
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _Distortion ("Barrel Distortion", Range(-0.2, 0.2)) = 0.05
        _Scanline ("Scanline Strength", Range(0, 1)) = 0.2
        _Noise ("Noise Strength", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Progress;
            float _LineMin, _Softness, _Glow, _Vignette, _Distortion, _Scanline, _Noise;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            // 简易hash噪声
            float hash21(float2 p){
                p = frac(p*float2(123.34,456.21));
                p += dot(p, p+45.32);
                return frac(p.x*p.y);
            }

            float2 barrel(float2 uv, float k){ // 轻微桶形畸变
                float2 c = uv*2-1;
                float r2 = dot(c,c);
                c *= (1 + k*r2);
                return c*0.5 + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 0→1：前0.85做“水平亮线收缩”，最后0.15做“亮点”
                float p1 = saturate(_Progress / 0.85);               // 线阶段进度
                float p2 = saturate((_Progress - 0.85) / 0.15);      // 点阶段进度

                // 轻微桶形畸变 + 扫描线抖动
                float2 uv = barrel(i.uv, _Distortion * (0.2 + 0.8*p1));
                float scan = (sin(uv.y * 1200.0 + _Time.y * 12.0) * 0.5 + 0.5) * _Scanline * p1;

                // 取样源
                fixed4 col = tex2D(_MainTex, uv);

                // —— 收缩遮罩 —— //
                float thicknessY = lerp(1.0, _LineMin, p1);          // 竖向收缩到极细
                float dY = abs(uv.y - 0.5);
                float lineMask = 1.0 - smoothstep(thicknessY, thicknessY + _Softness, dY);

                // 点阶段半径从0.5收缩到_LineMin
                float dotRadius = lerp(0.5, _LineMin, p2);
                float2 dc = uv - 0.5;
                float r = length(dc);
                float dotMask = 1.0 - smoothstep(dotRadius, dotRadius + _Softness, r);

                // 两阶段过渡（前期主line，后期主dot）
                float visMask = lerp(lineMask, dotMask, p2);          // 0~1
                // 边缘晕光（线与点）
                float lineGlow = pow(saturate(1.0 - dY / (thicknessY + 1e-5)), 6) * (1.0 - p2);
                float dotGlow  = pow(saturate(1.0 - r  / (dotRadius  + 1e-5)), 8) * p2;

                // 轻微闪白峰值（接近“关机”的瞬间）
                float spike = exp(-pow((_Progress - 0.88)/0.06, 2)) * 0.35;

                // 暗角
                float2 uvN = (uv - 0.5)/0.5; // -1~1
                float vign = 1.0 - saturate(length(uvN));
                float vigMul = 1.0 - (1.0 - vign) * _Vignette * (0.2 + 0.8*(1.0 - visMask));

                // 噪声（靠前期更明显）
                float n = (hash21(uv + _Time.y) - 0.5) * _Noise * (1.0 - _Progress);

                // 应用遮罩：主体内容被“压扁到线/点”，周围逐渐熄灭
                fixed3 baseCol = col.rgb * visMask;
                // 加上亮线/亮点的辉光 + 闪白
                baseCol += _Glow * (lineGlow + dotGlow);
                baseCol += spike;

                // 扫描线调制 & 暗角 & 噪声
                baseCol *= (1.0 - 0.12*scan);
                baseCol = baseCol * vigMul + n;

                // 完全未触发时不改画面
                if (_Progress <= 0.0001) return col;
                return fixed4(baseCol, col.a);
            }
            ENDCG
        }
    }
    FallBack Off
}
