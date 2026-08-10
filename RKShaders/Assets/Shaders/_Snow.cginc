#pragma multi_compile _ SNOW_ON
sampler2D _SnowTex;
float2 _SlowFollowCreatureScreenPos;
#ifndef RIPPLECLIP
#include "_RippleClip.cginc"
#endif
#ifndef COMMONFUNCTIONS
#include "_Functions.cginc"
#endif


float2 _scale_from(float2 uv, float2 p, float scale) {
    float2 offsetUV = uv - p;
    offsetUV *= scale;
    return offsetUV + p - uv;
}

float2 rippleDistortion(fixed rippleMask, float2 scrPos){

#if RIPPLE
    fixed gameplayShiftTex = rippleMask;
    return _scale_from(scrPos,_SlowFollowCreatureScreenPos,1-(smoothstep(.2,-.0,abs(gameplayShiftTex-.25))*.5-saturate(gameplayShiftTex-.2))*.5);
#endif
    return 0;
}

inline fixed4 AddSnow ( fixed4 levelTex, float2 texCoord, float2 scrPos ) {
#if RIPPLE
    fixed rippleMask = tex2D(_GameplayRippleMask,scrPos).x;
    if (rippleMask > .4) return levelTex;
    texCoord+=rippleDistortion(rippleMask, scrPos);
#endif
#if SNOW_ON
    fixed4 snowcol = tex2D(_SnowTex, texCoord);
    return lerp(levelTex,half4(snowcol.x,0,1,1),snowcol.y);
#else
    return levelTex;
#endif
}

float _snowpulse(float value, float p, float a){
    return smoothstep(a,0,abs(value-p));
}

inline fixed Sparkles(fixed depth, float2 spriteUV, float2 screenUV, float4 texCol, sampler2D _UniNoise, float _RAIN)
{
	depth = 1-depth;
	fixed noise = tex2D(_UniNoise,screenUV*3+depth*5-abs(fmod(spriteUV*1,1)*2-1)*(.002+depth*.002)).x;
	fixed noise2 = tex2D(_UniNoise,screenUV*2+depth*3+_RAIN*.001+abs(fmod(spriteUV*3,1)*2-1)*(.004+depth*.004)).y;
	
    noise = fmod(noise+noise2*2,.1)*10;
    return _snowpulse(noise,.1,.01)*(texCol.x!=0);
}
