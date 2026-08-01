// Original shader by Cactus and SlimeCubed
// Updated to Watcher + some other tweaks by Alduris

Shader "Futile/ReflectiveWater"
{
    Properties 
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    
    Category 
    {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha // SrcAlpha OneMinusSrcAlpha
		Fog { Color(0, 0, 0, 0) }
		Lighting Off

        Cull Back // NOTE! cull back so we don't get reflections on the wrong side

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
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "_ShaderFix.cginc"
#include "_TerrainMask.cginc"
#include "_RippleClip.cginc"
#define MAX_AIR_POCKETS 8

sampler2D _MainTex;
sampler2D _LevelTex;
sampler2D _PalTex;
sampler2D _RipplePalTex;
sampler2D _NoiseTex;
uniform float _waterDepth;
float4 _airPockets[MAX_AIR_POCKETS];

sampler2D _GrabTexture;
sampler2D _PreLevelColorGrab;

uniform float _ReflectionLerp;
uniform float _AlphaReflective;

uniform float4 _spriteRect;
uniform float2 _screenSize;

uniform float _palette;
uniform float _RAIN;
uniform float _light = 0;
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
uniform float _rippleFogAmount;
uniform float _RipplePaletteAmount;
uniform float _RippleTrailPaletteAmount;

inline float3 applyHue(float3 aColor, float aHue)
{
	float angle = radians(aHue);
	float3 k = float3(0.57735, 0.57735, 0.57735);
	float cosAngle = cos(angle);
	//Rodrigues' rotation formula
	return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
}

half4 LevelColor(float2 textCoord, float2 scrPos, half4 texcol)
{
	half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
	bool checkMaskOut = false;
	float depthCol = 40.0;
	
#if RIPPLE
	fixed4 rippleMask = tex2Dlod(_RippleMask, float4(scrPos, 0,0));//x = normal ripple mask; y = watcher trail
	fixed gameplayRippleMask = tex2Dlod(_GameplayRippleMask, float4(scrPos, 0,0)).x;
	gameplayRippleMask = lerp(gameplayRippleMask.x, rippleMask.y*.5, rippleMask.y);
	fixed shiftWave = smoothstep(.2,-.0, abs(rippleMask.x-.2))*-.02;
	fixed gameplayShiftWave = smoothstep(.2,-.0, abs(gameplayRippleMask-.2))*-.02;
	shiftWave = min(lerp(shiftWave, 0, gameplayRippleMask>.2), gameplayShiftWave); //combine cosmetic ripple color rim with gameplay ripple
	shiftWave = min(shiftWave, -smoothstep(.25, -.0, abs(rippleMask.y-.8))*.02); //add color rim to the trail
	fixed4 rippleColor = 0;
	fixed4 gameplayRippleColor = 0;
	float gameplayPaletteAmount = lerp(gameplayRippleMask, _RippleTrailPaletteAmount, saturate(saturate(rippleMask.y-.5)*2));
	rippleColor = tex2Dlod(_RipplePalTex, float4(0.5/32.0, 7.5/8, 0,0));
	gameplayRippleColor = tex2Dlod(_GameplayRipplePalTex, float4(0.5/32.0, 7.5/8, 0,0));
	setColor = lerp(setColor, rippleColor, smoothstep(.1, .4,rippleMask.x)*_RipplePaletteAmount);
	setColor = lerp(setColor, gameplayRippleColor, smoothstep(.1, .4, gameplayPaletteAmount));
#endif // RIPPLE

	if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0)
	{
		setColor = tex2Dlod(_PalTex, float4(0.5 / 32.0, 7.5 / 8, 0, 0));
		checkMaskOut = true;
	}
	else
	{
		int red = round(texcol.x * 255);
		int green = round(texcol.y * 255);
   
		int effectCol = 0;
		half notFloorDark = 1;
		if (green >= 16)
		{
			notFloorDark = 0;
			green -= 16;
		}
		if (green >= 8)
		{
			effectCol = 100;
			green -= 8;
		}
		else
			effectCol = green;
		
		half shadow = tex2Dlod(_NoiseTex, float4((textCoord.x * 0.5) + (_RAIN * 0.1 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)), 1 - (textCoord.y * 0.5) + (_RAIN * 0.2 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)), 0,0)).x;
		shadow = 0.5 + sin(fmod(shadow + (_RAIN * 0.1 * _cloudsSpeed) - textCoord.y, 1) * 3.14 * 2) * 0.5;
		shadow = clamp(((shadow - 0.5) * 6) + 0.5 - (_light * 4), 0, 1);

		if (red > 90)
			red -= 90;
		else
			shadow = 1.0;
   
		int paletteColor = clamp(floor((red - 1) / 30.0), 0, 2); //some distant objects want to get palette color 3, so we clamp it
 
		red = fmod(red - 1, 30.0);
  
		if (shadow != 1 && red >= 5)
		{
			half2 grabPos = float2(scrPos.x + -_lightDirAndPixelSize.x * _lightDirAndPixelSize.z * (red - 5), 1 - scrPos.y + _lightDirAndPixelSize.y * _lightDirAndPixelSize.w * (red - 5));
			grabPos = ((grabPos - half2(0.5, 0.3)) * (1 + (red - 5.0) / 460.0)) + half2(0.5, 0.3);
			float4 grabTexCol2 = tex2Dlod(_GrabTexture, half4(grabPos,0,0));
			if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0)
			{
				shadow = 1;
			}
		}
   
		setColor = lerp(tex2Dlod(_PalTex, float4((red * notFloorDark) / 32.0, (paletteColor + 3 + 0.5) / 8.0, 0,0)), tex2Dlod(_PalTex, float4((red * notFloorDark) / 32.0, (paletteColor + 0.5) / 8.0, 0,0)), shadow);

		half rbcol = (sin((_RAIN + (tex2Dlod(_NoiseTex, float4(textCoord.xy * 2, 0,0)).x * 4) + red / 12.0) * 3.14 * 2) * 0.5) + 0.5;
		setColor = lerp(setColor, tex2Dlod(_PalTex, float4((5.5 + rbcol * 25) / 32.0, 6.5 / 8.0, 0,0)), (green >= 4 ? 0.2 : 0.0) * _Grime);
	
		if (effectCol == 100)
		{
			half4 decalCol = tex2Dlod(_LevelTex, float4((255.5 - round(texcol.z * 255.0)) / 1400.0, 799.5 / 800.0, 0,0));
			if (paletteColor == 2)
				decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow * 0.1);
			decalCol = lerp(decalCol, tex2Dlod(_PalTex, float4(1.5 / 32.0, 7.5 / 8.0, 0,0)), red / 60.0);
			setColor = lerp(lerp(setColor, decalCol, 0.7), setColor * decalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, clamp((red - 3.5) * 0.3, 0, 1)));
		}
		else if (green > 0 && green < 3)
		{
			setColor = lerp(setColor, lerp(lerp(tex2Dlod(_PalTex, float4(30.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0, 0,0)), tex2Dlod(_PalTex, float4(31.5 / 32.0, (5.5 - (effectCol - 1) * 2) / 8.0, 0,0)), shadow), lerp(tex2Dlod(_PalTex, float4(30.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0, 0,0)), tex2Dlod(_PalTex, float4(31.5 / 32.0, (4.5 - (effectCol - 1) * 2) / 8.0, 0,0)), shadow), red / 30.0), texcol.z);
		}
		else if (green == 3)
		{
			setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z * _SwarmRoom);
		}
   
		setColor = lerp(setColor, tex2Dlod(_PalTex, float4(1.5 / 32.0, 7.5 / 8.0, 0,0)), clamp(red * (red < 10 ? lerp(notFloorDark, 1, 0.5) : 1) * _fogAmount / 30.0, 0, 1));

		
#if RIPPLE // copypasta of the original palette code with minor adjustments
		half4 rippleFogCol = tex2Dlod(_RipplePalTex, float4(1.5/32.0, 7.5/8.0, 0,0));
		half4 gameplayRippleFogCol = tex2Dlod(_GameplayRipplePalTex, float4(1.5/32.0, 7.5/8.0, 0,0));

		rippleColor = lerp(tex2Dlod(_RipplePalTex, float4((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0, 0,0)),
						   tex2Dlod(_RipplePalTex, float4((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0, 0,0)),
						   shadow);
		rippleColor = lerp(rippleColor, 
						   tex2Dlod(_RipplePalTex, float4((5.5 + rbcol*25)/32.0, 6.5 / 8.0, 0,0) ),
						   (green >= 4 ? 0.2 : 0.0) * _Grime);
		gameplayRippleColor =
			  lerp(tex2Dlod(_GameplayRipplePalTex, float4((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0, 0,0)),
				   tex2Dlod(_GameplayRipplePalTex, float4((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0, 0,0)),
				   shadow);
		gameplayRippleColor = lerp(gameplayRippleColor,
								   tex2Dlod(_GameplayRipplePalTex, float4((5.5 + rbcol*25)/32.0, 6.5 / 8.0, 0,0) ),
								   (green >= 4 ? 0.2 : 0.0) * _Grime);

		if (effectCol == 100) 
		{
			half4 decalCol = tex2Dlod(_MainTex, float4((255.5-round(texcol.z*255.0))/1400.0, 799.5/800.0, 0,0));
			if(paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
			half4 gameplayDecalCol = lerp(decalCol, gameplayRippleFogCol, red/60.0);
			decalCol = lerp(decalCol, rippleFogCol, red/60.0);
			rippleColor = lerp(lerp(rippleColor, decalCol, 0.7),
							   rippleColor*decalCol*1.5,
							   lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
			gameplayRippleColor = lerp(lerp(gameplayRippleColor, gameplayDecalCol, 0.7),
									   gameplayRippleColor*gameplayDecalCol*1.5,
									   lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
		}
		else if (green > 0 && green < 3) {
			rippleColor = 
			lerp(rippleColor, 
				 lerp(lerp(tex2Dlod(_RipplePalTex, float4(30.5/32.0, (5.5-(effectCol-1)*2)/8.0, 0,0)),
						   tex2Dlod(_RipplePalTex, float4(31.5/32.0, (5.5-(effectCol-1)*2)/8.0, 0,0)),
						   shadow),
					  lerp(tex2Dlod(_RipplePalTex, float4(30.5/32.0, (4.5-(effectCol-1)*2)/8.0, 0,0)),
						   tex2Dlod(_RipplePalTex, float4(31.5/32.0, (4.5-(effectCol-1)*2)/8.0, 0,0)),
						   shadow),
					  red/30.0),
				 texcol.z);

			gameplayRippleColor =
			lerp(gameplayRippleColor,
				 lerp(lerp(tex2Dlod(_GameplayRipplePalTex, float4(30.5/32.0, (5.5-(effectCol-1)*2)/8.0, 0,0)),
						   tex2Dlod(_GameplayRipplePalTex, float4(31.5/32.0, (5.5-(effectCol-1)*2)/8.0, 0,0)),
						   shadow),
					  lerp(tex2Dlod(_GameplayRipplePalTex, float4(30.5/32.0, (4.5-(effectCol-1)*2)/8.0, 0,0)),
						   tex2Dlod(_GameplayRipplePalTex, float4(31.5/32.0, (4.5-(effectCol-1)*2)/8.0, 0,0)),
						   shadow),
					  red/30.0),
				 texcol.z);
		} else if (green == 3) {
			rippleColor = lerp(rippleColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
			gameplayRippleColor = lerp(gameplayRippleColor, half4(1, 1, 1, 1), texcol.z*_SwarmRoom);
		}
	
		rippleColor =
			lerp(rippleColor,
				 rippleFogCol,
				 saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_rippleFogAmount/30.0));
		gameplayRippleColor =
			lerp(gameplayRippleColor,
				 gameplayRippleFogCol,
				 saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*.033));

		setColor = lerp(setColor,rippleColor,smoothstep(.1,.4,rippleMask.x)*_RipplePaletteAmount);
		setColor = lerp(setColor,gameplayRippleColor,smoothstep(.1,.4,gameplayPaletteAmount));
#endif // RIPPLE

		if (red >= 5)
		{
			checkMaskOut = true;
		}
	}
	
	// Color Adjustment params
	setColor.rgb *= _darkness;
	setColor.rgb = ((setColor.rgb - 0.5) * _contrast) + 0.5;
	float greyscale = dot(setColor.rgb, float3(.222, .707, .071)); // Convert to greyscale numbers with magic luminance numbers
	setColor.rgb = lerp(float3(greyscale, greyscale, greyscale), setColor.rgb, _saturation);
	setColor.rgb = applyHue(setColor.rgb, _hue);
	setColor.rgb += _brightness;
	
#if RIPPLE
	setColor = fixed4(lerp(setColor.xyz, setColor.xyz*fixed3(.85,.89,1.4), shiftWave*-80),setColor.w);
#endif

	setColor.a = depthCol;

	return setColor;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct v2f {
    float4 pos    : SV_POSITION;
    float2 uv     : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr    : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;
    return o;
}

half4 frag (v2f i) : SV_Target
{
    // Cut out around air pockets
    if (i.clr.a == 1) {
        for (int j = 0; j < MAX_AIR_POCKETS && _airPockets[j].z > _airPockets[j].x; j++) {
            float4 bounds = _airPockets[j];
            if (all(i.scrPos > bounds.xy) && all(i.scrPos < bounds.zw))
                discard;
        }
    }

    // WaterSurface normal code
    float2 textCoord = float2(floor(i.scrPos.x * _screenSize.x) / _screenSize.x, floor(i.scrPos.y * _screenSize.y) / _screenSize.y); 

    textCoord.x -= _spriteRect.x;
    textCoord.y -= _spriteRect.y;

    textCoord.x /= _spriteRect.z - _spriteRect.x;
    textCoord.y /= _spriteRect.w - _spriteRect.y;


    //float origDepth = TerrainAndLevelDepthUnclamped(_LevelTex, textCoord, _spriteRect);
    //float origDepth = TerrainAndLevelDepth(_LevelTex, textCoord, _spriteRect);
	
    half4 texcol = tex2D(_LevelTex, textCoord);
    half levelDepth = fmod(round(texcol.r * 255) - 1, 30.0);
    
    if (all(texcol.rgb == 1)) {
        levelDepth = 30;
	}
    
    half4 terrain = TerrainAtLevelPos(textCoord, _spriteRect);
    half terrainDepth = terrain.r * 30;

	float origDepth = min(levelDepth, terrainDepth);
    float waterDepth = i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0);

    if (any(texcol.xyz != 1.0) && origDepth / 30.0 < waterDepth) {
        return float4(0, 0, 0, 0);
    }
 
    if (waterDepth > 6.0/30.0) {
        half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
        if (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)
        return float4(0, 0, 0, 0);
    }

    // Reflective water
    float3 v_ray = float3(0.0, 0.0, 1.0);
    float3 v_reflect = reflect(v_ray, normalize(i.clr.rgb));
    
	float3 origin = float3(i.scrPos.xy * _screenSize, waterDepth * 30.0);
    float4 finalColor = float4(0,0,0,0);
	float STEPSIZE = 2.0;
	bool doGrabTexCheck = origin.z < 5.0;

	[loop]
	for (int step = 0; step < 64; step++)
	{
		origin += v_reflect * STEPSIZE;
		// Check for if depth z is 5, if so, check if GrabTexture returns not black for objects
		if (doGrabTexCheck && origin.z >= 5.0)
		{
			doGrabTexCheck = false;
			float4 grabOrig = tex2Dlod(_GrabTexture, float4(origin.xy / _screenSize.xy, 0, 0));
			if (any(grabOrig.rgb != 0.0))
			{
				finalColor = grabOrig;
				break;
			}
		}
						
		// Reflects geo with level color being the final color
		float2 sampleCoord = (floor(origin.xy) / _screenSize - _spriteRect.xy) / (_spriteRect.zw - _spriteRect.xy);
		half4 sampleColor = tex2Dlod(_LevelTex, float4(sampleCoord.xy, 0, 0));
		float levelDepth = fmod(round(sampleColor.r * 255) - 1, 30.0);
		if (origin.z > levelDepth && origin.z < levelDepth + 5 && sampleColor.r != 1.0)
		{
			half4 levelColor = LevelColor(sampleCoord, i.scrPos, sampleColor);
			finalColor = levelColor;
			break;
		}
	}

	if (all(finalColor.xyz == 0.0)) {
		finalColor = tex2D(_PalTex, float2(0.5 / 32.0, 7.5 / 8.0)); // sky color
	}

	finalColor = float4(_AlphaReflective * finalColor.rgb, 1.0 - _ReflectionLerp);

    // Return color
    return finalColor;

}
ENDCG
                
                
                
            }
        } 
    }
}
