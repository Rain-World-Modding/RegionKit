// Include file for HSL-related functions
#ifndef HSLFUNCTIONS
#define HSLFUNCTIONS

inline float3 rgb2hsl(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    float l = q.x - d * 0.5;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (1 - abs(l*2.0-1.0) + e), l);
}

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R, G, B));
}

inline float3 hsl2rgb(float3 c)
{
    // I couldn't find a good (and working) hsl to rgb easy so this just converts it to hsv first since hsv to rgb was plentiful :leditoroverload:
    c.yz = saturate(c.yz);
    float v = c.z + c.y * min(c.z, 1.0 - c.z);
    c.yz = float2(lerp(0, 2.0 * (1.0 - c.z / v), v > 0), v);
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

inline float3 rgb2hsl(float r, float g, float b)
{
    return rgb2hsl(float3(r, g, b));
}

inline float3 hsl2rgb(float r, float g, float b)
{
    return hsl2rgb(float3(r, g, b));
}

inline float3 hsllerp(float3 a, float3 b, float t)
{
    if (abs((b.x + 1) - a.x) < abs(b.x - a.x))
        b.x -= 1;
    if (abs((b.x - 1) - a.x) < abs(b.x - a.x))
        b.x += 1;
    return lerp(a, b, t);
}

inline float3 rgb_hsl_lerp(float3 a, float3 b, float t)
{
    a = rgb2hsl(a);
    b = rgb2hsl(b);
    return hsl2rgb(hsllerp(a, b, t));
}

#endif
