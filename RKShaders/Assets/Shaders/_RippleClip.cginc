//To clip by ripple use #include "_RippleClip.cginc"
//And then add rippleClip(i.scrPos); at the beginning of your fragment shader.
//Keywords:
//RIPPLE - main global ripple keyword, when it's enabled rippleClip keeps shader visible on the normal side by default
//ripple_both_sides - local keyword, if it's enabled rippleClip keeps shader visible on both sides
//ripple_other_side - local keyword, if its' enabled ripleClip keeps shader visible on the ripple side
//ripple_other_side_alt - same as the previous one, but the shader is also visible through the "ripple holes" left by The Watcher
//ripple_clip_distortion - overrides other keywords, clips the area where ripple distortion is visible/strong enough
#ifndef RIPPLECLIP
#pragma multi_compile _ RIPPLE
#pragma multi_compile_local _ ripple_other_side ripple_other_side_alt ripple_both_sides ripple_clip_distortion
#define RIPPLECLIP

sampler2D _GameplayRippleMask;
sampler2D _GameplayRipplePalTex;
sampler2D _RippleMask;
fixed4 _RippleColor;
fixed4 _RippleGold;
fixed4 _GoldRGB;

inline void rippleClipDistortionOnly (float2 scrPos){
    float gMask = tex2D(_GameplayRippleMask, scrPos).x;
    float rMask = tex2D(_RippleMask, scrPos).x;
        if ( gMask > .1 && gMask < .5 )
            discard ;
        if ( rMask > .1 && rMask < .5 )
            discard ;
}

inline void rippleClip ( float2 scrPos ) {
    #if ripple_clip_distortion
        rippleClipDistortionOnly(scrPos);
        return;
    #endif
    #if ripple_both_sides
        return ;
    #elif ripple_other_side
        if ( tex2D(_GameplayRippleMask, scrPos).y < .2 )
            discard ;
            return ;
    #elif ripple_other_side_alt
        if ( tex2D(_GameplayRippleMask, scrPos).x < .2 )
            discard ;
            return ;
    #endif
#if RIPPLE
    if ( tex2D(_GameplayRippleMask, scrPos).y > .2 )
        discard ;
#endif
}

inline float transitionRippleColorMask (float2 scrPos){
#if RIPPLE || ripple_other_side
    fixed rippleMask = tex2D(_GameplayRippleMask,scrPos).z;
    return smoothstep(.0,.2,rippleMask);
#endif
    return 0;
}

inline float allRippleColorMask (float2 scrPos){
#if RIPPLE || ripple_other_side
    fixed2 rippleMask = tex2D(_GameplayRippleMask,scrPos).x;
    return smoothstep(.0,.2,rippleMask.x);
#endif
    return 0;
}

inline float rippleTrailMask (float2 scrPos){
#if RIPPLE || ripple_other_side
    fixed trailMask = tex2D(_RippleMask,scrPos).y;
    return smoothstep(.3,.9,trailMask);
#endif
    return 0;
}

inline float allRippleAndTrailColorMask(float2 scrPos){
#if RIPPLE || ripple_other_side
     float mask = allRippleColorMask(scrPos);
     mask = max(rippleTrailMask(scrPos)*.8,mask);
     return mask;
#endif
    return 0;
}

inline float rippleColorMaskKeyword(float2 scrPos){
#if RIPPLE || ripple_other_side
 float mask = allRippleAndTrailColorMask(scrPos);
 // float trail = smoothstep(.0,.4,tex2D(_RippleMask,scrPos).y);
  #if !(ripple_other_side || ripple_other_side_alt)
    mask = 1-mask;
  #endif
  return mask;
#endif
    return 0;
}

inline float rippleColorTransitionMaskKeyword(float2 scrPos){
#if RIPPLE || ripple_other_side
 float mask = transitionRippleColorMask(scrPos);
 // float trail = smoothstep(.0,.4,tex2D(_RippleMask,scrPos).y);
  #if !(ripple_other_side || ripple_other_side_alt)
    mask = 1-mask;
  #endif
  return mask;
#endif
    return 0;
}

///Use on the final image, only clips by transitional ripple mask
inline float4 smoothTransRippleClip (inout fixed4 image, fixed4 color, float2 scrPos){
#if RIPPLE || ripple_other_side
 #if ripple_both_sides
    return image;
 #else
 float mask = rippleColorTransitionMaskKeyword(scrPos);
 image.xyz = lerp(color.xyz, image.xyz, mask);
 image.w *= mask;
 #endif
#endif
return image;
}

///Use on the final image, only clips by transitional ripple mask
inline float4 smoothTransRippleClip (inout fixed4 image, float2 scrPos){
    smoothTransRippleClip(image,_RippleGold*.5,scrPos);
    return image;
}

///Use on the final image
inline float4 smoothRippleClip (inout fixed4 image, fixed4 color, float2 scrPos){
#if RIPPLE || ripple_other_side
 #if ripple_both_sides
    return image;
 #else
 float mask = rippleColorMaskKeyword(scrPos);
 image.xyz = lerp(color.xyz,image.xyz,mask);
 image.w *= mask;
 #endif
#endif
return image;
}

///Use on the final image
inline float4 smoothRippleClip (inout fixed4 image, float2 scrPos){
    smoothRippleClip(image,_RippleGold*.5,scrPos);
    return image;
}

///Use on the final image, only clips by transitional ripple mask
inline float4 smoothTransRippleClipAdditive (inout fixed4 image, fixed4 color, float2 scrPos){
#if RIPPLE || ripple_other_side
 #if ripple_both_sides
    return image;
 #else
 float mask = rippleColorTransitionMaskKeyword(scrPos);
 image.xyz = lerp(color.xyz*image.w,image.xyz,mask);
 image *= mask;
 #endif
#endif
return image;
}

///Use on the final image, only clips by transitional ripple mask
inline float4 smoothTransRippleClipAdditive (inout fixed4 image, float2 scrPos){
    smoothTransRippleClipAdditive(image,_RippleGold*.5,scrPos);
    return image;
}

///Use on the final image
inline float4 smoothRippleClipAdditive (inout fixed4 image, fixed4 color, float2 scrPos){
#if RIPPLE || ripple_other_side
 #if ripple_both_sides
    return image;
 #else
 float mask = rippleColorMaskKeyword(scrPos);
 image.xyz = lerp(color.xyz*image.w,image.xyz,mask);
 image *= mask;
 #endif
#endif
return image;
}

///Use on the final image
inline float4 smoothRippleClipAdditive (inout fixed4 image, float2 scrPos){
    smoothRippleClipAdditive(image,_RippleGold*.5,scrPos);
    return image;
}

inline void ApplyShiftWaveColor(inout fixed4 image, float rippleTexture, float trailTexture){
#if RIPPLE || ripple_other_side
    fixed gameplayShiftWave = smoothstep(.2,-.0, abs(min(rippleTexture,smoothstep(1.1,.1,trailTexture))-.2));
    image = fixed4(lerp(image.xyz, image.xyz*fixed3(.85,.89,1.4), gameplayShiftWave*1.6),image.w);
#endif
}

inline void ApplyShiftWaveColor(inout fixed4 image, float2 scrPos){
#if RIPPLE || ripple_other_side
    float rippleTexture = tex2D(_GameplayRippleMask,scrPos).x;
    float trailTexture = tex2D(_RippleMask,scrPos).y;
    fixed gameplayShiftWave = smoothstep(.2,-.0, abs(min(rippleTexture,smoothstep(1.1,.1,trailTexture))-.2));
    image = fixed4(lerp(image.xyz, image.xyz*fixed3(.85,.89,1.4), gameplayShiftWave*1.6),image.w);
#endif
}

fixed4 RippleSpawnColor(float2 scrPos){
    fixed4 rippleColor = fixed4(_RippleGold.xyz*2,1);
    fixed4 goldColor = fixed4(_GoldRGB.xyz*3,.4);
#if RIPPLE || ripple_other_side
#if ripple_other_side
    return lerp(goldColor,rippleColor,transitionRippleColorMask(scrPos));
#else
    return lerp(rippleColor,goldColor,transitionRippleColorMask(scrPos));
#endif
#endif

    return rippleColor;
}

#endif
