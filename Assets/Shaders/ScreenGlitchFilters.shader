// ScreenGlitchFilters.shader (修正重影和残影效果)
Shader "Custom/ScreenGlitchFilters"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        
        // === CRT扫描线 ===
        [Header(CRT Scanline)]
        [Toggle] _EnableScanline ("Enable Scanline", Float) = 0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        _ScanlineFrequency ("Scanline Frequency", Range(100, 2000)) = 800
        _ScanlineBrightness ("Scanline Brightness", Range(0, 2)) = 1
        
        // === UV扰动 ===
        [Header(UV Distortion)]
        [Toggle] _EnableDistortion ("Enable Distortion", Float) = 0
        _DistortionIntensity ("Distortion Intensity", Range(0, 1)) = 0.5
        _BlockSize ("Block Size", Range(5, 100)) = 20
        
        // === 红蓝分色 ===
        [Header(Chromatic Aberration)]
        [Toggle] _EnableChromatic ("Enable Chromatic", Float) = 0
        _ChromaticIntensity ("Chromatic Intensity", Range(0, 1)) = 0.5
        _ChromaticAmount ("Chromatic Amount", Range(0, 0.15)) = 0.03
        
        // === 重影（流光法多重影像）===
        [Header(Ghost Effect)]
        [Toggle] _EnableGhost ("Enable Ghost", Float) = 0
        _GhostIntensity ("Ghost Intensity", Range(0, 1)) = 0.5
        _GhostCount ("Ghost Count", Range(2, 8)) = 4
        _GhostSpread ("Ghost Spread", Range(0, 0.1)) = 0.03
        
        // === 红蓝光晕 ===
        [Header(Color Glow)]
        [Toggle] _EnableGlow ("Enable Glow", Float) = 0
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.5
        
        // === 运动残影（光流法拖尾）===
        [Header(Motion Trail)]
        [Toggle] _EnableMotionBlur ("Enable Motion Trail", Float) = 0
        _MotionBlurIntensity ("Trail Intensity", Range(0, 1)) = 0.5
        _MotionBlurAmount ("Trail Length", Range(0, 0.2)) = 0.05
        _TrailCount ("Trail Count", Range(2, 10)) = 5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off
            
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
                
                float _EnableDistortion;
                float _DistortionIntensity;
                float _BlockSize;
                
                float _EnableChromatic;
                float _ChromaticIntensity;
                float _ChromaticAmount;
                
                float _EnableGhost;
                float _GhostIntensity;
                float _GhostCount;
                float _GhostSpread;
                
                float _EnableGlow;
                float _GlowIntensity;
                
                float _EnableMotionBlur;
                float _MotionBlurIntensity;
                float _MotionBlurAmount;
                float _TrailCount;

                float _EnableScanline;
                float _ScanlineIntensity;
                float _ScanlineFrequency;
                float _ScanlineBrightness;
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
                OUT.uv = IN.uv;
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float time = _Time.y;
                
                // === 滤镜1: UV扰动 ===
                float2 distortedUV = uv;
                
                if (_EnableDistortion > 0.5)
                {
                    // 块状错位
                    float2 blockUV = floor(uv * _BlockSize) / _BlockSize;
                    float blockRand = random(blockUV + floor(time * 8.0));
                    
                    if (blockRand < _DistortionIntensity * 0.7)
                    {
                        float2 blockOffset = float2(
                            (random(blockUV + float2(1, 0) + time) - 0.5),
                            (random(blockUV + float2(0, 1) + time) - 0.5)
                        ) * 0.25 * _DistortionIntensity;
                        distortedUV += blockOffset;
                    }
                    
                    // 水平撕裂
                    float lineNoise = noise(float2(0, floor(uv.y * 50.0) + time * 10.0));
                    if (lineNoise > 1.0 - _DistortionIntensity * 0.6)
                    {
                        float tearOffset = (random(float2(floor(uv.y * 50.0), time)) - 0.5) * 0.4 * _DistortionIntensity;
                        distortedUV.x += tearOffset;
                    }
                }
                
                // === 计算红蓝分色偏移量 ===
                float aberrationAmount = 0;
                if (_EnableChromatic > 0.5)
                {
                    aberrationAmount = _ChromaticAmount * _ChromaticIntensity;
                }
                
                // === 基础采样（带红蓝分色）===
                float4 baseColor = float4(0, 0, 0, 1);
                
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-aberrationAmount, 0)).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(aberrationAmount, 0)).b;
                baseColor.rgb = float3(r, g, b);
                
                // === 滤镜2: 运动残影（流光法清晰拖尾）===
                float4 color = baseColor;
                
                if (_EnableMotionBlur > 0.5)
                {
                    // 计算运动方向（基于噪声模拟光流向量）
                    float2 motionDir = normalize(float2(
                        noise(uv * 3.0 + time * 0.5) - 0.5,
                        noise(uv * 3.0 + time * 0.5 + 100.0) - 0.5
                    ));
                    
                    // 采样多个清晰的残影
                    int trailCount = (int)_TrailCount;
                    float3 trailAccum = baseColor.rgb;
                    
                    for (int i = 1; i < trailCount; i++)
                    {
                        float t = float(i) / float(trailCount - 1);
                        
                        // 沿运动方向的偏移
                        float2 trailOffset = motionDir * _MotionBlurAmount * t * _MotionBlurIntensity;
                        
                        // 添加一些随机偏移，模拟光流不准确
                        float2 jitter = float2(
                            (random(uv + float2(i, 0) + time) - 0.5) * 0.01,
                            (random(uv + float2(0, i) + time) - 0.5) * 0.01
                        ) * _MotionBlurIntensity;
                        
                        float2 sampleUV = distortedUV - trailOffset + jitter;
                        
                        // 采样清晰的残影（带红蓝分色）
                        float trail_r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV + float2(-aberrationAmount, 0)).r;
                        float trail_g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).g;
                        float trail_b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV + float2(aberrationAmount, 0)).b;
                        
                        // 每个残影的权重递减
                        float weight = (1.0 - t * 0.7) * _MotionBlurIntensity;
                        trailAccum += float3(trail_r, trail_g, trail_b) * weight;
                    }
                    
                    // 归一化（不是平均，而是叠加后压缩）
                    color.rgb = saturate(trailAccum / (1.0 + _MotionBlurIntensity * 2.0));
                }
                
                // === 滤镜3: 重影效果（流光法多重影像）===
                if (_EnableGhost > 0.5)
                {
                    int ghostCount = (int)_GhostCount;
                    float3 ghostAccum = float3(0, 0, 0);
                    
                    for (int j = 0; j < ghostCount; j++)
                    {
                        // 每个重影有不同的偏移方向和距离
                        float angle = 6.28318 * float(j) / float(ghostCount) + time * 0.5;
                        float distance = _GhostSpread * (0.5 + 0.5 * sin(time * 2.0 + float(j)));
                        
                        float2 ghostOffset = float2(cos(angle), sin(angle)) * distance * _GhostIntensity;
                        
                        // 添加色差偏移，让每个重影也有红蓝分色
                        float2 ghostUV = distortedUV + ghostOffset;
                        
                        float ghost_r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, ghostUV + float2(-aberrationAmount * 0.5, 0)).r;
                        float ghost_g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, ghostUV).g;
                        float ghost_b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, ghostUV + float2(aberrationAmount * 0.5, 0)).b;
                        
                        // 每个重影都是清晰的
                        float ghostAlpha = 0.3 * _GhostIntensity / float(ghostCount);
                        ghostAccum += float3(ghost_r, ghost_g, ghost_b) * ghostAlpha;
                    }
                    
                    // 叠加重影（additive）
                    color.rgb = saturate(color.rgb + ghostAccum);
                }
                
                // === 滤镜4: 红蓝光晕 ===
                if (_EnableGlow > 0.5)
                {
                    color.r += _GlowIntensity * 0.15;
                    color.b += _GlowIntensity * 0.15;
                }

                // === 滤镜5: CRT扫描线 ===
                if (_EnableScanline > 0.5)
                {
                    // 基于Y轴的扫描线波形（sin波）
                    float scan = sin(uv.y * _ScanlineFrequency);
                    // 将sin波调整为 [0,1] 区间
                    scan = (scan * 0.5 + 0.5);
                    // 调整亮度和强度
                    float brightness = lerp(1.0 - _ScanlineIntensity, 1.0, scan) * _ScanlineBrightness;
                    color.rgb *= brightness;
                }
                
                return saturate(color);
                
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}