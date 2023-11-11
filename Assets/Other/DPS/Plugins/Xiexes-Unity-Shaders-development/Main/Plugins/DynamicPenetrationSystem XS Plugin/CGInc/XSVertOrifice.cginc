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
	float penetratorLength = 0.1;
	float penetratorDistance;
	float3 orificePositionTracker = float3(0,0,-100);
	float3 orificeNormalTracker = float3(0,0,-99);
	float3 penetratorPositionTracker = float3(0,0,100);
	float3 penetratorNormalTracker = float3(0,0,100);
	float orificeType=0;
	
	GetBestLights(orificeType, orificePositionTracker, orificeNormalTracker, penetratorPositionTracker, penetratorNormalTracker, penetratorLength);
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

VertexOutput vert (VertexInput v)
{
	float3 normal = normalize( v.normal );
	float3 tangent = normalize( v.tangent.xyz );
	float3 binormal = normalize(cross( normal , tangent ));

	OrificeReshape(v.vertex, v.normal, v.tangent.xyz, v.vertexId);
	
    VertexOutput o = (VertexOutput)0;
    float3 wnormal = UnityObjectToWorldNormal(v.normal);
    tangent = UnityObjectToWorldDir(v.tangent.xyz);
    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
    float3 bitangent = cross(wnormal, tangent) * tangentSign;

    #if defined(Geometry)
        o.vertex = v.vertex;
    #endif

    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.ntb[0] = wnormal;
    o.ntb[1] = tangent;
    o.ntb[2] = bitangent;
    o.uv = v.uv;
    o.uv1 = v.uv1;
    o.color = float4(v.color.rgb, 0); // store if outline in alpha channel of vertex colors | 0 = not an outline
    o.normal = v.normal;
    o.screenPos = ComputeScreenPos(o.pos);
    o.objPos = normalize(v.vertex);

    #if !defined(UNITY_PASS_SHADOWCASTER)
        UNITY_TRANSFER_SHADOW(o, o.uv);
        UNITY_TRANSFER_FOG(o, o.pos);
    #else
        TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
    #endif
    return o;
}