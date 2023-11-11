#ifdef RALIV_PENETRATOR
	uniform float _SqueezeDist;
	uniform float _Length;
	uniform float _Wriggle;
	uniform float _BulgePower;
	uniform float _BulgeOffset;
	uniform float _WriggleSpeed;
	uniform float _ReCurvature;
	uniform float _Curvature;
	uniform float _Squeeze;
	uniform float _EntranceStiffness;
#endif
#ifdef RALIV_ORIFICE
	uniform sampler2D _OrificeData;
	uniform float _EntryOpenDuration;
	uniform float _Shape1Depth;
	uniform float _Shape1Duration;
	uniform float _Shape2Depth;
	uniform float _Shape2Duration;
	uniform float _Shape3Depth;
	uniform float _Shape3Duration;
	uniform float _BlendshapePower;
	uniform float _BlendshapeBadScaleFix;
#endif