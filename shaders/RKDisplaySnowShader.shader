Shader "Futile/RKDisplaySnowShader"
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	Category 
	{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
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
				AlphaTest Greater 0.8
				CGPROGRAM
					#pragma target 4.0
					#pragma vertex vert
					#pragma fragment frag		
					#pragma profileoption NumInstructionSlots=4096
					#pragma profileoption NumMathInstructionSlots=4096
					#pragma multi_compile __ HR
					#pragma exclude_renderers OpenGL
					#include "UnityCG.cginc"

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

#if defined(SHADER_API_PSSL)
	sampler2D _GrabTexture;
#else
	sampler2D _GrabTexture : register(s0);
#endif
	sampler2D _PalTex;

float _light = 0;

sampler2D _MainTex;
float2 _MainTex_TexelSize;
float4 _MainTex_ST;

float2 _screenSize;
float4 _spriteRect;

sampler2D _NoiseTex;

float4 _lightDirAndPixelSize;
float _RAIN;
float _cloudsSpeed;
float _fogAmount;

sampler2D _RKColoredSnowTex;
sampler2D _RKColoredSnowPalette;

float2 _RKColoredSnowPalette_TexelSize;

v2f vert (appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.clr = v.color;
	o.scrPos = ComputeScreenPos(o.pos);
	return o;
}

float GetDepth(float a) {
	if (a == 1.0) return 255;
	a = round(a * 255);
	float shadows = (step(a, 90) * -1 + 1) * 90;
	return fmod(a - shadows - 1, 30);
}

float GetShadows(float a) {
	a*=255;
	return (step(round(a),90)*-1+1);
}

half3 blendScreen(half3 baseColor, half3 blendColor) {
    return 1.0 - (1.0 - baseColor) * (1.0 - blendColor);
}

float4 frag(v2f i) : SV_Target {

	float2 textCoord = float2(floor(i.scrPos.x * _screenSize.x) / _screenSize.x, floor(i.scrPos.y * _screenSize.y) / _screenSize.y);
	textCoord.x -= _spriteRect.x;
	textCoord.y -= _spriteRect.y;

	textCoord.x /= _spriteRect.z - _spriteRect.x;
	textCoord.y /= _spriteRect.w - _spriteRect.y;

	float4 texCol = tex2D(_RKColoredSnowTex, textCoord);

	float depth = GetDepth(texCol.x);
	float4 grabColor = tex2D(_GrabTexture, float2(i.scrPos.x, i.scrPos.y));
	clip(grabColor.a - 0.8);
	
	if((grabColor.x > 0.003921568627451 || grabColor.y != 0.0 || grabColor.z != 0.0) && depth > 5.0) {
		return float4(0,0,0,0);
	}

	float shadows = GetShadows(texCol.x);
	shadows /= 2.0;
	float shadowGradient = (texCol.x - (depth + 1) / 255 - (shadows * 0.352941176471)) * 4.25;
	float4 fog = tex2D(_PalTex, float2(1.5 / 32.0, 7.5 / 8.0));

	float shadow = tex2D(_NoiseTex, float2((textCoord.x * 0.5) + (_RAIN * 0.1 * _cloudsSpeed) - (0.003 * (clamp(depth, 0, 30))), 1 - (textCoord.y * 0.5) + (_RAIN * 0.2 * _cloudsSpeed) - (0.003 * (clamp(depth, 0, 30))))).x;

	shadow = 0.5 + sin(fmod(shadow + (_RAIN * 0.1 * _cloudsSpeed) - textCoord.y, 1) * 3.14 * 2) * 0.5;
	shadow = clamp(((shadow - 0.5) * 6) + 0.5 - (_light * 4), 0, 1);

	float2 grabPos =  float2(i.scrPos.x - _lightDirAndPixelSize.x * _lightDirAndPixelSize.z * (depth - 5), i.scrPos.y + _lightDirAndPixelSize.y * _lightDirAndPixelSize.w * (depth - 5));
	grabPos = ((grabPos - float2(0.5, 0.3)) * (1 + (depth - 5.0) / 460.0)) + float2(0.5, 0.3);

	float4 grabColor2 = tex2D(_GrabTexture, grabPos);
	grabColor2 = -step(grabColor2, 0.003921568627451) + 1;

	if (depth < 6) {
		grabColor2 = float4(0, 0, 0, 0);
	}

	float2 texelSize = _RKColoredSnowPalette_TexelSize;
	float offsetX = texelSize.x * 0.5;
	float offsetY = texelSize.y * 0.5;
	uint pal = round(texCol.z * 255);
	float4 paletteCol = tex2D(_RKColoredSnowPalette, float2(offsetX + texelSize.x * (pal % 16), offsetY + texelSize.y * ((uint) (pal / 16))));
	
	float palDepth = 0.0;
	
	if (texCol.y * 40.0 > 9.0) {
		palDepth = texCol.y * 40.0 - 10.0;
	}

	float smalPal = palDepth * 0.03333;
	
	float blend = paletteCol.w;

	#if HR
		float4 snow = tex2D(_PalTex, float2(smalPal * 0.6375, 0.125 + (shadowGradient * 0.0625)));
		float4 snowPal = tex2D(_PalTex, float2(smalPal * 0.6375, 0.57 + (shadowGradient * 0.0625)));
		paletteCol = lerp(paletteCol, snowPal, smalPal * blend);
		float4 snowLight = float4(blendScreen(snowPal.xyz, paletteCol.xyz), 1.0);
		snowLight = lerp(snowLight, snowPal, smalPal * blend);
		snow = lerp(snow, snowLight, (-shadow + 1) * shadows * (-grabColor2.x + 1));
		snow = 0.02 + snow - shadowGradient * 0.01;
		snow.xyz *= paletteCol.xyz;
		snow = lerp(snow, snow + fog * 0.2, _fogAmount * smalPal * blend);
		return float4(snow.xyz, texCol.y * 40.0 < 9.0 ? 0.0 : 1.0);
	#else
		float4 snow = tex2D(_PalTex, float2(smalPal * 0.9375, 0.125 + (shadowGradient * 0.0625)));
		float4 snowPal = tex2D(_PalTex, float2(smalPal * 0.9375, 0.57 + (shadowGradient * 0.0625)));
		paletteCol = lerp(paletteCol, snowPal, smalPal * blend);
		float4 snowLight = float4(blendScreen(snowPal.xyz, paletteCol.xyz), 1.0);
		snowLight = lerp(snowLight, snowPal, smalPal * blend);
		snow = lerp(snow, snowLight, (-shadow + 1) * shadows * (-grabColor2.x + 1));
		snow += 0.2 + shadowGradient * 0.1;
		snow.xyz *= paletteCol.xyz;
		snow = lerp(snow, fog, _fogAmount * smalPal * blend);
		return float4(snow.xyz, texCol.y * 40.0 < 9.0 ? 0.0 : 1.0);
	#endif
}
				ENDCG
			}
		}
		FallBack "Transparent"
	}
}
