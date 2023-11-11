#ifdef RALIV_PENETRATOR
	#ifdef UNITY_PASS_FORWARDBASE
		PenetratorReshape(v.vertex, v.normal);
	#else
		v.vertex=float4(0,0,0,1);
	#endif
#endif
#ifdef RALIV_ORIFICE
	#ifdef UNITY_PASS_FORWARDBASE
		OrificeReshape(v.vertex, v.normal, v.tangent, v.vertexId);
	#else
		v.vertex=float4(0,0,0,1);
	#endif
#endif