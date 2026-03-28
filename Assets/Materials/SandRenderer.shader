Shader "Custom/SandRenderer"
{
    Properties
    {
        _Color ("Sand Color", Color) = (0.9, 0.8, 0.6, 1)
        _PointSize ("Point Size", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            // Tells Unity to use our custom 'setup' function for procedural drawing
            #pragma instancing_options procedural:setup

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _Color;
            float _PointSize;

            // The buffer holding our calibrated sand coordinates
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float3> CalibratedPoints;
            #endif

            // This runs for every single point before it gets drawn
            void setup()
            {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                // Grab the 3D position
                float3 pos = CalibratedPoints[unity_InstanceID];

                // Position and scale the tiny sand grain
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(pos, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = _PointSize;
            #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color; // Draw the sand color
            }
            ENDCG
        }
    }
}