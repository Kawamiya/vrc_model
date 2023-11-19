    Shader "VRLabs/Ragdoll System/Invisible"
    {
        SubShader
        {
            Tags {"Queue" = "Transparent" }
            Lighting Off
			ZWrite Off
            Pass
            {
                ColorMask 0    
            }
        }
    }

