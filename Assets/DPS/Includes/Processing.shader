// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Raliv/Processing"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _MainTex;


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


		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 unityObjectToClipPos3 = UnityObjectToClipPos( ase_vertex3Pos );
			float4 computeScreenPos4 = ComputeScreenPos( unityObjectToClipPos3 );
			float3 rotatedValue12 = RotateAroundAxis( float3( 0,0,0 ), float3( ( (( computeScreenPos4 / (computeScreenPos4).w )).xy * float2( 10,10 ) ) ,  0.0 ), float3( 0,0,1 ), 45.0 );
			float4 tex2DNode1 = tex2D( _MainTex, rotatedValue12.xy );
			float3 ase_worldNormal = i.worldNormal;
			float3 ase_worldPos = i.worldPos;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float dotResult5_g1 = dot( ase_worldNormal , ase_worldlightDir );
			c.rgb = ( tex2DNode1 * (dotResult5_g1*0.5 + 0.5) ).rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 unityObjectToClipPos3 = UnityObjectToClipPos( ase_vertex3Pos );
			float4 computeScreenPos4 = ComputeScreenPos( unityObjectToClipPos3 );
			float3 rotatedValue12 = RotateAroundAxis( float3( 0,0,0 ), float3( ( (( computeScreenPos4 / (computeScreenPos4).w )).xy * float2( 10,10 ) ) ,  0.0 ), float3( 0,0,1 ), 45.0 );
			float4 tex2DNode1 = tex2D( _MainTex, rotatedValue12.xy );
			o.Emission = tex2DNode1.rgb;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = IN.worldNormal;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18707
33;27;1211;1004;2412.812;1140.545;1.660861;True;False
Node;AmplifyShaderEditor.PosVertexDataNode;5;-2560.001,-634.7346;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.UnityObjToClipPosHlpNode;3;-2289.969,-634.7347;Inherit;False;1;0;FLOAT3;0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComputeScreenPosHlpNode;4;-2044.049,-637.9496;Inherit;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ComponentMaskNode;6;-1714.545,-385.5974;Inherit;False;False;False;False;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;7;-1491.126,-461.142;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SwizzleNode;9;-1346.465,-474.0009;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-1164.836,-475.6081;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;10,10;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;12;-910.3022,-665.2658;Inherit;False;False;4;0;FLOAT3;0,0,1;False;1;FLOAT;45;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;1;-522.2464,-454.3602;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;10;-677.8023,83.01329;Inherit;False;Half Lambert Term;-1;;1;86299dc21373a954aa5772333626c9c1;0;1;3;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-307.4708,49.20313;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;Raliv/Processing;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;3;0;5;0
WireConnection;4;0;3;0
WireConnection;6;0;4;0
WireConnection;7;0;4;0
WireConnection;7;1;6;0
WireConnection;9;0;7;0
WireConnection;8;0;9;0
WireConnection;12;3;8;0
WireConnection;1;1;12;0
WireConnection;11;0;1;0
WireConnection;11;1;10;0
WireConnection;0;2;1;0
WireConnection;0;13;11;0
ASEEND*/
//CHKSM=4CB46F1DAA3F16148CE3C8BD88AD73ECA90C55B0