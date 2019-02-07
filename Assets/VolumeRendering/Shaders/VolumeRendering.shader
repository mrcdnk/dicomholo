Shader "VolumeRendering/VolumeRendering"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_FirstSegmentColor("FirstSegmentColor", Color) = (0, 1, 0, 0.5)
		_SecondSegmentColor("SecondSegmentColor", Color) = ( 0, 0, 1, 0.5 )
		_ThirdSegmentColor("ThirdSegmentColor", Color) = ( 1, 0, 0, 0.5 )
		_Segments("Segments", 3D) = "" {}
		_Volume ("Volume", 3D) = "" {}
		_Intensity ("Intensity", Range(1.0, 5.0)) = 1.2
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
