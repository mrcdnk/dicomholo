Shader "VolumeRendering/VolumeRendering"
{
	Properties
	{
		_Volume ("Volume", 3D) = "" {}
		_Intensity("Intensity", Range(0.5, 2.0)) = 1
		_AlphaCutoff("AlphaCutoff", Range(0.1, 1)) = 0.97
		_StepCount("StepCount", Range(1, 256)) = 128
		_SliceMin ("Slice min", Vector) = (0.0, 0.0, 0.0, -1.0)
		_SliceMax ("Slice max", Vector) = (1.0, 1.0, 1.0, -1.0)
	}

	CGINCLUDE

	ENDCG

	SubShader {
		Cull Back
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		// ZTest Always

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#include "./VolumeRendering.cginc"
			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}
