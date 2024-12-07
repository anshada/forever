Shader "Forever/VFX/MagicalAura"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        
        [Header(Flow)]
        _FlowSpeed ("Flow Speed", Range(0,10)) = 1.0
        _FlowDirection ("Flow Direction", Vector) = (0,1,0,0)
        _FlowIntensity ("Flow Intensity", Range(0,2)) = 1.0
        _FlowTiling ("Flow Tiling", Range(0,10)) = 1.0
        
        [Header(Energy)]
        _EnergyIntensity ("Energy Intensity", Range(0,5)) = 1.0
        _EnergySpeed ("Energy Speed", Range(0,10)) = 1.0
        _EnergyScale ("Energy Scale", Range(0,5)) = 1.0
        [HDR] _EnergyColor ("Energy Color", Color) = (1,1,1,1)
        
        [Header(Distortion)]
        _DistortionAmount ("Distortion Amount", Range(0,1)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(0,10)) = 1.0
        _DistortionScale ("Distortion Scale", Range(0,10)) = 1.0
        
        [Header(Interaction)]
        _InteractionRadius ("Interaction Radius", Range(0,5)) = 1.0
        _InteractionPower ("Interaction Power", Range(0,5)) = 1.0
        _InteractionSpeed ("Interaction Speed", Range(0,10)) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBinormal : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;
            
            float _FlowSpeed;
            float4 _FlowDirection;
            float _FlowIntensity;
            float _FlowTiling;
            
            float _EnergyIntensity;
            float _EnergySpeed;
            float _EnergyScale;
            fixed4 _EnergyColor;
            
            float _DistortionAmount;
            float _DistortionSpeed;
            float _DistortionScale;
            
            float _InteractionRadius;
            float _InteractionPower;
            float _InteractionSpeed;
            
            // Global interaction points from shader manager
            float4 _InteractionPoints[10];
            int _InteractionPointCount;
            
            // Noise functions
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
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                // Calculate tangent space
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
                
                // Store world position and tangent space
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;
                o.worldNormal = worldNormal;
                o.worldTangent = worldTangent;
                o.worldBinormal = worldBinormal;
                
                // Calculate UVs with flow
                float2 flowUV = worldPos.xz * _FlowTiling;
                float2 timeOffset = _Time.y * _FlowSpeed * _FlowDirection.xz;
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.zw = flowUV + timeOffset;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate flow effect
                float2 flowUV = i.uv.zw;
                float noise1 = snoise(flowUV) * 0.5 + 0.5;
                float noise2 = snoise(flowUV * 2.0 + float2(1.23, 3.45)) * 0.5 + 0.5;
                float flow = lerp(noise1, noise2, sin(_Time.y * _FlowSpeed) * 0.5 + 0.5);
                
                // Calculate energy effect
                float time = _Time.y * _EnergySpeed;
                float2 energyUV = i.worldPos.xz * _EnergyScale + flow * _FlowIntensity;
                float energy = snoise(energyUV + time);
                energy = saturate(energy * _EnergyIntensity);
                
                // Calculate distortion
                float2 distortUV = i.worldPos.xz * _DistortionScale;
                float2 distortion = float2(
                    snoise(distortUV + time * _DistortionSpeed),
                    snoise(distortUV + time * _DistortionSpeed + float2(1.23, 3.45))
                );
                
                // Apply distortion to main UV
                float2 mainUV = i.uv.xy + distortion * _DistortionAmount;
                
                // Sample textures
                fixed4 col = tex2D(_MainTex, mainUV) * _Color;
                fixed4 noiseCol = tex2D(_NoiseTexture, flowUV);
                
                // Calculate interaction effect
                float interaction = 0;
                for (int j = 0; j < _InteractionPointCount; j++)
                {
                    float3 interactPos = _InteractionPoints[j].xyz;
                    float dist = distance(i.worldPos, interactPos);
                    
                    if (dist < _InteractionRadius)
                    {
                        float wave = sin(dist * 3.14159 - time * _InteractionSpeed);
                        interaction += saturate(wave) * pow(1 - dist / _InteractionRadius, _InteractionPower);
                    }
                }
                
                // Combine effects
                fixed4 finalColor = col;
                finalColor.rgb += _EnergyColor.rgb * energy;
                finalColor.rgb += _Color.rgb * interaction;
                finalColor.a *= noiseCol.r;
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
} 