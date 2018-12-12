// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Ray Marching/Ray Marching" 
{
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 3.0
	#pragma profileoption MaxLocalParams=1024 
	#pragma profileoption NumInstructionSlots=4096
	#pragma profileoption NumMathInstructionSlots=4096
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
	};
	
	sampler3D _VolumeTex;
	float4 _VolumeTex_TexelSize;

	sampler2D _FrontTex;
	sampler2D _BackTex;
	
	float4 _LightDir;
	float4 _LightPos;
	
	float _Dimensions;
	
	float _Opacity;
	int _Steps;
	float _StepSize;
	float4 _ClipDims;	
	float4 _ClipPlane;
	
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		
		o.uv[0] = v.texcoord.xy;
		o.uv[1] = v.texcoord.xy;
		#if SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			o.uv[0].y = 1-o.uv[0].y;
		#endif			
		return o;
	}
	
	half4 raymarch(v2f i, float offset) 
	{
		float3 frontPos = tex2D(_FrontTex, i.uv[1]).xyz;		
		float3 backPos = tex2D(_BackTex, i.uv[1]).xyz;				
		float3 dir = backPos - frontPos;
		float3 pos = frontPos;
		float4 dst = 0;
		float3 stepDist = dir * 1/_Steps;
		
			
		for(int k = 0; k < _Steps; k++)
		{
			float4 src = tex3D(_VolumeTex, pos);
			
			// clipping
			float border = step(1 - _ClipDims.x, pos.x);
			border *= step(pos.y, _ClipDims.y);
			border *= step(pos.z, _ClipDims.z);
			border *= step(0, dot(_ClipPlane, float4(pos - 0.5, 1)) + _ClipPlane.w);

	        // Standard blending	        
	        src.a *= saturate(_Opacity * border);
	        src.rgb *= src.a;
	        dst = (1.0f - dst.a) * src + dst;

			pos += stepDist;
		}

    	return dst + dst;
	}

	ENDCG
	
Subshader {
	ZTest Always Cull Off ZWrite Off
	Fog { Mode off }
		
	Pass 
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag(v2f i) : COLOR { return raymarch(i, 0); }	
		ENDCG
	}					
}

Fallback off
	
} // shader