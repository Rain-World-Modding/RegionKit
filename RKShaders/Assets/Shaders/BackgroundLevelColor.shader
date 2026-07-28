Shader "Futile/BackgroundLevelColor" {
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Color(0, 0, 0, 0) }
		Lighting Off
		Cull Off

		BindChannels {
			Bind "Vertex", vertex
			Bind "texcoord", texcoord
			Bind "Color", color
		}

		Pass {

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ RIPPLE
#pragma multi_compile _ COMBINEDLEVEL
#pragma multi_compile_local _ levelheat
#pragma multi_compile_local _ levelmelt
#include "UnityCG.cginc"
#include "_UrbanLife.cginc"
#include "_Functions.cginc"

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;
sampler2D _PalTex;
sampler2D _NoiseTex;

#if defined(SHADER_API_PSSL)
	sampler2D _GrabTexture;
#else
	sampler2D _GrabTexture : register(s0);
#endif

sampler2D _LevelTex;
sampler2D _RippleMask;
sampler2D _GameplayRippleMask;
sampler2D _RipplePalTex;
sampler2D _GameplayRipplePalTex;

float _RipplePaletteAmount;
float _RippleTrailPaletteAmount;

uniform float _rippleFogAmount;

uniform float _palette;
uniform float _RAIN;
uniform float _light = 0;
uniform float4 _spriteRect;
uniform float2 _screenOffset;

uniform float4 _lightDirAndPixelSize;
uniform float _fogAmount;
uniform float _waterLevel;
uniform float _Grime;
uniform float _SwarmRoom;
uniform float _WetTerrain;
uniform float _cloudsSpeed;
uniform float _darkness;
uniform float _contrast;
uniform float _saturation;
uniform float _hue;
uniform float _brightness;
uniform half4 _AboveCloudsAtmosphereColor;
uniform fixed _rimFix;

uniform float llfFogFalloff;
uniform float llfFogMax;

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;
	float2 scrPos : TEXCOORD2;
	float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert(appdata_full v) {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.uv2 = o.uv - _MainTex_TexelSize * .5 * _rimFix;
	o.scrPos = ComputeScreenPos(o.pos).xy;
	o.clr = v.color;
	return o;
}

inline float3 applyHue(float3 aColor, float aHue) {
	float angle = radians(aHue);
	float3 k = float3(0.57735, 0.57735, 0.57735);
	float cosAngle = cos(angle);
	return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
}

half4 frag(v2f i) : SV_Target {

	#if RIPPLE
		fixed4 rippleMask = tex2D(_RippleMask, i.scrPos);
		fixed gameplayRippleMask = tex2D(_GameplayRippleMask, i.scrPos).x;

		gameplayRippleMask = lerp(gameplayRippleMask.x, rippleMask.y * .5, rippleMask.y);

		fixed shiftWave = smoothstep(.2, -.0, abs(rippleMask.x - .2)) * -.02;
		fixed gameplayShiftWave = smoothstep(.2, -.0, abs(gameplayRippleMask - .2)) * -.02;

		shiftWave = min(lerp(shiftWave, 0, gameplayRippleMask > .2), gameplayShiftWave);
		shiftWave = min(shiftWave, -smoothstep(.25, -.0, abs(rippleMask.y - .8)) * .02);

		fixed4 rippleColor = 0;
		fixed4 gameplayRippleColor = 0;
		float gameplayPaletteAmount = lerp(gameplayRippleMask, _RippleTrailPaletteAmount, saturate(saturate(rippleMask.y - .5) * 2));
	#endif

	half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
	bool checkMaskOut = false;

	sampler2D levelTexture = _MainTex;

	float ugh = fmod(fmod(round(tex2D(_LevelTex, float2(i.uv.x, i.uv.y)).x * 255), 90) - 1, 30) / 300.0;

	float displace = tex2D(_NoiseTex, float2((i.uv.x * 1.5) - ugh + _RAIN * 0.01, (i.uv.y * 0.25) - ugh + _RAIN * 0.05)).x;
	displace = saturate((sin((displace + i.uv.x + i.uv.y + _RAIN * 0.1) * 3 * 3.14) - 0.95) * 20);

	half2 screenPos = half2(lerp(_spriteRect.x + _screenOffset.x, _spriteRect.z + _screenOffset.x, i.uv.x), lerp(_spriteRect.y + _screenOffset.y, _spriteRect.w + _screenOffset.y, i.uv.y));

	#if UNITY_UV_STARTS_AT_TOP
		screenPos.y = 1 - screenPos.y;
	#endif

	if (_WetTerrain < 0.5 || screenPos.y > _waterLevel) {
		displace = 0;
	}

	#if levelmelt
		displace = 0;
	#endif

	half4 texcol = tex2D(_MainTex, float2(i.uv2.x, i.uv2.y + displace * 0.001));
	half4 leveltexcol = tex2D(_LevelTex, float2(i.uv2.x, i.uv2.y + displace * 0.001));

	int red = round(texcol.x * 255);
	int green = round(texcol.y * 255);

	float effectCol = 0;

	float heatEffectCol = 0;
	float sky = false;
	bool melt = false;

	#if levelmelt
		melt = true;
	#endif

	if (leveltexcol.x != 1.0 || leveltexcol.y != 1.0 || leveltexcol.z != 1.0) {
		return half4(0, 0, 0, 0);
	}

	if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) {
		setColor = tex2D(_PalTex, float2(0.5 / 32.0, 7.5 / 8));

		#if levelmelt
			red = 31;
		#endif

		#if RIPPLE

			rippleColor = tex2D(_RipplePalTex, float2(0.5 / 32.0, 7.5 / 8));
			gameplayRippleColor = tex2D(_GameplayRipplePalTex, float2(0.5 / 32.0, 7.5 / 8));

			setColor = lerp(setColor, rippleColor, smoothstep(.1, .4, rippleMask.x) * _RipplePaletteAmount);
			setColor = lerp(setColor, gameplayRippleColor, smoothstep(.1, .4, gameplayPaletteAmount));

			if (_rimFix > .5 && gameplayRippleMask < .2) {
				setColor = _AboveCloudsAtmosphereColor;
			}

		#else

			if (_rimFix > .5) {
				setColor = _AboveCloudsAtmosphereColor;
			}

		#endif

		checkMaskOut = true;
		sky = true;
	} else {

		half notFloorDark = 1;

		if (green >= 16) {
			notFloorDark = 0;
			green -= 16;
		}

		if (green >= 8) {
			effectCol = 100;
			green -= 8;
		} else {
			effectCol = green;
		}

		half shadow = tex2D(_NoiseTex, float2((i.uv.x * 0.5) + (_RAIN * 0.1 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)), 1 - (i.uv.y * 0.5) + (_RAIN * 0.2 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)))).x;

		shadow = 0.5 + sin(fmod(shadow + (_RAIN * 0.1 * _cloudsSpeed) - i.uv.y, 1) * 3.14 * 2) * 0.5;
		shadow = saturate(((shadow - 0.5) * 6) + 0.5 - (_light * 4));

		if (red > 90) {
			red -= 90;
		} else {
			shadow = 1.0;
		}

		int paletteColor = clamp(floor((red - 1) / 30.0), 0, 2);

		red = fmod(red - 1, 30.0);

		#if URBANLIFE
			shadow = max(UrbanLifeShadows(i.scrPos, red), shadow);
		#endif

		if (shadow != 1) {

			float4 shadowFix = FixEdgeShadowStretch(screenPos, false);

			half2 grabPos = float2(screenPos.x + -_lightDirAndPixelSize.x * _lightDirAndPixelSize.z * (red + 25) * shadowFix.x, 1 - screenPos.y + _lightDirAndPixelSize.y * _lightDirAndPixelSize.w * (red + 25) * shadowFix.y);

			grabPos = lerp(grabPos, ((grabPos - half2(0.5, 0.3)) * (1 + (red + 25.0) / 460.0)) + half2(0.5, 0.3), shadowFix.zw);

			float4 grabTexCol2 = tex2D(_GrabTexture, grabPos);

			if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0) {
				shadow = 1;
			}

		}

		#if levelheat
			float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1 - screenPos.y));

			if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
				red = -25;
			}

			fixed reverse = 1;

			#if levelmelt
				reverse = -1;
				sky = false;
			#endif

			heatEffectCol = tex2D(_NoiseTex, half2(i.uv.x, i.uv.y * 0.5 - _RAIN * 0.123 * reverse));
			heatEffectCol = 0.5 + 0.5 * sin(heatEffectCol * 3.14 * 8 + i.uv.y * 36.231 - _RAIN * 2.63 * reverse + ((red + 30) / 30.0) * 8.12);
			heatEffectCol *= 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2((1.0 - i.uv.x) * 2, i.uv.y * 0.5 - _RAIN * 0.1862 * reverse)) * 3.14 * 8 + i.uv.y * 50.231 - _RAIN * 4.75442 * reverse + ((red + 30) / 30.0) * 8.12);
			heatEffectCol = 1 - heatEffectCol;
			heatEffectCol = pow(heatEffectCol, 30);

			half4 heatTexCol = tex2D(_MainTex, float2(i.uv.x, i.uv.y + lerp(-0.01, 0.02, heatEffectCol) * i.clr.w));

			#if RIPPLE
				heatTexCol = lerp(heatTexCol, texcol, step(.2, gameplayRippleMask));
			#endif

			texcol = heatTexCol;

			#if levelmelt
				screenPos.y -= lerp(-0.01, 0.02, heatEffectCol) * i.clr.w;
			#endif

			red = round(texcol.x * 255);
			red = fmod(red - 1, 30.0);

			heatEffectCol = 1 - abs(heatEffectCol - 0.5) * 2;
			heatEffectCol = pow(max(0, heatEffectCol - (1.0 - i.clr.w)), lerp(10.0, 1.0, i.clr.w));
		#endif

		half4 fogCol = tex2D(_PalTex, float2(1.5 / 32.0, 7.5 / 8.0));
		half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(i.uv.x * 2, i.uv.y * 2)).x * 4) + (red + 30) / 12.0) * 3.14 * 2) * 0.5) + 0.5;

		#if levelmelt
			setColor = tex2D(_PalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0));

			if (green > 0 && green < 3) {
				heatEffectCol = max(heatEffectCol, texcol.z);
			}

			if (effectCol == 100) {
				half4 decalCol = tex2D(_MainTex, float2((255.5 - round(texcol.z * 255.0)) / 1400.0, 799.5 / 800.0));

				if (paletteColor == 2) {
					decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow * 0.1);
				}

				decalCol = lerp(decalCol, fogCol, (red + 30) / 60.0);
				setColor = lerp(lerp(setColor, decalCol, 0.7), setColor * decalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, saturate(((red + 30) - 3.5) * 0.3)));
			}

			setColor = lerp(setColor, fogCol, saturate((red + 30) * _fogAmount / 30.0));

		#else

			setColor = lerp(tex2D(_PalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 3 + 0.5) / 8.0)), tex2D(_PalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0)), shadow);

			setColor = lerp(setColor, tex2D(_PalTex, float2((5.5 + rbcol * 25) / 32.0, 6.5 / 8.0)), (green >= 4 ? 0.2 : 0.0) * 0.0);

			if (effectCol == 100) {
				half4 decalCol = tex2D(_MainTex, float2((255.5 - round(texcol.z * 255.0)) / 1400.0, 799.5 / 800.0));

				if (paletteColor == 2) {
					decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow * 0.1);
				}

				decalCol = lerp(decalCol, fogCol, (red + 30) / 60.0);
				setColor = lerp(lerp(setColor, decalCol, 0.7), setColor * decalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, saturate(((red + 30) - 3.5) * 0.3)));

			} else if (green > 0 && green < 3) {

				setColor = lerp(setColor, lerp(lerp(tex2D(_PalTex, float2(30.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_PalTex, float2(31.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), shadow), lerp(tex2D(_PalTex, float2(30.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_PalTex, float2(31.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), shadow), (red + 30) / 30.0), texcol.z);

			} else if (green == 3) {

				setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z * _SwarmRoom);

			}

			setColor = lerp(setColor, fogCol, saturate((red + 30) * _fogAmount / 30.0));

		#endif

		#if RIPPLE

			half4 rippleFogCol = tex2D(_RipplePalTex, float2(1.5 / 32.0, 7.5 / 8.0));
			half4 gameplayRippleFogCol = tex2D(_GameplayRipplePalTex, float2(1.5 / 32.0, 7.5 / 8.0));

			rippleColor = lerp(tex2D(_RipplePalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 3 + 0.5) / 8.0)), tex2D(_RipplePalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0)), shadow);

			rippleColor = lerp(rippleColor, tex2D(_RipplePalTex, float2((5.5 + rbcol * 25) / 32.0, 6.5 / 8.0)), (green >= 4 ? 0.2 : 0.0) * 0.0);
			gameplayRippleColor = lerp(tex2D(_GameplayRipplePalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 3 + 0.5) / 8.0)), tex2D(_GameplayRipplePalTex, float2((29.0 * notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0)), shadow);

			gameplayRippleColor = lerp(gameplayRippleColor, tex2D(_GameplayRipplePalTex, float2((5.5 + rbcol * 25) / 32.0, 6.5 / 8.0)), (green >= 4 ? 0.2 : 0.0) * 0.0);

			if (effectCol == 100) {
				half4 decalCol = tex2D(_MainTex, float2((255.5 - round(texcol.z * 255.0)) / 1400.0, 799.5 / 800.0));

				if (paletteColor == 2) {
					decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow * 0.1);
				}

				half4 gameplayDecalCol = lerp(decalCol, gameplayRippleFogCol, (red + 30) / 60.0);
				decalCol = lerp(decalCol, rippleFogCol, (red + 30) / 60.0);
				rippleColor = lerp(lerp(rippleColor, decalCol, 0.7), rippleColor * decalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, saturate(((red + 30) - 3.5) * 0.3)));
				gameplayRippleColor = lerp(lerp(gameplayRippleColor, gameplayDecalCol, 0.7), gameplayRippleColor * gameplayDecalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, saturate(((red + 30) - 3.5) * 0.3)));

			} else if (green > 0 && green < 3) {

				rippleColor = lerp(rippleColor, lerp(lerp(tex2D(_RipplePalTex, float2(30.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_RipplePalTex, float2(31.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), shadow), lerp(tex2D(_RipplePalTex, float2(30.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_RipplePalTex, float2(31.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), shadow), (red + 30) / 30.0), texcol.z);

				gameplayRippleColor = lerp(gameplayRippleColor, lerp(lerp(tex2D(_GameplayRipplePalTex, float2(30.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_GameplayRipplePalTex, float2(31.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0)), shadow), lerp(tex2D(_GameplayRipplePalTex, float2(30.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), tex2D(_GameplayRipplePalTex, float2(31.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0)), shadow), (red + 30) / 30.0), texcol.z);

			} else if (green == 3) {

				rippleColor = lerp(rippleColor, half4(1, 1, 1, 1), texcol.z * _SwarmRoom);
				gameplayRippleColor = lerp(gameplayRippleColor, half4(1, 1, 1, 1), texcol.z * _SwarmRoom);

			}

			rippleColor = lerp(rippleColor, rippleFogCol, saturate((red + 30) * _rippleFogAmount / 30.0));
			gameplayRippleColor = lerp(gameplayRippleColor, gameplayRippleFogCol, saturate((red + 30) * .033));

			setColor = lerp(setColor, rippleColor, smoothstep(.1, .4, rippleMask.x) * _RipplePaletteAmount);
			setColor = lerp(setColor, gameplayRippleColor, smoothstep(.1, .4, gameplayPaletteAmount));

		#endif

		checkMaskOut = true;
	}

	if (checkMaskOut) {
		float4 grabTexCol = tex2D(_GrabTexture, float2(screenPos.x, 1 - screenPos.y));

		if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
			setColor.w = 0;

			#if levelmelt || levelheat
				setColor = grabTexCol;
				red = 5;
				heatEffectCol = 0;
			#endif

		}
	}

	#if levelheat
		if (sky && melt) {
			heatEffectCol = pow(1 - i.uv.y, 3) * 0.5 * i.clr.w;
		}

		if ((!sky && (green == 0 || green >= 3)) || melt) {
			heatEffectCol = lerp(heatEffectCol, 1, ((red + 30) / 30.0) * 0.1 * i.clr.w);

			if (heatEffectCol < 0.75) {
				setColor = lerp(setColor, lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)), tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)), (red + 30) / 30.0), heatEffectCol / 0.75);
			} else {
				setColor = lerp(lerp(tex2D(_PalTex, float2(31.5 / 32.0, 5.5 / 8.0)), tex2D(_PalTex, float2(30.5 / 32.0, 4.5 / 8.0)), (red + 30) / 30.0), half4(1, 1, 1, 1), heatEffectCol - 0.75);
			}
		}

	#endif

	float4 grabTexCol = tex2D(_GrabTexture, i.scrPos);

	if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0) {
		return half4(0, 0, 0, 0);
	} else {
		setColor.w = 1.0;
	}

	setColor = lerp(setColor, tex2D(_PalTex, float2(0.5 / 32.0, 7.5 / 8.0)), pow(clamp(red / llfFogMax, 0.0, 1.0), llfFogFalloff));

	#if !levelmelt
		setColor.rgb *= _darkness;
		setColor.rgb = ((setColor.rgb - 0.5) * _contrast) + 0.5;
		float greyscale = dot(setColor.rgb, float3(.222, .707, .071));
		setColor.rgb = lerp(float3(greyscale, greyscale, greyscale), setColor.rgb, _saturation);
		setColor.rgb = applyHue(setColor.rgb, _hue);
		setColor.rgb += _brightness;
	#endif

	#if RIPPLE
		setColor = fixed4(lerp(setColor.xyz, setColor.xyz * fixed3(.85, .89, 1.4), shiftWave * -80), setColor.w);
	#endif

	return setColor;
}

			ENDCG
		}
	}
}