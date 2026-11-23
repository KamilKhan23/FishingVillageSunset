// Hand-written sunset skybox for the Fishing Village.
// Three-band gradient (zenith / upper sky / horizon), a low sun disc with a wide halo,
// and procedural streaky clouds lit pink-orange on the sun side and lilac-grey away from it.
Shader "FishingVillage/SunsetSkybox"
{
    Properties
    {
        _ZenithColor ("Zenith Colour", Color) = (0.17, 0.20, 0.42, 1)
        _UpperColor ("Upper Sky Colour", Color) = (0.62, 0.42, 0.55, 1)
        _HorizonColor ("Horizon Colour", Color) = (1.0, 0.60, 0.36, 1)
        _GroundColor ("Below Horizon Colour", Color) = (0.42, 0.30, 0.32, 1)
        _SunDirection ("Direction Towards Sun", Vector) = (-0.85, 0.12, 0.5, 0)
        _SunColor ("Sun Colour", Color) = (1.0, 0.72, 0.42, 1)
        _SunSize ("Sun Disc Size (radians)", Range(0.005, 0.1)) = 0.035
        _SunDiscBrightness ("Sun Disc Brightness", Range(0, 8)) = 4
        _HaloStrength ("Halo Strength", Range(0, 2)) = 0.85
        _HaloTightness ("Halo Tightness", Range(1, 128)) = 9
        _HorizonGlow ("Horizon Glow Towards Sun", Range(0, 2)) = 0.6
        _CloudLitColor ("Cloud Lit Colour", Color) = (1.0, 0.62, 0.48, 1)
        _CloudShadeColor ("Cloud Shade Colour", Color) = (0.45, 0.36, 0.50, 1)
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.52
        _CloudScale ("Cloud Scale", Range(0.1, 5)) = 1.3
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ZenithColor, _UpperColor, _HorizonColor, _GroundColor, _SunColor, _CloudLitColor, _CloudShadeColor;
            float4 _SunDirection;
            float _SunSize, _SunDiscBrightness, _HaloStrength, _HaloTightness, _HorizonGlow;
            float _CloudCoverage, _CloudScale, _CloudOpacity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float sum = 0.0, amp = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    sum += valueNoise(p) * amp;
                    p = p * 2.03 + float2(17.1, 9.7);
                    amp *= 0.5;
                }
                return sum;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(d, sunDir);
                float towardSun = saturate(sunDot * 0.5 + 0.5);
                float y = d.y;

                // Sky gradient
                float up = saturate(y);
                float3 col = lerp(_HorizonColor.rgb, _UpperColor.rgb, smoothstep(0.0, 0.22, up));
                col = lerp(col, _ZenithColor.rgb, smoothstep(0.15, 0.75, up));

                // The horizon burns brighter on the sun side and cools away from it
                float horizonBand = 1.0 - smoothstep(0.0, 0.3, up);
                col += _SunColor.rgb * pow(towardSun, 3.0) * horizonBand * _HorizonGlow;
                col = lerp(col, col * float3(0.78, 0.80, 0.98), (1.0 - towardSun) * horizonBand * 0.5);

                // Below the horizon (hidden by sea and fog, but keeps reflections warm)
                col = lerp(col, _GroundColor.rgb, smoothstep(0.0, 0.1, -y));

                // Clouds: streaks stretched along the horizon, kept to the lower half of the sky
                if (y > 0.0)
                {
                    float2 p = d.xz / (y + 0.12) * _CloudScale;
                    float n = fbm(p * float2(0.45, 1.6));
                    float cloud = smoothstep(_CloudCoverage, _CloudCoverage + 0.22, n);
                    cloud *= smoothstep(0.01, 0.07, y) * (1.0 - smoothstep(0.35, 0.75, y));
                    float lit = pow(towardSun, 4.0);
                    float3 cloudCol = lerp(_CloudShadeColor.rgb, _CloudLitColor.rgb, lit);
                    cloudCol += _SunColor.rgb * pow(saturate(sunDot), 24.0) * 1.2; // bright rims near the sun
                    col = lerp(col, cloudCol, cloud * _CloudOpacity);
                }

                // Halo and sun disc
                col += _SunColor.rgb * pow(saturate(sunDot), _HaloTightness) * _HaloStrength;
                float disc = smoothstep(cos(_SunSize * 1.2), cos(_SunSize), sunDot) * smoothstep(-0.01, 0.01, y);
                col = lerp(col, _SunColor.rgb * _SunDiscBrightness, disc);

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
