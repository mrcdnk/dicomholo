#ifndef __VOLUME_RENDERING_INCLUDED__
#define __VOLUME_RENDERING_INCLUDED__

#include "UnityCG.cginc"

sampler3D _Volume;
half _Intensity;
half _AlphaCutoff;
half _Opacity;
half _StepCount;
half3 _SliceMin, _SliceMax;
float4x4 _AxisRotationMatrix;

static const fixed3 AABB_MIN = fixed3(-0.5, -0.5, -0.5);
static const fixed3 AABB_MAX = fixed3(0.5, 0.5, 0.5);

static const float EPSILON = 0.01;
static const float LOWER_EPSILON = -EPSILON;
static const float UPPER_EPSILON = 1 + EPSILON;

struct Ray {
  fixed3 origin;
  fixed3 dir;
};

bool intersect(Ray r, out float t0, out float t1)
{
  float3 invR = 1.0 / r.dir;
  float3 tbot = invR * (AABB_MIN - r.origin);
  float3 ttop = invR * (AABB_MAX - r.origin);
  float3 tmin = min(ttop, tbot);
  float3 tmax = max(ttop, tbot);
  float2 t = max(tmin.xx, tmin.yz);
  t0 = max(t.x, t.y);
  t = min(tmax.xx, tmax.yz);
  t1 = min(t.x, t.y);
  return t0 <= t1;
}

float3 localize(float3 p) {
  return mul(unity_WorldToObject, float4(p, 1)).xyz;
}

float3 get_uv(float3 p) {
  // float3 local = localize(p);
  return (p + 0.5);
}

float4 sample_volume(sampler3D vol, float3 uv, float3 p)
{
  float4 v = tex3Dlod(vol, float4(uv, 0)) * _Intensity;

  float3 axis = mul(_AxisRotationMatrix, float4(p, 0)).xyz;
  axis = get_uv(axis);
  float min = step(_SliceMin.x, axis.x) * step(_SliceMin.y, axis.y) * step(_SliceMin.z, axis.z);
  float max = step(axis.x, _SliceMax.x) * step(axis.y, _SliceMax.y) * step(axis.z, _SliceMax.z);

  return v * min * max;
}

bool outside(float3 uv)
{
  return (
			uv.x < LOWER_EPSILON || uv.y < LOWER_EPSILON || uv.z < LOWER_EPSILON ||
			uv.x > UPPER_EPSILON || uv.y > UPPER_EPSILON || uv.z > UPPER_EPSILON
		);
}

struct appdata
{
  float4 vertex : POSITION;
  float2 uv : TEXCOORD0;
};

struct v2f
{
  float4 vertex : SV_POSITION;
  float2 uv : TEXCOORD0;
  float3 world : TEXCOORD1;
  float3 local : TEXCOORD2;
  float opacity : COLOR0;
};

v2f vert(appdata v)
{
  v2f o;
  o.vertex = UnityObjectToClipPos(v.vertex);
  o.uv = v.uv;
  o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
  o.local = v.vertex.xyz;
  o.opacity = _Opacity / 3; // average of r g b colors

  return o;
}

fixed4 frag(v2f i) : SV_Target
{
  Ray ray;
  // ray.origin = localize(i.world);
  ray.origin = i.local;

  // world space direction to object space
  float3 dir = (i.world - _WorldSpaceCameraPos);
  ray.dir = normalize(mul(unity_WorldToObject, dir));

  float tnear;
  float tfar;
  intersect(ray, tnear, tfar);

  tnear = max(0.0, tnear);

  // float3 start = ray.origin + ray.dir * tnear;
  float3 start = ray.origin;
  float3 end = ray.origin + ray.dir * tfar;
  float dist = abs(tfar - tnear); // float dist = distance(start, end);
  float step_size = dist / float(_StepCount);
  float3 ds = normalize(end - start) * step_size;

  float4 dst = float4(0, 0, 0, 0);
  float3 p = start;

  //[unroll(256)]
  for (int iter = 0; iter < _StepCount; ++iter)
  {
    float3 uv = get_uv(p);
    float4 src = sample_volume(_Volume, uv, p);

	src.a *= saturate((src.r+src.g+src.b) * i.opacity);

	src.rgb *= src.a;

    // blend
    dst = (1.0 - dst.a) * src + dst;

	if (dst.a > _AlphaCutoff) {
		break;
	}

    p += ds;
  }

  dst = saturate(dst);

  return dst;
}

#endif 
