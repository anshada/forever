Shader "Forever/Environment/MagicalWater"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _CausticsTex ("Caustics Texture", 2D) = "black" {}
        
        [Header(Surface)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.95
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
        _WaveSpeed ("Wave Speed", Range(0,2)) = 1.0
        _WaveScale ("Wave Scale", Vector) = (1,1,1,1)
        
        [Header(Depth)]
        _DepthGradientShallow ("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
        _DepthGradientDeep ("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        _DepthMaxDistance ("Depth Maximum Distance", Float) = 1.0
        _DepthStrength ("Depth Strength", Range(0,1)) = 0.5
        
        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (1,1,1,1)
        _FoamDistance ("Foam Distance", Float) = 0.4
        _FoamNoiseScale ("Foam Noise Scale", Float) = 100
        _FoamSpeed ("Foam Speed", Float) = 1
        
        [Header(Magic)]
        [HDR] _MagicColor ("Magic Color", Color) = (1,1,1,1)
        _MagicIntensity ("Magic Intensity", Range(0,1)) = 0.5
        _MagicFlowSpeed ("Magic Flow Speed", Range(0,2)) = 1.0
        _MagicScale ("Magic Scale", Range(0,10)) = 1.0
        
        [Header(Caustics)]
        _CausticsScale ("Caustics Scale", Range(0,10)) = 1.0
        _CausticsSpeed ("Caustics Speed", Range(0,2)) = 1.0
        [HDR] _CausticsColor ("Caustics Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        GrabPass { "_WaterBackground" }
        
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 4.0
        
        #include "UnityCG.cginc"
        
        struct Input
        {
            float2 uv_MainTex;
            float4 screenPos;
            float3 worldPos;
            float4 grabPos;
            float eyeDepth;
            INTERNAL_DATA
        };
        
        sampler2D _MainTex;
        sampler2D _NormalMap1;
        sampler2D _NormalMap2;
        sampler2D _CausticsTex;
        sampler2D _WaterBackground;
        sampler2D _CameraDepthTexture;
        
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        float _NormalStrength;
        float4 _WaveScale;
        float _WaveSpeed;
        
        fixed4 _DepthGradientShallow;
        fixed4 _DepthGradientDeep;
        float _DepthMaxDistance;
        float _DepthStrength;
        
        fixed4 _FoamColor;
        float _FoamDistance;
        float _FoamNoiseScale;
        float _FoamSpeed;
        
        fixed4 _MagicColor;
        float _MagicIntensity;
        float _MagicFlowSpeed;
        float _MagicScale;
        
        float _CausticsScale;
        float _CausticsSpeed;
        fixed4 _CausticsColor;
        
        // Noise function
        float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
        
        float snoise(float2 v)
        {
            const float4 C = float4(0.211324865405187, 0.366025403784439,
                                 -0.577350269189626, 0.024390243902439);
            float2 i  = floor(v + dot(v, C.yy));
            float2 x0 = v -   i + dot(i, C.xx);
            float2 i1;
            i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
            float4 x12 = x0.xyxy + C.xxzz;
            x12.xy -= i1;
            i = mod289(i);
            float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                + i.x + float3(0.0, i1.x, 1.0));
            float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy),
                                      dot(x12.zw,x12.zw)), 0.0);
            m = m*m;
            m = m*m;
            float3 x = 2.0 * frac(p * C.www) - 1.0;
            float3 h = abs(x) - 0.5;
            float3 ox = floor(x + 0.5);
            float3 a0 = x - ox;
            m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
            float3 g;
            g.x  = a0.x  * x0.x  + h.x  * x0.y;
            g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            return 130.0 * dot(m, g);
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Calculate vertex displacement
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float2 uv1 = worldPos.xz * _WaveScale.xy;
            float2 uv2 = worldPos.xz * _WaveScale.zw;
            float time = _Time.y * _WaveSpeed;
            
            float noise1 = snoise(uv1 + time);
            float noise2 = snoise(uv2 - time * 0.5);
            float combinedNoise = (noise1 + noise2) * 0.5;
            
            v.vertex.y += combinedNoise * 0.1;
            
            // Store data for fragment shader
            o.grabPos = ComputeGrabScreenPos(UnityObjectToClipPos(v.vertex));
            o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Sample textures with scrolling UVs
            float2 uv1 = IN.uv_MainTex + _Time.y * _WaveSpeed * float2(1, 0);
            float2 uv2 = IN.uv_MainTex - _Time.y * _WaveSpeed * float2(0, 1);
            
            fixed3 normal1 = UnpackNormal(tex2D(_NormalMap1, uv1));
            fixed3 normal2 = UnpackNormal(tex2D(_NormalMap2, uv2));
            
            // Blend normals
            fixed3 normalBlend = normalize(normal1 + normal2);
            normalBlend = lerp(float3(0,0,1), normalBlend, _NormalStrength);
            
            // Calculate water depth
            float4 screenPos = IN.screenPos;
            float2 screenUV = screenPos.xy / screenPos.w;
            
            float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
            float surfaceDepth = IN.eyeDepth;
            float depthDifference = backgroundDepth - surfaceDepth;
            
            // Calculate water color based on depth
            float waterDepthDifference = saturate(depthDifference / _DepthMaxDistance);
            fixed4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference);
            
            // Sample background with distortion
            float2 distortion = normalBlend.xy * 0.1;
            float4 backgroundColor = tex2D(_WaterBackground, screenUV + distortion);
            
            // Calculate foam
            float foamDepth = saturate(depthDifference / _FoamDistance);
            float foam = 1 - foamDepth;
            
            // Add foam noise
            float2 foamUV = IN.worldPos.xz * _FoamNoiseScale;
            float foamNoise = snoise(foamUV + _Time.y * _FoamSpeed);
            foam *= saturate(foamNoise * 0.5 + 0.5);
            
            // Calculate caustics
            float2 causticsUV = IN.worldPos.xz * _CausticsScale + _Time.y * _CausticsSpeed;
            fixed4 caustics = tex2D(_CausticsTex, causticsUV) * _CausticsColor;
            
            // Calculate magic effect
            float2 magicUV = IN.worldPos.xz * _MagicScale;
            float magicNoise = snoise(magicUV + _Time.y * _MagicFlowSpeed);
            float magic = saturate(magicNoise * 0.5 + 0.5) * _MagicIntensity;
            
            // Combine everything
            fixed4 c = waterColor * _Color;
            c = lerp(backgroundColor, c, c.a);
            c.rgb += _FoamColor.rgb * foam;
            c.rgb += caustics.rgb * waterDepthDifference;
            c.rgb += _MagicColor.rgb * magic;
            
            o.Albedo = c.rgb;
            o.Normal = normalBlend;
            o.Emission = caustics.rgb * waterDepthDifference + _MagicColor.rgb * magic;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = waterColor.a;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
} 