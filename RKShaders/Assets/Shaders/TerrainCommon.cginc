#include "_RippleClip.cginc" // modified because everything is in the same directory

sampler2D _SunlightTex;
float4 _SunlightTex_TexelSize;
float4 _lightDirAndPixelSize;
float2 _screenSize;
float4 _camInRoomRect;
float2 _sunlightOffset;
float4 _spriteRect;
float _RAIN;
float _cloudsSpeed;
sampler2D _NoiseTex;
float _light;
sampler2D _LevelTex;
sampler2D _PalTex;
float _WetTerrain;
float _waterLevel;
sampler2D _terrainPalette;
float4 _terrainPalette_TexelSize;

// Level color modifiers
uniform float _darkness;
uniform float _contrast;
uniform float _saturation;
uniform float _hue;
uniform float _brightness;

float4 _terrainParams; // (light factor, waves, edge radius, grain)
float4 _terrainParams2; // (goo height, sky fade, _, _)

#define TERRAIN_LIGHT_FACTOR _terrainParams.x
#define TERRAIN_WAVES _terrainParams.y
#define TERRAIN_EDGE_RADIUS _terrainParams.z
#define TERRAIN_GRAIN _terrainParams.w
#define TERRAIN_GOO_HEIGHT _terrainParams2.x
#define TERRAIN_SKY_FADE _terrainParams2.y

#if RIPPLE
sampler2D _RipplePalTex;
float _RipplePaletteAmount;
float _RippleTrailPaletteAmount;
uniform float _rippleFogAmount;
#endif

#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif

float4 GetRippleParams(float2 scrPos)
{
#if RIPPLE
    half4 rippleMask = tex2D(_RippleMask, scrPos); //x = normal ripple mask; y = watcher trail
    half gameplayRipple = tex2D(_GameplayRippleMask, scrPos).x;
    half gameplayRippleMask = lerp(gameplayRipple.x, rippleMask.y*.5, rippleMask.y);
    float gameplayPaletteAmount = lerp(gameplayRippleMask, _RippleTrailPaletteAmount, saturate(saturate(rippleMask.y-.5)*2));
    
    return float4(smoothstep(.1, .4, rippleMask.x) * _RipplePaletteAmount, smoothstep(.1, .4, gameplayPaletteAmount),rippleMask.y,gameplayRipple);
#else
    return float4(0, 0, 0, 0);
#endif
}

static float3 applyHue(float3 aColor, float aHue)
{
    float angle = radians(aHue);
    float3 k = float3(0.57735, 0.57735, 0.57735);
    float cosAngle = cos(angle);
	//Rodrigues' rotation formula
    return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
}

static float3 applyColorModifiers(float3 color)
{
    // From LevelColor.shader
    // Color Adjustment params
    color.rgb *= _darkness;
    color.rgb = ((color.rgb - 0.5) * _contrast) + 0.5;
    float greyscale = dot(color.rgb, float3(.222, .707, .071)); // Convert to greyscale numbers with magic luminance numbers
    color.rgb = lerp(float3(greyscale, greyscale, greyscale), color.rgb, _saturation);
    if (_hue != 0)
        color.rgb = applyHue(color.rgb, _hue);
    color.rgb += _brightness;
    return color;
}

half4 PaletteSurfaceColor(float lightness, float depth, float sunlit, float2 rippleParams)
{
    float depthLayers = (_terrainPalette_TexelSize.w - 1) / 2;
    float depthCoord = (depthLayers - 1) * saturate(depth);
    
    half4 color = tex2Dlod(_terrainPalette, float4(lightness, (1.5 + depthCoord + depthLayers * sunlit) * _terrainPalette_TexelSize.y, 0.0, 0.0));
    color.rgb = applyColorModifiers(color.rgb);
    
    #if RIPPLE
    lightness = saturate(lightness);
    depth = saturate(depth);
	half4 rippleColor = tex2D(_RipplePalTex, float2((0.5 + depth * 29.0) / 32.0, (0.5 + 2.0 * lightness + 3.0 * sunlit) / 8.0));
	half4 gameplayRippleColor = tex2D(_GameplayRipplePalTex, float2((0.5 + depth * 29.0) / 32.0, (0.5 + 2.0 * lightness + 3.0 * sunlit) / 8.0));
	color = lerp(color, rippleColor, rippleParams.x);
	color = lerp(color, gameplayRippleColor, rippleParams.y);
    #endif
    
    return color;
}

half4 PaletteGlitterColor(float2 rippleParams)
{
    half4 color = tex2Dlod(_terrainPalette, float4(float2(0.5, 0.5) * _terrainPalette_TexelSize.xy, 0.0, 0.0));
    color.rgb = applyColorModifiers(color.rgb);
    
    #if RIPPLE
	color.rgb = lerp(half3(1, 1, 1), color.rgb, (1 - rippleParams.x) * (1 - rippleParams.y));
    #endif
    
    return color;
}

half4 PaletteLightTint(float2 rippleParams)
{
    half4 color = tex2Dlod(_terrainPalette, float4(float2(1.5, 0.5) * _terrainPalette_TexelSize.xy, 0.0, 0.0));
    color.rgb = applyColorModifiers(color.rgb);
    
    #if RIPPLE
	color.rgb = lerp(half3(1,1,1), color.rgb, (1 - rippleParams.x) * (1 - rippleParams.y));
    #endif
    
    return color;
}

half4 LevelPalette(float u, float v, float2 rippleParams)
{
    half4 color = tex2Dlod(_PalTex, float4(u, v, 0.0, 0.0));
    
    color.rgb = applyColorModifiers(color.rgb);
    
    #if RIPPLE
    half4 rippleColor = tex2Dlod(_RipplePalTex, float4(u, v, 0.0, 0.0));
    half4 gameplayRippleColor = tex2Dlod(_GameplayRipplePalTex, float4(u, v, 0.0, 0.0));
    color = lerp(color, rippleColor, rippleParams.x);
    color = lerp(color, gameplayRippleColor, rippleParams.y);
    #endif
    
    return color;
}

float SampleSunlight(float2 screenPos, float layer)
{
    // Adapted from LevelColor.shader
    half2 grabPos = screenPos + _lightDirAndPixelSize.xy * _lightDirAndPixelSize.zw * float2(-1, 1) * layer;
    grabPos = ((grabPos - half2(0.5, 0.3)) * (1 + layer / 460.0)) + half2(0.5, 0.3);
    
    // Center relative to the level
    grabPos = (_camInRoomRect.xy / _camInRoomRect.zw + grabPos) * _screenSize + _sunlightOffset;
    
    return tex2D(_SunlightTex, grabPos * _SunlightTex_TexelSize.xy).r;
}

float2 ScreenToLevelTex(float2 screenPos)
{
    float2 textCoord = floor(screenPos * _screenSize) / _screenSize;
    textCoord -= _spriteRect.xy;
    textCoord /= _spriteRect.zw - _spriteRect.xy;
    
    float ugh = fmod(fmod(round(tex2D(_LevelTex, textCoord).x * 255), 90) - 1, 30) / 300.0;
    float displace = tex2D(_NoiseTex, float2((textCoord.x * 1.5) - ugh + (_RAIN * 0.01), (textCoord.y * 0.25) - ugh + _RAIN * 0.05)).x;
    displace = clamp((sin((displace + textCoord.x + textCoord.y + _RAIN * 0.1) * 3 * 3.14) - 0.95) * 20, 0, 1);
    
    if (_WetTerrain < 0.5 || 1 - screenPos.y > _waterLevel)
        displace = 0;
  
    return float2(textCoord.x, textCoord.y + displace * 0.001);
}

float ObjectShadows(float2 levelUv, float2 screenPos, float layer)
{
    float red = layer;
    half shadow = tex2D(_NoiseTex, float2((levelUv.x * 0.5) + (_RAIN * 0.1 * _cloudsSpeed) - (0.003 * red), 1 - (levelUv.y * 0.5) + (_RAIN * 0.2 * _cloudsSpeed) - (0.003 * red))).x;
    shadow = 0.5 + sin(fmod(shadow + (_RAIN * 0.1 * _cloudsSpeed) - levelUv.y, 1) * 3.14 * 2) * 0.5;
    shadow = clamp(((shadow - 0.5) * 6) + 0.5 - (_light * 4), 0, 1);

    if (shadow != 1 && red >= 5)
    {
        half2 grabPos = float2(screenPos.x + -_lightDirAndPixelSize.x * _lightDirAndPixelSize.z * (red - 5), 1 - screenPos.y + _lightDirAndPixelSize.y * _lightDirAndPixelSize.w * (red - 5));
        grabPos = ((grabPos - half2(0.5, 0.3)) * (1 + (red - 5.0) / 460.0)) + half2(0.5, 0.3);
        float4 grabTexCol2 = tex2D(_GrabTexture, grabPos);
        if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0)
        {
            shadow = 1.0;
        }
    }

    return 1.0 - shadow;
}

float LevelShadows(float2 levelUv, float dpth)
{
    float src = dpth * 30.0;
    for (float t = 0.0; t < 1.0; t += 0.025)
    {
        float target = t * src;
        half2 grabPos = levelUv.xy + float2(-1.0, 1.0) * _lightDirAndPixelSize.xy * _lightDirAndPixelSize.zw * (src - target);
        //grabPos = ((grabPos-half2(0.5, 0.3))*(1 + (src-target)/460.0))+half2(0.5, 0.3);
        float4 grabTexCol2 = tex2Dlod(_LevelTex, float4(grabPos, 0.0, 0.0));
        float depth = fmod(round(grabTexCol2.r * 255.0) - 1, 30.0);
        if (any(grabTexCol2.rgb != 1.0) && depth <= target && depth > target - 2.0)
        {
            return 0.0;
        }
    }
    return 1.0;
}
