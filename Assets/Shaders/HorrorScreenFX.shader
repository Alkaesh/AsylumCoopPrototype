Shader "Hidden/AsylumHorror/HorrorScreenFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradeTint ("Grade Tint", Color) = (0.86, 0.94, 0.91, 1)
        _Exposure ("Exposure", Float) = 0.93
        _Contrast ("Contrast", Float) = 1.08
        _Saturation ("Saturation", Float) = 0.84
        _Vignette ("Vignette", Float) = 0.26
        _Grain ("Grain", Float) = 0.025
        _BloomThreshold ("Bloom Threshold", Float) = 0.66
        _BloomStrength ("Bloom Strength", Float) = 0.04
        _NoiseTime ("Noise Time", Float) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _GradeTint;
            float _Exposure;
            float _Contrast;
            float _Saturation;
            float _Vignette;
            float _Grain;
            float _BloomThreshold;
            float _BloomStrength;
            float _NoiseTime;

            float Random01(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float3 color = tex2D(_MainTex, uv).rgb;

                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float3 graded = lerp(luminance.xxx, color * _GradeTint.rgb, _Saturation);
                graded *= _Exposure;
                graded = ((graded - 0.5) * _Contrast) + 0.5;

                float highlight = saturate((luminance - _BloomThreshold) / max(0.001, 1.0 - _BloomThreshold));
                graded += color * highlight * _BloomStrength;

                float2 centered = uv * 2.0 - 1.0;
                float vignetteMask = saturate(1.0 - dot(centered, centered) * _Vignette);
                graded *= vignetteMask;

                float grain = (Random01(uv * _ScreenParams.xy + _NoiseTime * 47.31) - 0.5) * _Grain;
                graded += grain.xxx;

                return float4(saturate(graded), 1.0);
            }
            ENDCG
        }
    }
}
