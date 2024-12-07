Shader "Forever/Environment/MagicalPlant"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _GrowthMap ("Growth Map", 2D) = "white" {}
        
        [Header(Surface)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
        
        [Header(Growth)]
        _GrowthProgress ("Growth Progress", Range(0,1)) = 0.0
        _GrowthDirection ("Growth Direction", Vector) = (0,1,0,0)
        _GrowthScale ("Growth Scale", Range(0,2)) = 1.0
        _GrowthCurve ("Growth Curve", Range(0.1,5)) = 2.0
        
        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0,2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0,10)) = 1.0
        _WindDirection ("Wind Direction", Vector) = (1,0,0,0)
        
        [Header(Magic)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 1.0
        _PulseAmplitude ("Pulse Amplitude", Range(0,2)) = 1.0
        _MagicIntensity ("Magic Intensity", Range(0,1)) = 0.5
        _ColorChangeSpeed ("Color Change Speed", Range(0,10)) = 1.0
        _ColorChangeRange ("Color Change Range", Range(0,1)) = 0.2
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 4.0
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_EmissionMap;
            float3 worldPos;
            float growthFactor;
        };
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _EmissionMap;
        sampler2D _GrowthMap;
        sampler2D _NoiseTexture;
        
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        float _NormalStrength;
        
        float _GrowthProgress;
        float4 _GrowthDirection;
        float _GrowthScale;
        float _GrowthCurve;
        
        float _WindStrength;
        float _WindSpeed;
        float4 _WindDirection;
        
        fixed4 _EmissionColor;
        float _PulseSpeed;
        float _PulseAmplitude;
        float _MagicIntensity;
        float _ColorChangeSpeed;
        float _ColorChangeRange;
        
        // Noise functions
        float hash(float2 p)
        {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        }
        
        float noise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = hash(i + float2(0.0, 0.0));
            float b = hash(i + float2(1.0, 0.0));
            float c = hash(i + float2(0.0, 1.0));
            float d = hash(i + float2(1.0, 1.0));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Calculate growth factor based on vertex position
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float heightFactor = dot(v.vertex.xyz, _GrowthDirection.xyz);
            float growthFactor = saturate((heightFactor + 1.0) * 0.5);
            
            // Sample growth map
            float growth = tex2Dlod(_GrowthMap, float4(v.texcoord.xy, 0, 0)).r;
            
            // Apply growth transformation
            float growthMask = pow(growthFactor, _GrowthCurve) * growth;
            float finalGrowth = saturate(_GrowthProgress - growthMask);
            v.vertex.xyz += _GrowthDirection.xyz * finalGrowth * _GrowthScale;
            
            // Apply wind effect
            float3 wind = _WindDirection.xyz * _WindStrength;
            float time = _Time.y * _WindSpeed;
            float2 noiseUV = worldPos.xz * 0.1;
            float windNoise = noise(noiseUV + time);
            float windFactor = growthFactor * windNoise;
            v.vertex.xyz += wind * windFactor * (1.0 - growthMask);
            
            // Store growth factor for fragment shader
            o.growthFactor = growthFactor;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Sample base textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            fixed4 e = tex2D(_EmissionMap, IN.uv_EmissionMap);
            
            // Apply normal strength
            n = lerp(float3(0,0,1), n, _NormalStrength);
            
            // Calculate magic effect
            float time = _Time.y * _ColorChangeSpeed;
            float2 magicUV = IN.worldPos.xz * 0.1;
            float magicNoise = noise(magicUV + time);
            float magic = saturate(magicNoise * 0.5 + 0.5) * _MagicIntensity;
            
            // Calculate color change
            float3 hsvColor = rgb2hsv(c.rgb);
            hsvColor.x += sin(time + IN.worldPos.y) * _ColorChangeRange * magic;
            c.rgb = hsv2rgb(hsvColor);
            
            // Calculate pulsing emission
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            float3 emission = _EmissionColor.rgb * e.rgb * (1.0 + pulse * _PulseAmplitude);
            emission += _EmissionColor.rgb * magic * IN.growthFactor;
            
            // Apply growth factor to transparency
            float growthAlpha = saturate(_GrowthProgress - (1.0 - IN.growthFactor));
            
            o.Albedo = c.rgb;
            o.Normal = n;
            o.Emission = emission;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * growthAlpha;
        }
        
        // Color space conversion functions
        float3 rgb2hsv(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }
        
        float3 hsv2rgb(float3 c)
        {
            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
        }
        
        ENDCG
    }
    FallBack "Diffuse"
} 