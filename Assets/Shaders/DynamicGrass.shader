Shader "Forever/Environment/DynamicGrass"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0,2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0,10)) = 1.0
        _WindDirection ("Wind Direction", Vector) = (1,0,0,0)
        
        [Header(Grass)]
        _GrassHeight ("Grass Height", Range(0,2)) = 1.0
        _GrassWidth ("Grass Width", Range(0,1)) = 0.1
        _GrassRandomness ("Randomness", Range(0,1)) = 0.1
        _GrassBlades ("Blades per Vertex", Range(1,5)) = 3
        
        [Header(Interaction)]
        _InteractionRadius ("Interaction Radius", Range(0,5)) = 1.0
        _InteractionStrength ("Interaction Strength", Range(0,1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 4.0
        #pragma multi_compile_instancing
        
        #include "UnityCG.cginc"
        
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 vertexColor : COLOR;
        };
        
        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 tangent : TANGENT;
            float2 texcoord : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _NoiseTexture;
        
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        float _WindStrength;
        float _WindSpeed;
        float4 _WindDirection;
        
        float _GrassHeight;
        float _GrassWidth;
        float _GrassRandomness;
        int _GrassBlades;
        
        float _InteractionRadius;
        float _InteractionStrength;
        
        // Global interaction points (e.g., player position, footsteps)
        float4 _InteractionPoints[10];
        int _InteractionPointCount;
        
        // Noise functions
        float rand(float3 co)
        {
            return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
        }
        
        float noise(float2 p)
        {
            return tex2Dlod(_NoiseTexture, float4(p * 0.1, 0, 0)).r;
        }
        
        // Calculate wind effect
        float3 applyWind(float3 pos, float3 normal, float phase)
        {
            float3 wind = _WindDirection.xyz * _WindStrength;
            float time = _Time.y * _WindSpeed;
            
            // Large wind waves
            float2 uv = pos.xz * 0.1;
            float windNoise = noise(uv + time * 0.1);
            
            // Small wind detail
            float detailNoise = noise(uv * 3.0 + time * 0.2);
            
            // Combine noises
            float windEffect = windNoise * 0.8 + detailNoise * 0.2;
            windEffect = (windEffect * 2 - 1) * _WindStrength;
            
            // Apply wind based on height
            float heightFactor = pos.y * 0.5;
            pos.xz += wind.xz * windEffect * heightFactor;
            
            return pos;
        }
        
        // Calculate interaction effect
        float3 applyInteraction(float3 pos, float3 normal)
        {
            float3 displacement = float3(0, 0, 0);
            
            for (int i = 0; i < _InteractionPointCount; i++)
            {
                float3 interactionPos = _InteractionPoints[i].xyz;
                float dist = distance(pos.xz, interactionPos.xz);
                
                if (dist < _InteractionRadius)
                {
                    float strength = 1 - (dist / _InteractionRadius);
                    strength = smoothstep(0, 1, strength) * _InteractionStrength;
                    
                    float3 direction = normalize(pos - interactionPos);
                    displacement += direction * strength;
                }
            }
            
            return pos + displacement;
        }
        
        void vert(inout appdata v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            UNITY_SETUP_INSTANCE_ID(v);
            
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 normal = UnityObjectToWorldNormal(v.normal);
            
            // Random offset per vertex
            float random = rand(worldPos);
            float phase = random * 2 * UNITY_PI;
            
            // Apply height variation
            float heightVar = (random * 2 - 1) * _GrassRandomness;
            worldPos.y *= _GrassHeight * (1 + heightVar);
            
            // Apply wind
            worldPos = applyWind(worldPos, normal, phase);
            
            // Apply interaction
            worldPos = applyInteraction(worldPos, normal);
            
            // Transform back to object space
            v.vertex.xyz = mul(unity_WorldToObject, float4(worldPos, 1)).xyz;
            
            // Store world position for fragment shader
            o.worldPos = worldPos;
            o.vertexColor = v.color;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Sample textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            
            // Apply vertex color
            c *= IN.vertexColor;
            
            // Apply height-based color variation
            float heightFactor = saturate(IN.worldPos.y / _GrassHeight);
            c.rgb = lerp(c.rgb * 0.8, c.rgb, heightFactor);
            
            o.Albedo = c.rgb;
            o.Normal = n;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
} 