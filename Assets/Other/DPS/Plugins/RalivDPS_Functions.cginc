void GetBestLights( float Channel, inout int orificeType, inout float3 orificePositionTracker, inout float3 orificeNormalTracker, inout float3 penetratorPositionTracker, inout float penetratorLength ) {
	float ID = step( 0.5 , Channel );
	float baseID = ( ID * 0.02 );
	float holeID = ( baseID + 0.01 );
	float ringID = ( baseID + 0.02 );
	float normalID = ( 0.05 + ( ID * 0.01 ) );
	float penetratorID = ( 0.09 + ( ID * -0.01 ) );
	float4 orificeWorld;
	float4 orificeNormalWorld;
	float4 penetratorWorld;
	float penetratorDist=100;
	for (int i=0;i<4;i++) {
		float range = (0.005 * sqrt(1000000 - unity_4LightAtten0[i])) / sqrt(unity_4LightAtten0[i]);
		if (length(unity_LightColor[i].rgb) < 0.01) {
			if (abs(fmod(range,0.1)-holeID)<0.005) {
				orificeType=0;
				orificeWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
				orificePositionTracker = mul( unity_WorldToObject, orificeWorld ).xyz;
			}
			if (abs(fmod(range,0.1)-ringID)<0.005) {
				orificeType=1;
				orificeWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
				orificePositionTracker = mul( unity_WorldToObject, orificeWorld ).xyz;
			}
			if (abs(fmod(range,0.1)-normalID)<0.005) {
				orificeNormalWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
				orificeNormalTracker = mul( unity_WorldToObject, orificeNormalWorld ).xyz;
			}
			if (abs(fmod(range,0.1)-penetratorID)<0.005) {
				float3 tempPenetratorPositionTracker = penetratorPositionTracker;
				penetratorWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
				penetratorPositionTracker = mul( unity_WorldToObject, penetratorWorld ).xyz;
				if (length(penetratorPositionTracker)>length(tempPenetratorPositionTracker)) {
					penetratorPositionTracker = tempPenetratorPositionTracker;
				} else {
					penetratorLength=unity_LightColor[i].a;
				}
			}
		}
	}
}

#ifdef RALIV_PENETRATOR

void PenetratorReshape(inout float4 vertex, inout float3 normal) {
	float orificeChannel=0;
	float orificeType = 0;
	float3 orificePositionTracker = float3(0,0,100);
	float3 orificeNormalTracker = float3(0,0,99);
	float3 penetratorPositionTracker = float3(0,0,1);
	float3 penetratorNormalTracker = float3(0,0,1);
	float pl=0;
	GetBestLights(orificeChannel, orificeType, orificePositionTracker, orificeNormalTracker, penetratorNormalTracker, pl);
	float3 orificeNormal = normalize( lerp( ( orificePositionTracker - orificeNormalTracker ) , orificePositionTracker , max( _EntranceStiffness , 0.01 )) );
	float behind = smoothstep(-_Length*0.5, _Length*0.2, orificePositionTracker.z);
	//orificePositionTracker.xy = behind * orificePositionTracker.xy;
	//orificeNormal.xy = behind * orificeNormal.xy;
	orificePositionTracker.z=(abs(orificePositionTracker.z+(_Length*0.2))-(_Length*0.2))*(1+step(orificePositionTracker.z,0)*2);
	orificePositionTracker.z=smoothstep(-_Length*0.2, _Length*0.2, orificePositionTracker.z) * orificePositionTracker.z;
	float distanceToOrifice = length( orificePositionTracker );
	float3 PhysicsNormal = normalize(penetratorNormalTracker.xyz);
	float enterFactor = smoothstep( _Length , _Length+0.05 , distanceToOrifice);
	float wriggleTimeY = _Time.y * _WriggleSpeed;
	float curvatureMod = ( _Length * ( ( cos( wriggleTimeY ) * _Wriggle ) + _Curvature ) );
	float wriggleTimeX = _Time.y * ( _WriggleSpeed * 0.79 );
	float3 finalOrificeNormal = normalize( lerp( orificeNormal , ( PhysicsNormal + ( ( float3(0,1,0) * ( curvatureMod + ( _Length * ( _ReCurvature + ( ( sin( wriggleTimeY ) * 0.3 ) * _Wriggle ) ) * 2.0 ) ) ) + ( float3(0.5,0,0) * ( cos( wriggleTimeX ) * _Wriggle ) ) ) ) , enterFactor) );
	float3 finalOrificePosition = lerp( orificePositionTracker , ( ( normalize(penetratorNormalTracker) * _Length ) + ( float3(0,0.2,0) * ( sin( ( wriggleTimeY + UNITY_PI ) ) * _Wriggle ) * _Length ) + ( float3(0.2,0,0) * _Length * ( sin( ( wriggleTimeX + UNITY_PI ) ) * _Wriggle ) ) ) , enterFactor);
	float finalOrificeDistance = length( finalOrificePosition );
	float3 bezierBasePosition = float3(0,0,0);
	float bezierDistanceThird = ( finalOrificeDistance / 3.0 );
	float3 curvatureOffset = lerp( float3( 0,0,0 ) , ( float3(0,1,0) * ( curvatureMod * -0.2 ) ) , saturate( ( distanceToOrifice / _Length ) ));
	float3 bezierBaseNormal = ( ( bezierDistanceThird * float3(0,0,1) ) + curvatureOffset );
	float3 bezierOrificeNormal = ( finalOrificePosition - ( bezierDistanceThird * finalOrificeNormal ) );
	float3 bezierOrificePosition = finalOrificePosition;
	float vertexBaseTipPosition = ( vertex.z / finalOrificeDistance );

	float3 sphereifyDistance = ( vertex.xyz - float3(0,0, distanceToOrifice) );
	float3 sphereifyNormal = normalize( sphereifyDistance );
	float sphereifyFactor = smoothstep( 0.01 , -0.01 , distanceToOrifice - vertex.z);
	sphereifyFactor *= 1-orificeType;
	vertex.xyz = lerp( vertex.xyz , ( float3(0,0, distanceToOrifice) + ( min( length( sphereifyDistance ) , _Squeeze ) * sphereifyNormal ) ) , sphereifyFactor);

	float squeezeFactor = smoothstep( 0.0 , _SqueezeDist , vertex.z - distanceToOrifice);
	squeezeFactor = max( squeezeFactor , smoothstep( 0.0 , _SqueezeDist , distanceToOrifice - vertex.z));
	squeezeFactor = 1- (1-squeezeFactor) * smoothstep(0,0.01,vertex.z) * behind * (1-enterFactor);
	vertex.xy = lerp( ( normalize(vertex.xy) * min( length( vertex.xy ) , _Squeeze ) ) , vertex.xy , squeezeFactor);
	
	float bulgeFactor = 1-smoothstep( 0.0 , _BulgeOffset , abs( ( finalOrificeDistance - vertex.z ) ));
	float bulgeFactorBaseClip = smoothstep( 0.0 , 0.05 , vertex.z);
	vertex.xy *= lerp( 1.0 , ( 1.0 + _BulgePower) , ( bulgeFactor * bulgeFactorBaseClip * behind * (1-enterFactor)));

	float t = saturate(vertexBaseTipPosition);
	float oneMinusT = 1 - t;
	float3 bezierPoint = oneMinusT * oneMinusT * oneMinusT * bezierBasePosition + 3 * oneMinusT * oneMinusT * t * bezierBaseNormal + 3 * oneMinusT * t * t * bezierOrificeNormal + t * t * t * bezierOrificePosition;
	float3 straightLine = (float3(0.0 , 0.0 , vertex.z));
	float baseFactor = smoothstep( 0.05 , -0.05 , vertex.z);
	bezierPoint = lerp( bezierPoint , straightLine , baseFactor);
	bezierPoint = lerp( ( ( finalOrificeNormal * ( vertex.z - finalOrificeDistance ) ) + finalOrificePosition ) , bezierPoint , step( vertexBaseTipPosition , 1.0 ));
	float3 bezierDerivitive = 3 * oneMinusT * oneMinusT * (bezierBaseNormal - bezierBasePosition) + 6 * oneMinusT * t * (bezierOrificeNormal - bezierBaseNormal) + 3 * t * t * (bezierOrificePosition - bezierOrificeNormal);
	bezierDerivitive = normalize( lerp( bezierDerivitive , float3(0,0,1) , baseFactor) );
	float bezierUpness = dot( bezierDerivitive , float3( 0,1,0 ) );
	float3 bezierUp = lerp( float3(0,1,0) , float3( 0,0,-1 ) , saturate( bezierUpness ));
	float bezierDownness = dot( bezierDerivitive , float3( 0,-1,0 ) );
	bezierUp = normalize( lerp( bezierUp , float3( 0,0,1 ) , saturate( bezierDownness )) );
	float3 bezierSpaceX = normalize( cross( bezierDerivitive , bezierUp ) );
	float3 bezierSpaceY = normalize( cross( bezierDerivitive , -bezierSpaceX ) );
	float3 bezierSpaceVertexOffset = ( ( vertex.y * bezierSpaceY ) + ( vertex.x * -bezierSpaceX ) );
	float3 bezierSpaceVertexOffsetNormal = normalize( bezierSpaceVertexOffset );
	float distanceFromTip = ( finalOrificeDistance - vertex.z );

	float3 bezierSpaceVertexOffsetFinal = lerp( bezierSpaceVertexOffset , bezierSpaceVertexOffset , enterFactor);
	float3 bezierConstructedVertex = ( bezierPoint + bezierSpaceVertexOffsetFinal );

	normal = normalize( ( ( -bezierSpaceX * normal.x ) + ( bezierSpaceY * normal.y ) + ( bezierDerivitive * normal.z ) ) );
	vertex.xyz = bezierConstructedVertex;
	
	vertex.w = 1;
}

#endif

#ifdef RALIV_ORIFICE

	float3 getBlendOffset(float blendSampleIndex, float activationDepth, float activationSmooth, int vertexID, float penetrationDepth, float3 normal, float3 tangent, float3 binormal) {
		float blendTextureSize = 1024;
		float2 blendSampleUV = (float2(( ( fmod( (float)vertexID , blendTextureSize ) + 0.5 ) / (blendTextureSize) ) , ( ( ( floor( ( vertexID / (blendTextureSize) ) ) + 0.5 ) / (blendTextureSize) ) + blendSampleIndex/8 )));
		float3 sampledBlend = tex2Dlod( _OrificeData, float4( blendSampleUV, 0, 0.0) ).rgb;
		float blendActivation = smoothstep( ( activationDepth ) , ( activationDepth + activationSmooth ) , penetrationDepth);
		blendActivation = -cos(blendActivation*3.1416)*0.5+0.5;
		float3 blendOffset = ( ( sampledBlend - float3(1,1,1)) * (blendActivation) * _BlendshapePower * _BlendshapeBadScaleFix );
		return ( ( blendOffset.x * normal ) + ( blendOffset.y * tangent ) + ( blendOffset.z * binormal ) );
	}

	void OrificeReshape(inout float4 vertex, inout float3 normal, float3 tangent, int vertexId) {
		float orificeChannel=0;
		float penetratorLength = 0.1;
		float penetratorDistance;
		float3 orificePositionTracker = float3(0,0,-100);
		float3 orificeNormalTracker = float3(0,0,-99);
		float3 penetratorPositionTracker = float3(0,0,100);
		float orificeType=0;
		
		GetBestLights(orificeChannel, orificeType, orificePositionTracker, orificeNormalTracker, penetratorPositionTracker, penetratorLength);
		penetratorDistance = distance(orificePositionTracker, penetratorPositionTracker );
		float penetrationDepth = max(0, penetratorLength - penetratorDistance);

		float3 binormal = normalize(cross( normal , tangent ));

		vertex.xyz += getBlendOffset(0, 0, _EntryOpenDuration, vertexId, penetrationDepth, normal, tangent, binormal);
		vertex.xyz += getBlendOffset(2, _Shape1Depth, _Shape1Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		vertex.xyz += getBlendOffset(4, _Shape2Depth, _Shape2Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		vertex.xyz += getBlendOffset(6, _Shape3Depth, _Shape3Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		vertex.w = 1;

		normal += getBlendOffset(1, 0, _EntryOpenDuration, vertexId, penetrationDepth, normal, tangent, binormal);
		normal += getBlendOffset(3, _Shape1Depth, _Shape1Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		normal += getBlendOffset(5, _Shape2Depth, _Shape2Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		normal += getBlendOffset(7, _Shape3Depth, _Shape3Duration, vertexId, penetrationDepth, normal, tangent, binormal);
		normal = normalize(normal);
	}

#endif