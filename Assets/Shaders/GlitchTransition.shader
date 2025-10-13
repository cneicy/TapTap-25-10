Shader "Custom/GlitchTransition"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.8
        _ChromaticAmount ("Chromatic Amount", Range(0, 0.15)) = 0.05
        _BlockSize ("Block Size", Range(5, 100)) = 20
        _MotionBlurAmount ("Motion Blur Amount", Range(0, 0.2)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Progress;
                float _GlitchIntensity;
                float _ChromaticAmount;
                float _BlockSize;
                float _MotionBlurAmount;
            CBUFFER_END
            
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float time = _Time.y;
                float progress = _Progress;
                
                // === 黑屏阶段 ===
                if (progress > 0.35 && progress < 0.65)
                {
                    return float4(0, 0, 0, 1);
                }
                
                // === 效果强度曲线（红蓝分色、运动残影、UV扰动都同步） ===
                float effectStrength = 0;        // 总效果强度
                float aberrationAmount = 0;      // 红蓝分色偏移量
                float ghostOpacity = 0;          // 重影透明度
                float motionBlurStrength = 0;    // 运动残影强度
                float distortionStrength = 0;    // UV扰动强度（撕裂、块状）
                
                if (progress < 0.15)
                {
                    // 0-15%: 所有效果同步淡入
                    float t = smoothstep(0.0, 0.15, progress);
                    effectStrength = t;
                    aberrationAmount = _ChromaticAmount * t;
                    ghostOpacity = t;
                    motionBlurStrength = t;
                    distortionStrength = 0; // 前期暂无UV扰动
                }
                else if (progress >= 0.15 && progress < 0.25)
                {
                    // 15-25%: 效果保持，UV扰动淡入
                    effectStrength = 1.0;
                    aberrationAmount = _ChromaticAmount;
                    ghostOpacity = 1.0;
                    motionBlurStrength = 1.0;
                    distortionStrength = smoothstep(0.15, 0.25, progress);
                }
                else if (progress >= 0.25 && progress <= 0.85)
                {
                    // 25-85%: 所有效果保持最大
                    effectStrength = 1.0;
                    aberrationAmount = _ChromaticAmount;
                    ghostOpacity = 1.0;
                    motionBlurStrength = 1.0;
                    distortionStrength = 1.0;
                }
                else if (progress > 0.85)
                {
                    // 85-100%: 所有效果同步淡出
                    float t = 1.0 - smoothstep(0.85, 1.0, progress);
                    effectStrength = t;
                    aberrationAmount = _ChromaticAmount * t;
                    ghostOpacity = t;
                    motionBlurStrength = t;
                    distortionStrength = t;
                }
                
                // === UV扰动（撕裂、块状错位）- 仅在中段出现 ===
                float2 distortedUV = uv;
                
                if (distortionStrength > 0)
                {
                    // 块状错位
                    float2 blockUV = floor(uv * _BlockSize) / _BlockSize;
                    float blockRand = random(blockUV + floor(time * 8.0));
                    
                    if (blockRand < distortionStrength * 0.7)
                    {
                        float2 blockOffset = float2(
                            (random(blockUV + float2(1, 0) + time) - 0.5),
                            (random(blockUV + float2(0, 1) + time) - 0.5)
                        ) * 0.25 * distortionStrength * _GlitchIntensity;
                        distortedUV += blockOffset;
                    }
                    
                    // 水平撕裂
                    float lineNoise = noise(float2(0, floor(uv.y * 50.0) + time * 10.0));
                    if (lineNoise > 1.0 - distortionStrength * 0.6)
                    {
                        float tearOffset = (random(float2(floor(uv.y * 50.0), time)) - 0.5) * 0.4 * distortionStrength * _GlitchIntensity;
                        distortedUV.x += tearOffset;
                    }
                }
                
                // === 基础颜色初始化 ===
                float4 color = float4(0, 0, 0, 1);
                
                // === 运动残影采样（多层累加）===
                if (motionBlurStrength > 0)
                {
                    // 采样5层运动残影
                    int numSamples = 5;
                    float3 accumulatedColor = float3(0, 0, 0);
                    
                    for (int i = 0; i < numSamples; i++)
                    {
                        float offset = float(i) / float(numSamples - 1); // 0 到 1
                        
                        // 每层有不同的时间偏移
                        float2 motionOffset = float2(
                            noise(uv * 4.0 + time + offset * 10.0) - 0.5,
                            noise(uv * 4.0 + time + offset * 10.0 + 50.0) - 0.5
                        ) * _MotionBlurAmount * motionBlurStrength * (1.0 - offset * 0.5);
                        
                        float2 sampleUV = distortedUV + motionOffset;
                        
                        // 每层都应用红蓝分色
                        float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV + float2(-aberrationAmount, 0)).r;
                        float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).g;
                        float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV + float2(aberrationAmount, 0)).b;
                        
                        // 权重：中心层权重最大，边缘层权重小
                        float weight = 1.0 - abs(offset - 0.5) * 0.8;
                        accumulatedColor += float3(r, g, b) * weight;
                    }
                    
                    // 平均颜色
                    color.rgb = accumulatedColor / float(numSamples);
                }
                else
                {
                    // 无运动残影时，直接采样
                    float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-aberrationAmount, 0)).r;
                    float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV).g;
                    float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(aberrationAmount, 0)).b;
                    color.rgb = float3(r, g, b);
                }
                
                // === 重影效果（叠加在残影之上）===
                if (ghostOpacity > 0)
                {
                    float4 ghost1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-aberrationAmount * 1.5, 0));
                    float4 ghost2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(aberrationAmount * 1.5, 0));
                    
                    color.rgb += ghost1.rgb * 0.25 * ghostOpacity;
                    color.rgb += ghost2.rgb * 0.25 * ghostOpacity;
                }
                
                // === 红蓝光晕 ===
                if (effectStrength > 0)
                {
                    color.r += effectStrength * 0.15;
                    color.b += effectStrength * 0.15;
                }
                
                // === 扫描线 ===
                float scanlineStrength = effectStrength;
                if (scanlineStrength > 0)
                {
                    float scanline = sin(uv.y * 400.0 + time * 20.0) * 0.5 + 0.5;
                    color.rgb -= scanline * 0.08 * scanlineStrength;
                }
                
                // === 淡入淡出 ===
                float alpha = 1.0;
                if (progress < 0.05)
                {
                    alpha = progress / 0.05;
                }
                else if (progress > 0.95)
                {
                    alpha = (1.0 - progress) / 0.05;
                }
                
                color.a = alpha;
                
                return saturate(color);
            }
            ENDHLSL
        }
    }
}