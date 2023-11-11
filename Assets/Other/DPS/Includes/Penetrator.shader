Shader "Raliv/Penetrator"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Color("Color", Color) = (0,0,0,0)
		_Metallic("Metallic", 2D) = "black" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Emission("Emission", 2D) = "black" {}
		_EmissionPower("EmissionPower", Range( 0 , 3)) = 1
		_Occlusion("Occlusion", 2D) = "white" {}
		[Header(Penetration Entry Deformation)]_Squeeze("Squeeze Minimum Size", Range( 0 , 0.2)) = 0
		_SqueezeDist("Squeeze Smoothness", Range( 0 , 0.1)) = 0
		_BulgePower("Bulge Amount", Range( 0 , 1)) = 0
		_BulgeOffset("Bulge Length", Range( 0 , 0.3)) = 0
		_Length("Length of Penetrator Model", Range( 0 , 3)) = 0
		[Header(Alignment Adjustment)]_EntranceStiffness("Entrance Stiffness", Range( 0.01 , 1)) = 0.01
		[Header(Resting Curvature)]_Curvature("Curvature", Range( -1 , 1)) = 0
		_ReCurvature("ReCurvature", Range( -1 , 1)) = 0
		[Header(Movement)]_Wriggle("Wriggle Amount", Range( 0 , 1)) = 0
		_WriggleSpeed("Wriggle Speed", Range( 0.1 , 30)) = 0.28
		[Header(Toon Shading (Check to activate))]_CellShadingSharpness("Cell Shading Sharpness", Range( 0 , 1)) = 0
		_ToonSpecularSize("ToonSpecularSize", Range( 0 , 1)) = 0
		_ToonSpecularIntensity("ToonSpecularIntensity", Range( 0 , 1)) = 0
		[Toggle(_TOONSHADING_ON)] _ToonShading("Toon Shading", Float) = 0
		[Header(Advanced)]_OrificeChannel("OrificeChannel Please Use 0", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry" }
		Cull Back
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile __ _TOONSHADING_ON
		#pragma surface surf StandardCustomLighting keepalpha noshadow vertex:vertexDataFunc 

		
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

		#define RALIV_PENETRATOR;

		#include "../Plugins/RalivDPS_Defines.cginc"
		#include "../Plugins/RalivDPS_Functions.cginc"

		
		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			PenetratorReshape(v.vertex, v.normal);
			/*float orificeType = 0;
			float3 orificePositionTracker = float3(0,0,100);
			float3 orificeNormalTracker = float3(0,0,99);
			float3 penetratorPositionTracker = float3(0,0,1);
			float pl=0;
			GetBestLights(_OrificeChannel, orificeType, orificePositionTracker, orificeNormalTracker, penetratorPositionTracker, pl);
			float3 orificeNormal = normalize( lerp( ( orificePositionTracker - orificeNormalTracker ) , orificePositionTracker , max( _EntranceStiffness , 0.01 )) );
			float3 PhysicsNormal = normalize(penetratorPositionTracker.xyz) * _Length * 0.3;
			float wriggleTime = _Time.y * _WriggleSpeed;
			float temp_output_257_0 = ( _Length * ( ( cos( wriggleTime ) * _Wriggle ) + _Curvature ) );
			float wiggleTime = _Time.y * ( _WriggleSpeed * 0.39 );
			float distanceToOrifice = length( orificePositionTracker );
			float enterFactor = smoothstep( ( _Length + -0.05 ) , _Length , distanceToOrifice);
			float3 finalOrificeNormal = normalize( lerp( orificeNormal , ( PhysicsNormal + ( ( float3(0,1,0) * ( temp_output_257_0 + ( _Length * ( _ReCurvature + ( ( sin( wriggleTime ) * 0.3 ) * _Wriggle ) ) * 2.0 ) ) ) + ( float3(0.5,0,0) * ( cos( wiggleTime ) * _Wriggle ) ) ) ) , enterFactor) );
			float smoothstepResult186 = smoothstep( _Length , ( _Length + 0.05 ) , distanceToOrifice);
			float3 finalOrificePosition = lerp( orificePositionTracker , ( ( normalize(penetratorPositionTracker) * _Length ) + ( float3(0,0.2,0) * ( sin( ( wriggleTime + UNITY_PI ) ) * _Wriggle ) * _Length ) + ( float3(0.2,0,0) * _Length * ( sin( ( wiggleTime + UNITY_PI ) ) * _Wriggle ) ) ) , smoothstepResult186);
			float finalOrificeDistance = length( finalOrificePosition );
			float3 bezierBasePosition = float3(0,0,0);
			float temp_output_59_0 = ( finalOrificeDistance / 3.0 );
			float3 lerpResult274 = lerp( float3( 0,0,0 ) , ( float3(0,1,0) * ( temp_output_257_0 * -0.2 ) ) , saturate( ( distanceToOrifice / _Length ) ));
			float3 temp_output_267_0 = ( ( temp_output_59_0 * float3(0,0,1) ) + lerpResult274 );
			float3 bezierBaseNormal = temp_output_267_0;
			float3 temp_output_63_0 = ( finalOrificePosition - ( temp_output_59_0 * finalOrificeNormal ) );
			float3 bezierOrificeNormal = temp_output_63_0;
			float3 bezierOrificePosition = finalOrificePosition;
			float vertexBaseTipPosition = ( v.vertex.z / finalOrificeDistance );
			float t = saturate(vertexBaseTipPosition);
			float oneMinusT = 1 - t;
			float3 bezierPoint = oneMinusT * oneMinusT * oneMinusT * bezierBasePosition + 3 * oneMinusT * oneMinusT * t * bezierBaseNormal + 3 * oneMinusT * t * t * bezierOrificeNormal + t * t * t * bezierOrificePosition;
			float3 straightLine = (float3(0.0 , 0.0 , v.vertex.z));
			float baseFactor = smoothstep( 0.05 , -0.05 , v.vertex.z);
			bezierPoint = lerp( bezierPoint , straightLine , baseFactor);
			bezierPoint = lerp( ( ( finalOrificeNormal * ( v.vertex.z - finalOrificeDistance ) ) + finalOrificePosition ) , bezierPoint , step( vertexBaseTipPosition , 1.0 ));
			float3 bezierDerivitive = 3 * oneMinusT * oneMinusT * (bezierBaseNormal - bezierBasePosition) + 6 * oneMinusT * t * (bezierOrificeNormal - bezierBaseNormal) + 3 * t * t * (bezierOrificePosition - bezierOrificeNormal);
			bezierDerivitive = normalize( lerp( bezierDerivitive , float3(0,0,1) , baseFactor) );
			float bezierUpness = dot( bezierDerivitive , float3( 0,1,0 ) );
			float3 bezierUp = lerp( float3(0,1,0) , float3( 0,0,-1 ) , saturate( bezierUpness ));
			float bezierDownness = dot( bezierDerivitive , float3( 0,-1,0 ) );
			bezierUp = normalize( lerp( bezierUp , float3( 0,0,1 ) , saturate( bezierDownness )) );
			float3 bezierSpaceX = normalize( cross( bezierDerivitive , bezierUp ) );
			float3 bezierSpaceY = normalize( cross( bezierDerivitive , -bezierSpaceX ) );
			float3 bezierSpaceVertexOffset = ( ( v.vertex.y * bezierSpaceY ) + ( v.vertex.x * -bezierSpaceX ) );
			float3 bezierSpaceVertexOffsetNormal = normalize( bezierSpaceVertexOffset );
			float distanceFromTip = ( finalOrificeDistance - v.vertex.z );
			float squeezeFactor = smoothstep( 0.0 , _SqueezeDist , -distanceFromTip);
			squeezeFactor = max( squeezeFactor , smoothstep( 0.0 , _SqueezeDist , distanceFromTip));
			float3 bezierSpaceVertexOffsetSqueezed = lerp( ( bezierSpaceVertexOffsetNormal * min( length( bezierSpaceVertexOffset ) , _squeeze ) ) , bezierSpaceVertexOffset , squeezeFactor);
			float bulgeFactor = smoothstep( 0.0 , _BulgeOffset , abs( ( finalOrificeDistance - v.vertex.z ) ));
			float bulgeFactorBaseClip = smoothstep( 0.0 , 0.05 , v.vertex.z);
			float bezierSpaceVertexOffsetBulged = lerp( 1.0 , ( 1.0 + _BulgePower ) , ( ( 1.0 - bulgeFactor ) * 100.0 * bulgeFactorBaseClip ));
			float3 bezierSpaceVertexOffsetFinal = lerp( ( bezierSpaceVertexOffsetSqueezed * bezierSpaceVertexOffsetBulged ) , bezierSpaceVertexOffset , enterFactor);
			float3 bezierConstructedVertex = ( bezierPoint + bezierSpaceVertexOffsetFinal );
			float3 sphereifyDistance = ( bezierConstructedVertex - finalOrificePosition );
			float3 sphereifyNormal = normalize( sphereifyDistance );
			float sphereifyFactor = smoothstep( 0.05 , -0.05 , distanceFromTip);
			float killSphereifyForRing = lerp( sphereifyFactor , 0.0 , orificeType);
			bezierConstructedVertex = lerp( bezierConstructedVertex , ( ( min( length( sphereifyDistance ) , _squeeze ) * sphereifyNormal ) + finalOrificePosition ) , killSphereifyForRing);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			bezierConstructedVertex = lerp( bezierConstructedVertex , ( -ase_worldViewDir * float3( 10000,10000,10000 ) ) , _WorldSpaceLightPos0.w);
			*/
			//v.normal = normalize( ( ( -bezierSpaceX * v.normal.x ) + ( bezierSpaceY * v.normal.y ) + ( bezierDerivitive * v.normal.z ) ) );
			//v.vertex.xyz = bezierConstructedVertex;
			//v.vertex.w = 1;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			SurfaceOutputStandard s393 = (SurfaceOutputStandard ) 0;
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
