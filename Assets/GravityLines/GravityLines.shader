Shader "Custom/GravityLines" {
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Range(0, 1)) = 0
		_Metallic("Metallic", Range(0, 1)) = 0
	}

	SubShader
	{
		//Tags{ "RenderType" = "Opaque" }
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }

		CGPROGRAM

		#pragma surface surf Standard vertex:vert addshadow nolightmap alpha:fade
		//#pragma surface surf Standard vertex:vert addshadow nolightmap
		#pragma instancing_options procedural:setup
		#pragma target 3.5

		struct LineData
		{
			bool Active;
			float3 Albedo;
			float Radius;
			float Phi;
			float Time;
			float LifeTime;
		};


		struct Input
		{
			float vface : VFACE;
			fixed4 color : COLOR;
		};

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vid : SV_VertexID;
			fixed4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		half4 _Color;
		half _Smoothness;
		half _Metallic;

		//float3 _MeshScale;
		uint _InstanceCount;
		uint _MeshVertices;
		bool _ShowAllLines;

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		StructuredBuffer<LineData> _LineDataBuffer;
        StructuredBuffer<float4> _PositionBuffer;
        StructuredBuffer<float4> _TangentBuffer;
        StructuredBuffer<float4> _NormalBuffer;
		#endif

		void vert(inout appdata v)
		{
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

			uint idx = unity_InstanceID * _MeshVertices + v.vertex.x;

			float age = _LineDataBuffer[unity_InstanceID].Time / _LineDataBuffer[unity_InstanceID].LifeTime;
			//float a1 = (float)_MeshVertices;
			//float a2 = (float)v.vertex.x;
			float posNorm = (float)v.vertex.x / (float)_MeshVertices;
			//float a = abs(age - posNorm)*10;
			//float a = clamp(posNorm - max(0,age - 0.3), 0, 1)*10;
			float a = clamp(posNorm - age + 0.3, 0, 1)*100;

			if( posNorm > age - 0.1 || age - 0.2 > posNorm)
			{
				a = 0;
			}else
			{
				a = 1;
			}

			//v.color = fixed4(_LineDataBuffer[unity_InstanceID].Albedo, max(0, 1 - a));
			v.color = fixed4(_LineDataBuffer[unity_InstanceID].Albedo, a);

			// activeでないinstanceは表示しない
			//if (!_LineDataBuffer[unity_InstanceID].Active)
			//{
			//	v.color = fixed4(_LineDataBuffer[unity_InstanceID].Albedo, 0);
			//}

			//float x = _PositionBuffer[idx].x;
			//float y = _PositionBuffer[idx].y;
			//float z = _PositionBuffer[idx].z;

			//v.vertex = float4(x, y, z, 1);
			v.vertex = _PositionBuffer[idx];
			v.normal = _NormalBuffer[idx];

			if(_ShowAllLines)
			{
				v.color = fixed4(_LineDataBuffer[unity_InstanceID].Albedo, 1);	// 全て表示
			}

			#endif
		}

		void setup()
		{
			//unity_ObjectToWorld = _LocalToWorld;
			//unity_WorldToObject = _WorldToLocal;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = IN.color.rgb * _Color.rgb;
			o.Alpha = IN.color.a;// _Color.a;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Normal = float3(0, 0, IN.vface < 0 ? -1 : 1);
		}

		ENDCG
	}
	FallBack "Diffuse"
}
