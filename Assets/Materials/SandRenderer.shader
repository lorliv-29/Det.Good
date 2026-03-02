Shader "Custom/SandRenderer"
{
    Properties
    {
        _WaterColor ("Water Color (Low)", Color) = (0.1, 0.4, 0.8, 1)
        _SandColor ("Sand Color (Mid)", Color) = (0.9, 0.8, 0.6, 1)
        _SnowColor ("Snow Color (High)", Color) = (1.0, 1.0, 1.0, 1)
        _PointSize ("Point Size", Float) = 0.05
        _MinHeight ("Water Height Level", Float) = 0.0 
        _MaxHeight ("Snow Height Level", Float) = 0.2 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // PASS 1: THE COLOR PASS (What your eyes see)
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _WaterColor, _SandColor, _SnowColor;
            float _PointSize, _MinHeight, _MaxHeight;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float3> CalibratedPoints;
            #endif

            void setup() {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float3 pos = CalibratedPoints[unity_InstanceID];
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(pos, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = _PointSize;
            #endif
            }

            v2f vert (appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    o.worldPos = CalibratedPoints[unity_InstanceID];
                #else
                    o.worldPos = float3(0,0,0);
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float h = 01 - smoothstep(_MinHeight, _MaxHeight, i.worldPos.z);
                fixed4 finalColor = lerp(
                    lerp(_WaterColor, _SandColor, h * 2.0), 
                    lerp(_SandColor, _SnowColor, (h - 0.5) * 2.0), 
                    step(0.5, h)
                );
                return finalColor;
            }
            ENDCG
        }

        // PASS 2: THE DEPTH PASS (What the SSAO shadows see)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _PointSize;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float3> CalibratedPoints;
            #endif

            void setup() {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float3 pos = CalibratedPoints[unity_InstanceID];
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(pos, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = _PointSize;
            #endif
            }

            v2f vert (appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return 0; // We don't care about color here, just depth!
            }
            ENDCG
        }
    }
}