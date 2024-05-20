// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Davis3D/AlienPlants_Vol3/DustMote"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Cutoff( "Mask Clip Value", Float ) = 0
		_DustTex("DustTex", 2D) = "white" {}
		_Glow("Glow", Float) = 0.01
		_Wave_Push_Intensity("Wave_Push_Intensity", Float) = 0
		_Wave_Push_Speed("Wave_Push_Speed", Float) = 0.1
		_Opacity("Opacity", Float) = 0
		_DepthFadeLength("DepthFadeLength", Float) = 100
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float eyeDepth;
		};

		uniform float _Wave_Push_Speed;
		uniform float _Wave_Push_Intensity;
		uniform float4 _Color;
		uniform sampler2D _DustTex;
		uniform float4 _DustTex_ST;
		uniform float _Glow;
		uniform float _DepthFadeLength;
		uniform float _Opacity;
		uniform float _Cutoff = 0;


		float3 WorldToAbsoluteWorld3_g2( float3 In )
		{
			#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
			    In += _WorldSpaceCameraPos.xyz;
			#endif
			return In;
		}


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float4 _0101 = float4(0,1,0,1);
			float3 appendResult13_g1 = (float3(_0101.x , _0101.y , _0101.z));
			float3 normalizeResult14_g1 = normalize( appendResult13_g1 );
			float3 temp_cast_0 = (3.0).xxx;
			float temp_output_19_0_g1 = ( _0101.w * ( ( _Time.y * _Wave_Push_Speed ) * -0.5 ) );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 In3_g2 = ase_worldPos;
			float3 localWorldToAbsoluteWorld3_g2 = WorldToAbsoluteWorld3_g2( In3_g2 );
			float3 temp_output_62_0_g1 = localWorldToAbsoluteWorld3_g2;
			float3 temp_output_45_0_g1 = abs( ( ( frac( ( ( ( normalizeResult14_g1 * temp_output_19_0_g1 ) + ( temp_output_62_0_g1 / 10.24 ) ) + 0.5 ) ) * 2.0 ) + -1.0 ) );
			float dotResult58_g1 = dot( normalizeResult14_g1 , ( ( ( temp_cast_0 - ( temp_output_45_0_g1 * 2.0 ) ) * temp_output_45_0_g1 ) * temp_output_45_0_g1 ) );
			float3 temp_cast_1 = (3.0).xxx;
			float3 temp_output_46_0_g1 = abs( ( ( frac( ( ( temp_output_19_0_g1 + ( temp_output_62_0_g1 / 2.0 ) ) + 0.5 ) ) * 2.0 ) + -1.0 ) );
			float3 temp_cast_2 = (0.0).xxx;
			float3 temp_output_8_0_g1 = float3( 0,0,0 );
			float3 rotatedValue6_g1 = RotateAroundAxis( ( float3(0,0,-10) + temp_output_8_0_g1 ), temp_output_8_0_g1, cross( normalizeResult14_g1 , float3(0,0,1) ), ( dotResult58_g1 + distance( ( ( ( temp_cast_1 - ( temp_output_46_0_g1 * 2.0 ) ) * temp_output_46_0_g1 ) * temp_output_46_0_g1 ) , temp_cast_2 ) ) );
			v.vertex.xyz += ( ( ( rotatedValue6_g1 * 1.0 ) * _Wave_Push_Intensity ) + temp_output_8_0_g1 );
			v.vertex.w = 1;
			o.eyeDepth = -UnityObjectToViewPos( v.vertex.xyz ).z;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_DustTex = i.uv_texcoord * _DustTex_ST.xy + _DustTex_ST.zw;
			float4 tex2DNode4 = tex2D( _DustTex, uv_DustTex );
			o.Albedo = ( _Color * tex2DNode4.g ).rgb;
			o.Emission = ( _Color * _Glow ).rgb;
			float cameraDepthFade6 = (( i.eyeDepth -_ProjectionParams.y - 0.0 ) / _DepthFadeLength);
			float temp_output_10_0 = ( ( ( tex2DNode4.g * _Color.a ) * cameraDepthFade6 ) * _Opacity );
			o.Alpha = temp_output_10_0;
			clip( temp_output_10_0 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
0;73;1031;661;1663.419;-6.015045;1;True;False
Node;AmplifyShaderEditor.ColorNode;2;-987.4002,-174.6738;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,0,0.3897533,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-1084.534,-2.86432;Inherit;True;Property;_DustTex;DustTex;2;0;Create;True;0;0;0;False;0;False;-1;None;b2aea96aae9bb844f903a2081fb6f2dc;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;8;-996.0248,220.3133;Inherit;False;Property;_DepthFadeLength;DepthFadeLength;7;0;Create;True;0;0;0;False;0;False;100;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-692.7045,94.9249;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;6;-799.3539,201.8907;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-573.7216,266.3416;Inherit;False;Property;_Opacity;Opacity;6;0;Create;True;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-933.5735,-245.3052;Inherit;False;Property;_Glow;Glow;3;0;Create;True;0;0;0;False;0;False;0.01;15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-547.5967,156.2164;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-707.0798,430.6295;Inherit;False;Constant;_Float1;Float 1;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-788.563,346.679;Inherit;False;Property;_Wave_Push_Intensity;Wave_Push_Intensity;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-789.919,509.188;Inherit;False;Property;_Wave_Push_Speed;Wave_Push_Speed;5;0;Create;True;0;0;0;False;0;False;0.1;0.125;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-725.5809,-68.12855;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-418.2083,157.1837;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-725.4478,-224.5462;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;20;-568.3154,413.4206;Inherit;False;SimpleGrassWind;-1;;1;2f5588d2087aaab40a4d9a662ffe0ccd;0;4;5;FLOAT;0;False;7;FLOAT;0;False;23;FLOAT;1;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-239.2989,-60.64421;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Davis3D/AlienPlants_Vol3/DustMote;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;1;False;-1;3;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0;True;False;0;True;Transparent;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;7;0;4;2
WireConnection;7;1;2;4
WireConnection;6;0;8;0
WireConnection;9;0;7;0
WireConnection;9;1;6;0
WireConnection;3;0;2;0
WireConnection;3;1;4;2
WireConnection;10;0;9;0
WireConnection;10;1;11;0
WireConnection;13;0;2;0
WireConnection;13;1;12;0
WireConnection;20;5;16;0
WireConnection;20;7;17;0
WireConnection;20;23;18;0
WireConnection;0;0;3;0
WireConnection;0;2;13;0
WireConnection;0;9;10;0
WireConnection;0;10;10;0
WireConnection;0;11;20;0
ASEEND*/
//CHKSM=0B93AA5A0C17F812ADB296403E09D746340FDA1A