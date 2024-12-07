Shader "Forever/Environment/GlowingMushroom"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _PatternMap ("Pattern Map", 2D) = "white" {}
        
        [Header(Surface)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Translucency ("Translucency", Range(0,1)) = 0.5
        _TranslucencyColor ("Translucency Color", Color) = (1,1,1,1)
        
        [Header(Glow)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _GlowPattern ("Glow Pattern", Range(0,5)) = 1.0
        _GlowSpeed ("Glow Speed", Range(0,10)) = 1.0
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1.0
        
        [Header(Reaction)]
        _ReactionRadius ("Reaction Radius", Range(0,5)) = 1.0
        _ReactionSpeed ("Reaction Speed", Range(0,10)) = 1.0
        _ReactionIntensity ("Reaction Intensity", Range(0,2)) = 1.0
        [HDR] _ReactionColor ("Reaction Color", Color) = (1,1,1,1)
        
        [Header(Animation)]
        _SwayAmount ("Sway Amount", Range(0,1)) = 0.1
        _SwaySpeed ("Sway Speed", Range(0,10)) = 1.0
        _SwayDirection ("Sway Direction", Vector) = (1,0,1,0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf StandardTranslucent alpha:fade vertex:vert
        #pragma target 4.0
        
        #include "UnityPBSLighting.cginc"
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_EmissionMap;
            float3 worldPos;
            float3 viewDir;
            float height;
        };
        
        struct SurfaceOutputStandardTranslucent
        {
            fixed3 Albedo;
            fixed3 Normal;
            half3 Emission;
            half Metallic;
            half Smoothness;
            half Translucency;
            fixed3 TranslucencyColor;
            half Alpha;
        };
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _EmissionMap;
        sampler2D _PatternMap;
        
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        half _Translucency;
        fixed4 _TranslucencyColor;
        
        fixed4 _EmissionColor;
        float _GlowPattern;
        float _GlowSpeed;
        float _GlowIntensity;
        
        float _ReactionRadius;
        float _ReactionSpeed;
        float _ReactionIntensity;
        fixed4 _ReactionColor;
        
        float _SwayAmount;
        float _SwaySpeed;
        float4 _SwayDirection;
        
        // Global reaction points from shader manager
        float4 _ReactionPoints[10];
        int _ReactionPointCount;
        
        // Noise function
        float hash(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }
        
        float noise(float3 x)
        {
            float3 p = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            
            return lerp(lerp(lerp(hash(p + float3(0,0,0)), 
                               hash(p + float3(1,0,0)), f.x),
                          lerp(hash(p + float3(0,1,0)), 
                               hash(p + float3(1,1,0)), f.x), f.y),
                     lerp(lerp(hash(p + float3(0,0,1)), 
                               hash(p + float3(1,0,1)), f.x),
                          lerp(hash(p + float3(0,1,1)), 
                               hash(p + float3(1,1,1)), f.x), f.y), f.z);
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Calculate world position
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            // Apply sway animation
            float time = _Time.y * _SwaySpeed;
            float3 swayDir = normalize(_SwayDirection.xyz);
            float swayFactor = sin(time + worldPos.x * 0.5) * _SwayAmount;
            v.vertex.xyz += swayDir * swayFactor * v.texcoord.y;
            
            // Store height for glow pattern
            o.height = v.vertex.y;
            o.worldPos = worldPos;
        }
        
        half4 LightingStandardTranslucent(SurfaceOutputStandardTranslucent s, half3 viewDir, UnityGI gi)
        {
            // Standard PBS lighting
            SurfaceOutputStandard r;
            r.Albedo = s.Albedo;
            r.Normal = s.Normal;
            r.Emission = s.Emission;
            r.Metallic = s.Metallic;
            r.Smoothness = s.Smoothness;
            r.Alpha = s.Alpha;
            
            half4 pbsLight = LightingStandard(r, viewDir, gi);
            
            // Add translucency
            float3 lightDir = gi.light.dir;
            float3 lightColor = gi.light.color;
            float3 normal = s.Normal;
            
            float translucencyDot = pow(saturate(dot(-normal, lightDir)), 1);
            float3 translucencyColor = s.TranslucencyColor * lightColor * translucencyDot * s.Translucency;
            
            pbsLight.rgb += translucencyColor;
            
            return pbsLight;
        }
        
        void surf(Input IN, inout SurfaceOutputStandardTranslucent o)
        {
            // Sample base textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            fixed4 e = tex2D(_EmissionMap, IN.uv_EmissionMap);
            fixed4 pattern = tex2D(_PatternMap, IN.uv_MainTex);
            
            // Calculate glow pattern
            float time = _Time.y * _GlowSpeed;
            float glowNoise = noise(float3(IN.worldPos.xz * _GlowPattern, time));
            float glow = saturate(glowNoise * pattern.r);
            
            // Calculate reaction effect
            float reaction = 0;
            for (int i = 0; i < _ReactionPointCount; i++)
            {
                float3 reactionPos = _ReactionPoints[i].xyz;
                float dist = distance(IN.worldPos, reactionPos);
                
                if (dist < _ReactionRadius)
                {
                    float wave = sin(dist * 3.14159 - _Time.y * _ReactionSpeed);
                    reaction += saturate(wave) * (1 - dist / _ReactionRadius);
                }
            }
            reaction = saturate(reaction) * _ReactionIntensity;
            
            // Combine emission effects
            float3 emission = _EmissionColor.rgb * e.rgb * glow * _GlowIntensity;
            emission += _ReactionColor.rgb * reaction;
            
            // Apply height-based color variation
            float heightFactor = saturate(IN.height * 0.5 + 0.5);
            c.rgb *= lerp(0.8, 1.2, heightFactor);
            
            o.Albedo = c.rgb;
            o.Normal = n;
            o.Emission = emission;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Translucency = _Translucency;
            o.TranslucencyColor = _TranslucencyColor.rgb;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
} 