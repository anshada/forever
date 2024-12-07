Shader "Forever/VFX/Firefly"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _SoftParticlesFactor ("Soft Particles Factor", Range(0,3)) = 1.0
        
        [Header(Glow)]
        _GlowRadius ("Glow Radius", Range(0,2)) = 1.0
        _GlowFalloff ("Glow Falloff", Range(1,5)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.2
        
        [Header(Trail)]
        _TrailLength ("Trail Length", Range(0,1)) = 0.5
        _TrailWidth ("Trail Width", Range(0,1)) = 0.1
        [HDR] _TrailColor ("Trail Color", Color) = (1,1,1,1)
        _TrailFade ("Trail Fade", Range(0,5)) = 1.0
    }
    
    Category
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend One One
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off
        
        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0
                #pragma multi_compile_particles
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
                
                sampler2D _MainTex;
                fixed4 _Color;
                float _SoftParticlesFactor;
                
                float _GlowRadius;
                float _GlowFalloff;
                float _PulseSpeed;
                float _PulseAmount;
                
                float _TrailLength;
                float _TrailWidth;
                fixed4 _TrailColor;
                float _TrailFade;
                
                UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
                
                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 velocity : TEXCOORD1;
                    float age : TEXCOORD2;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
                
                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 projPos : TEXCOORD1;
                    float3 worldPos : TEXCOORD2;
                    float age : TEXCOORD3;
                    float4 velocity : TEXCOORD4;
                    UNITY_FOG_COORDS(5)
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                float4 _MainTex_ST;
                
                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    
                    // Calculate trail deformation
                    float trailFactor = v.texcoord.y * _TrailLength;
                    float3 trailOffset = v.velocity.xyz * trailFactor;
                    float3 vertexPos = v.vertex.xyz - trailOffset;
                    
                    // Apply width variation along trail
                    float widthFactor = 1.0 - (trailFactor * _TrailFade);
                    vertexPos.xy *= lerp(1.0, _TrailWidth, trailFactor) * widthFactor;
                    
                    o.vertex = UnityObjectToClipPos(float4(vertexPos, 1.0));
                    o.projPos = ComputeScreenPos(o.vertex);
                    COMPUTE_EYEDEPTH(o.projPos.z);
                    
                    o.color = v.color * _Color;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.age = v.age;
                    o.velocity = v.velocity;
                    
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }
                
                fixed4 frag (v2f i) : SV_Target
                {
                    // Soft particles
                    float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                    float partZ = i.projPos.z;
                    float fade = saturate(_SoftParticlesFactor * (sceneZ - partZ));
                    
                    // Sample main texture
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    
                    // Calculate glow effect
                    float time = _Time.y * _PulseSpeed;
                    float pulse = 1.0 + sin(time) * _PulseAmount;
                    float glowFactor = pow(col.a, _GlowFalloff) * _GlowRadius * pulse;
                    
                    // Apply trail color
                    fixed4 trailCol = _TrailColor;
                    trailCol.a *= 1.0 - i.texcoord.y; // Fade along trail
                    
                    // Combine colors
                    fixed4 finalColor = lerp(trailCol, i.color, i.texcoord.y) * col;
                    finalColor.rgb *= glowFactor;
                    finalColor.a = finalColor.a * fade;
                    
                    // Apply fog
                    UNITY_APPLY_FOG_COLOR(i.fogCoord, finalColor, fixed4(0,0,0,0));
                    
                    return finalColor;
                }
                ENDCG
            }
        }
    }
} 