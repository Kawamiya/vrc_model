Shader "Raliv/Orifice"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,0)
		_Metallic("Metallic", 2D) = "black" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Emission("Emission", 2D) = "black" {}
		_EmissionPower("EmissionPower", Range( 0 , 3)) = 1
		_Occlusion("Occlusion", 2D) = "white" {}
		_OrificeData("OrificeData", 2D) = "white" {}
		_EntryOpenDuration("Entry Trigger Duration", Range( 0 , 1)) = 0.1
		_Shape1Depth("Shape 1 Trigger Depth", Range( 0 , 5)) = 0.1
		_Shape1Duration("Shape 1 Trigger Duration", Range( 0 , 1)) = 0.1
		_Shape2Depth("Shape 2 Trigger Depth", Range( 0 , 5)) = 0.2
		_Shape2Duration("Shape 2 Trigger Duration", Range( 0 , 1)) = 0.1
		_Shape3Depth("Shape 3 Trigger Depth", Range( 0 , 5)) = 0.3
		_Shape3Duration("Shape 3 Trigger Duration", Range( 0 , 1)) = 0.1
		_BlendshapePower("Blend Shape Power", Range(0,5)) = 1
		_BlendshapeBadScaleFix("Blend Shape Bad Scale Fix", Range(1,100)) = 1
		[Header(Advanced)]_OrificeChannel("OrificeChannel Please Use 0", Float) = 0
		[Header(Toon Shading (Check to activate))]_CellShadingSharpness("Cell Shading Sharpness", Range( 0 , 1)) = 0
		_ToonSpecularSize("ToonSpecularSize", Range( 0 , 1)) = 0
		_ToonSpecularIntensity("ToonSpecularIntensity", Range( 0 , 1)) = 0
		[Toggle(_TOONSHADING_ON)] _ToonShading("Toon Shading", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile __ _TOONSHADING_ON
		#pragma surface surf StandardCustomLighting keepalpha noshadow vertex:vertexDataFunc 

		struct appdata_full_custom
		{
			float4 vertex : POSITION;
			float4 tangent : TANGENT;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			float4 texcoord3 : TEXCOORD3;
			fixed4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			uint vertexId : SV_VertexID;
		};

		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
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
		uniform float4 _MainTex_ST;
		uniform float4 _Color;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform sampler2D _Emission;
		uniform float4 _Emission_ST;
		uniform float _EmissionPower;
		uniform sampler2D _Metallic;
		uniform float4 _Metallic_ST;
		uniform float _Smoothness;
		uniform sampler2D _Occlusion;
		uniform float4 _Occlusion_ST;
		uniform float _CellShadingSharpness;
		uniform float _ToonSpecularSize;
		uniform float _ToonSpecularIntensity;

		#define RALIV_ORIFICE;

		#include "../Plugins/RalivDPS_Defines.cginc"
		#include "../Plugins/RalivDPS_Functions.cginc"


		void vertexDataFunc( inout appdata_full_custom v, out Input o )	{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 normal = normalize( v.normal );
			float3 tangent = normalize( v.tangent.xyz );
			float3 binormal = normalize(cross( normal , tangent ));
			OrificeReshape(v.vertex, v.normal, v.tangent.xyz, v.vertexId);
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			SurfaceOutputStandard s393 = (SurfaceOutputStandard) 0;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode145 = tex2D( _MainTex, uv_MainTex );
			float4 temp_output_146_0 = ( tex2DNode145 * _Color );
			s393.Albedo = temp_output_146_0.rgb;
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			float3 tex2DNode147 = UnpackNormal( tex2D( _BumpMap, uv_BumpMap ) );
			s393.Normal = WorldNormalVector( i , tex2DNode147 );
			float2 uv_Emission = i.uv_texcoord * _Emission_ST.xy + _Emission_ST.zw;
			float4 tex2DNode283 = tex2D( _Emission, uv_Emission );
			s393.Emission = ( tex2DNode283 * _EmissionPower ).rgb;
			float2 uv_Metallic = i.uv_texcoord * _Metallic_ST.xy + _Metallic_ST.zw;
			float4 tex2DNode148 = tex2D( _Metallic, uv_Metallic );
			s393.Metallic = tex2DNode148.r;
			s393.Smoothness = ( tex2DNode148.a * _Smoothness );
			float2 uv_Occlusion = i.uv_texcoord * _Occlusion_ST.xy + _Occlusion_ST.zw;
			s393.Occlusion = tex2D( _Occlusion, uv_Occlusion ).r;

			data.light = gi.light;

			UnityGI gi393 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g393 = UnityGlossyEnvironmentSetup( s393.Smoothness, data.worldViewDir, s393.Normal, float3(0,0,0));
			gi393 = UnityGlobalIllumination( data, s393.Occlusion, s393.Normal, g393 );
			#endif

			float3 surfResult393 = LightingStandard ( s393, viewDir, gi393 ).rgb;
			surfResult393 += s393.Emission;

			#ifdef UNITY_PASS_FORWARDADD//393
			surfResult393 -= s393.Emission;
			#endif//393
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			float3 newWorldNormal396 = (WorldNormalVector( i , tex2DNode147 ));
			float3 ase_worldPos = i.worldPos;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float dotResult5_g1 = dot( newWorldNormal396 , ase_worldlightDir );
			float temp_output_402_0 = ( _CellShadingSharpness * 10.0 );
			UnityGI gi411 = gi;
			float3 diffNorm411 = WorldNormalVector( i , tex2DNode147 );
			gi411 = UnityGI_Base( data, 1, diffNorm411 );
			float3 indirectDiffuse411 = gi411.indirect.diffuse + diffNorm411 * 0.0001;
			float temp_output_470_0 = ( 1.0 - _ToonSpecularSize );
			float temp_output_457_0 = ( temp_output_470_0 * temp_output_470_0 );
			float3 normalizeResult446 = normalize( reflect( -ase_worldlightDir , newWorldNormal396 ) );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float dotResult418 = dot( normalizeResult446 , ase_worldViewDir );
			float saferPower437 = max( dotResult418 , 0.0001 );
			float temp_output_437_0 = pow( saferPower437 , 20.0 );
			float smoothstepResult449 = smoothstep( temp_output_457_0 , ( temp_output_457_0 + ( ( 1.1 - temp_output_457_0 ) * 0.5 ) ) , temp_output_437_0);
			#ifdef _TOONSHADING_ON
				float4 staticSwitch436 = ( ( ase_lightColor * max( saturate( (-temp_output_402_0 + ((dotResult5_g1*0.5 + 0.5) - 0.0) * (( temp_output_402_0 + 1.0 ) - -temp_output_402_0) / (1.0 - 0.0)) ) , 0.1 ) * temp_output_146_0 ) + ( float4( indirectDiffuse411 , 0.0 ) * temp_output_146_0 ) + ( ase_lightColor * saturate( smoothstepResult449 ) * _ToonSpecularIntensity ) );
			#else
				float4 staticSwitch436 = float4( surfResult393 , 0.0 );
			#endif
			c.rgb = staticSwitch436.rgb;
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
			o.Normal = float3(0,0,1);
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode145 = tex2D( _MainTex, uv_MainTex );
			float4 temp_output_146_0 = ( tex2DNode145 * _Color );
			o.Albedo = temp_output_146_0.rgb;
		}

		ENDCG
	}
}
