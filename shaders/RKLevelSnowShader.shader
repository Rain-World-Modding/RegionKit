Shader "Futile/RKLevelSnowShader"
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off

		BindChannels
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord
			Bind "Color", color
		}

		SubShader 
		{
			Pass
			{
				CGPROGRAM
					#pragma target 4.0
					#pragma vertex vert
					#pragma fragment frag		
					#pragma profileoption NumInstructionSlots=4096
					#pragma profileoption NumMathInstructionSlots=4096
					#pragma exclude_renderers OpenGL
					#include "UnityCG.cginc"

#if defined(SHADER_API_PSSL)
	sampler2D _GrabTexture;
#else
	sampler2D _GrabTexture : register(s0);
#endif

sampler2D _MainTex;
sampler2D _LevelTex;

float4 _MainTex_ST;

sampler2D _NoiseTex;
sampler2D _NoiseTex2;

sampler2D _RKColoredSnowSources;
sampler2D _RKColoredSnowSources2;

float2 _LevelTex_TexelSize;
float2 _RKColoredSnowSources2_TexelSize;
float2 _RKColoredSnowSources_TexelSize;


struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 pos : SV_POSITION;
	float4 scrPos : TEXCOORD1;
	float4 clr : COLOR;
};

v2f vert(appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.clr = v.color;
	o.scrPos = ComputeScreenPos(o.pos);
	return o;
}

float GetDepth(float a)
{
	a = clamp(a, 0, 1);
	if (a == 1.0) return 255;
	a = round(a * 255);
	float shadows = (step(a, 90) * -1 + 1) * 90;
	return fmod(a - shadows - 1, 30);
}

float GetShadows(float a)
{
	a *= 255;
	return (step(round(a), 90) * -1 + 1);
}

float easeOutCubic(float t) {
	return (t = t - 1.0) * t * t + 1.0;
}

float easeInOutExpo(float t) {
	if (t == 0.0 || t == 1.0) {
		return t;
	}
	if ((t *= 2.0) < 1.0) {
		return 0.5 * pow(2.0, 10.0 * (t - 1.0));
	} else {
		return 0.5 * (-pow(2.0, -10.0 * (t - 1.0)) + 2.0);
	}
}

fixed4 frag(v2f i) : SV_Target
{
	float ratio = _LevelTex_TexelSize.y / _LevelTex_TexelSize.x;
	float2 textCoord = i.uv;
	
	float2 sourceSize = _RKColoredSnowSources_TexelSize;
	float offsetX = sourceSize.x * 0.5;
	float offsetY = sourceSize.y * 0.5;
	
	float2 source2Size = _RKColoredSnowSources2_TexelSize;
	float offset2X = source2Size.x * 0.5;
	float offset2Y = source2Size.y * 0.5;

	uint snowLayersSize = 0;
	uint palIds[256] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
	uint layerPals[20] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
	float2 snowLayers[20] = { float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1), float2(1, 1) };
	
	uint iters = (uint) (tex2D(_RKColoredSnowSources2, float2(offset2X + source2Size.x * 3, offset2Y + source2Size.y * 3)).x * 20) + 2;

	for(uint o = 0; o < iters; o++) {
		uint hor = o % 7;
		uint hor2 = (o + 20) % 7;
		uint hor3 = ((uint) (o / 4) + 40) % 7;
		
		uint ver = o / 7;
		uint ver2 = (o + 20) / 7;
		uint ver3 = ((uint) (o / 4) + 40) / 7;
		
		uint count = o % 4;
		
		fixed4 snow = tex2D(_RKColoredSnowSources, float2(offsetX + sourceSize.x * hor, offsetY + sourceSize.y * ver));
		fixed4 snow2 = tex2D(_RKColoredSnowSources, float2(offsetX + sourceSize.x * hor2, offsetY + sourceSize.y * ver2));
		uint shape = (tex2D(_RKColoredSnowSources, float2(offsetX + sourceSize.x * hor3, offsetY + sourceSize.y * ver3))[count]) * 8 + 0.4;
		
		float rad = DecodeFloatRG(snow2.xy) * 4;
		float2 coord = ((float2(DecodeFloatRG(snow.xy), DecodeFloatRG(snow.zw)) - 0.3) * 3.33333) / rad;
		float2 snowcoord = textCoord / rad;
		
		uint h2 = ((uint) (o / 2)) % 4;
		uint v2 = ((uint) (o / 2)) / 4;
		
		uint o2 = (o % 2) * 2;
		
		uint h3 = ((uint) ((o + 40) / 4)) % 4;
		uint v3 = ((uint) ((o + 40) / 4)) / 4;
		
		uint o3 = (o % 4);
		
		fixed4 snow4 = tex2D(_RKColoredSnowSources2, float2(offset2X + source2Size.x * h2, offset2Y + source2Size.y * v2));
		fixed4 snow5 = tex2D(_RKColoredSnowSources2, float2(offset2X + source2Size.x * h3, offset2Y + source2Size.y * v3));
		
		uint depthHere = (uint) GetDepth(tex2D(_LevelTex, textCoord));

		uint pal = round(snow5[o3] * 255);
		if (palIds[pal] == 0) {
			snowLayersSize++;
			palIds[pal] = snowLayersSize;
			snowLayers[palIds[pal] - 1] = float2(1, 1);
			layerPals[palIds[pal] - 1] = pal;
		}
		
		float snowIntensity = snowLayers[palIds[pal] - 1].x;
		float snowNoise = snowLayers[palIds[pal] - 1].y;
		
		float circle = -clamp(smoothstep(0.5, 0.4, abs((snowcoord.x * ratio - coord.x * ratio))) * smoothstep(0.5, 0.4, abs((snowcoord.y - coord.y))), 0, 1) + 1;

		int sign = 1;
		
		if (shape > 3) {
			shape -= 4;
			sign = 0;
		}

		if (shape >= 1 && shape < 4) {
			circle = clamp(easeOutCubic(length(float2(snowcoord.x * ratio - coord.x * ratio, snowcoord.y - coord.y))), 0, 1);
		}

		if (shape == 2) {
			circle = clamp(circle + smoothstep(_LevelTex_TexelSize.y * 30 * 0.1, _LevelTex_TexelSize.y * 30, abs(textCoord.y - coord.y * rad)), 0, 1);
		} else if (shape == 3) {
			circle = clamp(circle + smoothstep(_LevelTex_TexelSize.x * 30 * 0.1, _LevelTex_TexelSize.x * 30, abs(textCoord.x - coord.x * rad)), 0, 1);
		}
		
		if (sign == 1) {
			snowIntensity *= clamp(lerp(1 - snow2.z, 1, circle), 0, 1);
			snowNoise *= clamp(lerp(1 - snow2.w, 1, circle), 0, 1);
		} else {
			snowIntensity = clamp(snowIntensity - lerp(1 - snow2.z, 1, circle) + 1, 0, 1);
			snowNoise = clamp(snowNoise - lerp(1 - snow2.w, 1, circle) + 1, 0, 1);
		}
		
		snowLayers[palIds[pal] - 1] = float2(snowIntensity, snowNoise);
	}

	float depthNoise = tex2D(_NoiseTex, float2(textCoord.x, textCoord.y) * 2).x + tex2D(_NoiseTex2, float2(textCoord.x, textCoord.y) * 1).x * 0.3;

	fixed4 lvlCol = tex2D(_LevelTex, textCoord);

	float shadows = GetShadows(lvlCol);
	float depth = GetDepth(lvlCol.x);

	float noiseHere = tex2D(_NoiseTex, float2(textCoord.x, textCoord.y) * 5).x;
	float4 loopInfos[20] = { float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows), float4(0, 0, 0, shadows) };

	for (uint c = 0; c < 20; c++) {
		float tex = tex2D(_LevelTex, float2(textCoord.x, textCoord.y - _LevelTex_TexelSize.y * c)).x;
		float topDepth = GetDepth(tex2D(_LevelTex, float2(textCoord.x + _LevelTex_TexelSize.x * trunc(c * 0.35), textCoord.y - _LevelTex_TexelSize.y * c)).x);
		float botDepth = GetDepth(tex2D(_LevelTex, float2(textCoord.x - _LevelTex_TexelSize.x * trunc(c * 0.35), textCoord.y - _LevelTex_TexelSize.y * c)).x);
		float s = shadows;
		if (c > 0) {
			s = max(loopInfos[c - 1].w, GetShadows(tex));
		}
		loopInfos[c] = float4(tex, topDepth, botDepth, s);
	}

	float3 heightLayers[20] = { float3(clamp((trunc(depth) + 1) / 255 + shadows * 0.352941176471, 0, 1), 0, -1), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0) };

	uint mid = 0;
	for (uint i = 0; i < iters; i++) {
		float input = -snowLayers[i].x + 1;
		float input2 = -snowLayers[i].y + 1;
		
		float thicOffset = 2 + trunc(8 * clamp(input * 1.05 * depthNoise, 0, 1));
		for(uint b = 0; b < 10; b++) {

			if (b < thicOffset){
				depth = max(depth, GetDepth(tex2D(_LevelTex, float2(textCoord.x, textCoord.y + _LevelTex_TexelSize.y * b)).x));
			}

		}

		float heightNoise = noiseHere - lerp(0.25, 0.3, tex2D(_NoiseTex, float2(textCoord.x, textCoord.y + depth * 0.03) * 4).x);
		float height = clamp(lerp(input, input - heightNoise - 0.15 * (1 - input), (0.5 - input * 0.5) + input2) * 20, 0, 20);
		float minDepth = depth;
		
		float tsg = 0;
		float sg = 0;
		uint h = 0;
		
		uint h2 = ((uint) (i / 2)) % 4;
		uint v2 = ((uint) (i / 2)) / 4;
		uint o2 = (i % 2) * 2;
		fixed4 snow4 = tex2D(_RKColoredSnowSources2, float2(offset2X + source2Size.x * h2, offset2Y + source2Size.y * v2));

		for (uint c = 0; c < 20; c++) {
			if (c < height) {
				if (!(minDepth < (uint) (snow4[o2] * 30))) {
					float tex = loopInfos[c].x;
					float topDepth = loopInfos[c].y;
					float botDepth = loopInfos[c].z;

					float curDepth = clamp(depth, GetDepth(tex), max(topDepth, botDepth));

					minDepth = min(minDepth, curDepth);
					sg = max(sg, (-step(tsg - minDepth, 0.001) + 1) * c);
					tsg = minDepth;
					h++;
				}
			}
		}
		
		half shadowGradient = clamp(trunc((sg / height) * 3) * 0.11763333333, 0, 1);
		
		if(trunc(depth) != trunc(minDepth)) {
			if (!(tsg > (uint) (snow4[o2 + 1] * 30))) {
				heightLayers[i] = float3(clamp((trunc(minDepth) + 1) / 255 + loopInfos[clamp(h, 0, 19)].w * 0.352941176471 + shadowGradient, 0, 1), 1.0, tsg);
				if (tsg > heightLayers[mid].z) {
					mid = i;
				}
			}
		}
	}
	
	float3 layer = heightLayers[mid];
	return float4(layer.x, layer.y == 1.0 ? (layer.z + 10.0) / 40.0 : 0.0, layer.y == 1.0 ? layerPals[mid] / 255.0 : 0.0, 1.0);
}
				ENDCG
			}
		} 
	}
}
