Shader "Kingdoms/Village Ground"
{
    Properties
    {
        _GrassColor ("Grass", Color) = (0.32, 0.52, 0.16, 1)
        _OuterColor ("Surrounding Grass", Color) = (0.19, 0.34, 0.12, 1)
        _GridColor ("Grid", Color) = (0.27, 0.43, 0.13, 1)
        _VillageHalfSize ("Village Half Size", Float) = 22
        _CellSize ("Grid Cell Size", Float) = 1
        _GridStrength ("Grid Visibility", Range(0,1)) = 0.28
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Ground"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _GrassColor, _OuterColor, _GridColor;
                float _VillageHalfSize, _CellSize, _GridStrength;
            CBUFFER_END
            float _KingdomsPlacementGrid;
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float edge = max(abs(p.x), abs(p.y));
                float village = 1 - smoothstep(_VillageHalfSize - 0.04, _VillageHalfSize + 0.04, edge);
                float2 cell = p / max(_CellSize, 0.01);
                float2 cellWidth = max(fwidth(cell), 0.001);
                float2 lines = abs(frac(cell + 0.5) - 0.5) / cellWidth;
                float grid = 1 - saturate(min(lines.x, lines.y));
                float checker = fmod(abs(floor(cell.x*.25) + floor(cell.y*.25)), 2) * 0.022;
                float grass=Noise(p*.7)*.1+Noise(p*5)*.055;
                float grain=(Noise(p*35)-.5)*.07/(1+length(fwidth(p))*16);
                half3 color = lerp(_OuterColor.rgb, _GrassColor.rgb + checker, village);
                color+=grass+grain-.045;
                color = lerp(color, _GridColor.rgb, grid * village * _GridStrength * _KingdomsPlacementGrid);
                float border = (1 - smoothstep(0.06, 0.15, abs(edge - _VillageHalfSize)));
                color = lerp(color, half3(0.57, 0.62, 0.28), border * 0.65 * _KingdomsPlacementGrid);
                float coast=p.x+29+Noise(float2(p.y*.2,0))*1.8;
                float shore=1-smoothstep(0,2,coast);
                color=lerp(color,half3(.56,.44,.24)+Noise(p*4)*.04,shore);
                float water=1-smoothstep(-.8,.05,coast);
                float ripple=pow(saturate(sin(p.x*4+p.y*.17+Noise(p*.4)*2)),12)*.035;
                color=lerp(color,half3(.065,.32,.4)+ripple,water);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half illumination = 0.55 + 0.45 * saturate(light.direction.y) * light.shadowAttenuation;
                return half4(color * illumination, 1);
            }
            ENDHLSL
        }
    }
}
