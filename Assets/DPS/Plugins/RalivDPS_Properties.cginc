// PENETRATOR
_Squeeze("Squeeze Minimum Size", Range( 0 , 0.2)) = 0
_SqueezeDist("Squeeze Smoothness", Range( 0 , 0.1)) = 0
_BulgePower("Bulge Amount", Range( 0 , 0.01)) = 0
_BulgeOffset("Bulge Length", Range( 0 , 0.3)) = 0
_Length("Length of Penetrator Model", Range( 0 , 3)) = 0
_EntranceStiffness("Entrance Stiffness", Range( 0.01 , 1)) = 0.01
_Curvature("Curvature", Range( -1 , 1)) = 0
_ReCurvature("ReCurvature", Range( -1 , 1)) = 0
_Wriggle("Wriggle Amount", Range( 0 , 1)) = 0
_WriggleSpeed("Wriggle Speed", Range( 0.1 , 30)) = 0.28
// ORIFICE
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