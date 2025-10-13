Shader "Custom/RectTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rows ("Rows", Float) = 8
        _Columns ("Columns", Float) = 12
        _GapSize ("Gap Size", Range(0, 1)) = 0.05
        _ExpandProgress ("Expand Progress", Range(0, 1)) = 0
        _Color1 ("Color 1", Color) = (0, 0, 0, 1)
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)
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
            Name "RectTransition"
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
                float _Rows;
                float _Columns;
                float _GapSize;
                float _ExpandProgress;
                float4 _Color1;
                float4 _Color2;
            CBUFFER_END
            
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
                
                float2 gridUV = float2(uv.x * _Columns, uv.y * _Rows);
                float2 gridID = floor(gridUV);
                
                float2 gridCenter = (gridID + 0.5) / float2(_Columns, _Rows);
                
                float2 center = float2(0.5, 0.5);
                float2 dirFromCenter = normalize(gridCenter - center);
                float distFromCenter = length(gridCenter - center);
                
                float2 gapOffset = dirFromCenter * _GapSize;
                float2 adjustedGridCenter = gridCenter + gapOffset;
                
                float2 rectSize = float2(1.0 / _Columns, 1.0 / _Rows);
                float2 adjustedRectMin = adjustedGridCenter - rectSize * 0.5;
                float2 adjustedRectMax = adjustedGridCenter + rectSize * 0.5;
                
                bool inGap = (uv.x < adjustedRectMin.x || uv.x > adjustedRectMax.x ||
                             uv.y < adjustedRectMin.y || uv.y > adjustedRectMax.y);
                
                if (inGap)
                {
                    discard;
                }
                
                if (_ExpandProgress > 0.0)
                {
                    float disappearThreshold = distFromCenter * 1.5;

                    float expandAmount = max(0, _ExpandProgress - disappearThreshold);
                    float2 expandOffset = dirFromCenter * expandAmount * 2.0;

                    float2 expandedCenter = gridCenter + expandOffset;
                    float2 expandedRectMin = expandedCenter - rectSize * 0.5;
                    float2 expandedRectMax = expandedCenter + rectSize * 0.5;

                    bool outOfBounds = (uv.x < expandedRectMin.x || uv.x > expandedRectMax.x ||
                                       uv.y < expandedRectMin.y || uv.y > expandedRectMax.y);
                    
                    if (outOfBounds || expandAmount > 0.5)
                    {
                        discard;
                    }

                    float fadeOut = 1.0 - saturate(expandAmount * 2.0);

                    bool isBlack = fmod(gridID.x + gridID.y, 2.0) < 0.5;
                    float4 color = isBlack ? _Color1 : _Color2;
                    color.a *= fadeOut;
                    
                    return color;
                }

                bool isBlack = fmod(gridID.x + gridID.y, 2.0) < 0.5;
                return isBlack ? _Color1 : _Color2;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}