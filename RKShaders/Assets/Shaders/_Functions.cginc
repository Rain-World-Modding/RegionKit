//Include file for common functions
#ifndef COMMONFUNCTIONS
#define COMMONFUNCTIONS
static const float PI = 3.14159265;


// clips by a rippleMask, depending on sideControl
// sideControl: 0 = normal side, 1 = ripple side, .5 = both sides
inline void rippleClip(fixed sideControl, fixed rippleMask){
    if(sideControl>.5)
       if (rippleMask < .2)
           discard;
    if(sideControl<.5)
        if (rippleMask > .2)
            discard;
}

inline float get_depth01(fixed level){
    level*=255;
    fixed sky_mask = step(255,level);
    level-=1;
    float r = fmod(level,30);
    return lerp(r/30,1,sky_mask);
}

inline float get_depth(fixed level){
    level*=255;
    fixed sky_mask = step(255,level);
    level-=1;
    float r = fmod(level,30);
    return lerp(r,30,sky_mask);
}

inline float get_depth01_sat(float level){
    level=saturate(level)*255.0;
    fixed sky_mask = step(255,level);
    level-=1;
    float r = fmod(level,30);
    return lerp(r/30,1,sky_mask);
}

inline float get_depth_sat(float level){
    level=saturate(level)*255.0;
    fixed sky_mask = step(255,level);
    level-=1;
    float r = fmod(level,30);
    return lerp(r,30,sky_mask);
}

inline float iLerp(float from, float to, float value){
    return (value-from)/(to-from);
}

inline float iLerpClamp(float from, float to, float value){
    return saturate(iLerp(from,to,value));
}

inline float2 iLerp(float2 from, float2 to, float2 value){
    return float2(iLerp(from.x,to.x,value.x),iLerp(from.y,to.y,value.y));
}

inline float2 iLerpClamp(float2 from, float2 to, float2 value){
    return saturate(iLerp(from,to,value));
}

inline float2x2 rotate2d(float a)
{
    return float2x2 (
            cos(a), -sin(a),
            sin(a), cos(a)
            );
}

inline float pulse(float value, float p, float a, float b){
    return lerp(smoothstep(p+b,p,value),smoothstep(p-a,p,value),step(value,p));
}

inline float pulse(float value, float p, float a){
    return smoothstep(a,0,abs(value-p));
}

inline float2 circularMotion(float a){
    return float2(sin(a),cos(a));
}

inline float mod(float x, float y){ //GLSL-like modulo function
    return x-y*floor(x/y);
}

inline float2 mod(float2 x, float y){ 
    return float2(mod(x.x,y),mod(x.y,y));
}

inline float2 mod(float2 x, float2 y){ 
    return float2(mod(x.x,y.x),mod(x.y,y.y));
}

inline float2 mirror(float2 uv,float2 screenSize){
    float2 st = 1/screenSize;
    uv = abs(1-abs(uv-1));
    uv = clamp(uv,st*2,1-st*2);
    return uv;
}

inline float mirror(float a, float b){
    return b-abs(a-b);
}

inline float3 rgb2hsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

inline float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

inline float3 hsv2rgb(float h, float s, float v)
{
    return hsv2rgb(float3(h,s,v));
}

float lightness(float3 t) {
    return max(max(t.x,t.y),t.z);
}

float2 QuantizeToPixels(float2 coord,float2 _screenSize){
    float2 pixel = 1/_screenSize;
    return coord - fmod(coord, pixel) + pixel * .5;
}

inline float4 FixEdgeShadowStretch(float2 screenPos, bool invertY){
    if (invertY) screenPos.y = 1-screenPos.y;
    return float4(saturate(iLerp(float(0.0).xx,float(0.3).xx,screenPos)),saturate(iLerp(float(0.0).xx,float(0.1).xx,screenPos)));
}

#endif
