Shader "Custom/AlwaysOnTop"
{
    Properties
    {
        _BaseColor("Color", Color) = (0,0,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            // Required for VR Stereo Instancing
            #pragma multi_compile_instancing 
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                // 1. Define Instance ID for the input
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                // 2. Define Stereo Output for the v2f struct
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;
                
                // 3. Setup the instance ID (tells Unity "which eye/instance is this?")
                UNITY_SETUP_INSTANCE_ID(input); 
                
                // 4. Initialize stereo output (passes the eye index to the rasterizer)
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // Optional: Necessary if you use per-instance properties in fragment
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

                return _BaseColor;
            }

            ENDHLSL
        }
    }
}



/*
NOT WORKING WITH VR 
Shader "Custom/AlwaysOnTop"
{
    Properties
    {
        _BaseColor("Color", Color) = (0,0,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                return _BaseColor;
            }

            ENDHLSL
        }
    }
}
*/