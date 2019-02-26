Shader "VolumeRendering/VolumeRendering"
{
	Properties
	{
		_Volume ("Volume", 3D) = "" {}
		_Intensity("Intensity", Range(0.5, 3.0)) = 1
		_AlphaCutoff("AlphaCutoff", Range(0.1, 1)) = 0.97
		_StepCount("StepCount", Range(1, 256)) = 128
		_SliceMin ("Slice min", Vector) = (0.0, 0.0, 0.0, -1.0)
		_SliceMax ("Slice max", Vector) = (1.0, 1.0, 1.0, -1.0)
	}

	CGINCLUDE

	ENDCG

	SubShader {
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		// ZTest Always

		Pass
		{
			CGPROGRAM

			#include "./VolumeRendering.cginc"
			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}
