// Original shader by Cactus
// Reconstructed from decompiled code and updated to 1.10+ by Alduris

Shader "Futile/MossWaterRGB"
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
sampler2D _NoiseTex;
uniform float _waterDepth;
float4 _airPockets[MAX_AIR_POCKETS];

sampler2D _GrabTexture;

uniform float4 _spriteRect;
uniform float2 _screenSize;

uniform float4 _InputColorMoss;


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

    o.uv = float2(0.05, 1) * TRANSFORM_TEX(v.texcoord, _MainTex);

    float noise = tex2Dlod(_NoiseTex, float4(o.uv.xy, 0, 1)).x;
    noise = 7 * saturate(noise * 10 - 5);

    float3 pos = float3(noise, noise, noise) + v.vertex;

    float3 pos1 = float3(pos.x, noise * o.uv.y + pos.y, pos.z);
    float3 pos2 = pos - float3(0,1,0);

    pos = o.uv.y < 0.1 ? pos1 : pos2;

    float4 usePos = float4(pos.xyz, noise);
    o.pos = UnityObjectToClipPos(usePos);
    o.scrPos = ComputeScreenPos(o.pos);

    o.clr = _InputColorMoss;

    return o;
}

float random(float2 seed) {
    return frac(43758.5469 * sin(dot(seed, float2(25.9796, 156.466))));
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
    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

    textCoord.x -= _spriteRect.x;
    textCoord.y -= _spriteRect.y;

    textCoord.x /= _spriteRect.z - _spriteRect.x;
    textCoord.y /= _spriteRect.w - _spriteRect.y;


    if (TerrainAndLevelDepthUnclamped(_LevelTex, textCoord, _spriteRect)/30.0 < i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0)) 
        return float4(0, 0, 0, 0);
 
    if (i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0) > 6.0/30.0) {
        half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
        if (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)
        return float4(0, 0, 0, 0);
    }

    // Moss water time

    // Predetermine some variables
    half mossNoise1 = tex2D(_NoiseTex, i.uv.xy).x;
    mossNoise1 = saturate(mossNoise1 * 10 - 5); // r0.w

    half mossNoise2 = tex2D(_NoiseTex, 0.5 * i.uv.xy).x;
    mossNoise2 = saturate(mossNoise2 * 10 - 5); // r1.w

    half mossRandom = random(i.uv.xy); // r2.x

    bool cond1 = mossNoise1 > 0; // r2.y
    bool cond2 = cond1 && mossNoise2 > 0; // r2.z

    // Determine color
    half3 mossBaseColor = float3(20.0/255.0, 110.0/255.0, 110.0/255.0);

    half4 mossColor1 = 0.5 * half4(mossBaseColor.xyz, mossNoise2 - mossNoise1) + half4(i.clr.xyz, mossNoise1); // r3.xyzw
    half4 mossColor2 = half4(mossBaseColor + i.clr.xyz, mossNoise2); // r1.xyzw
    half4 mossColor3 = half4(i.clr.xyz, mossNoise1); // r0.xyzw

    half4 mossColor = cond1 ? mossColor3 : mossColor2;
    mossColor = cond2 ? mossColor1 : mossColor;
    mossColor.xyz += mossRandom * float3(15.0/255.0, 15.0/255.0, 15.0/255.0);

    half4 finalColor = half4(mossColor.xyz, mossColor.w * (1 - i.uv.y));

    // Handle ripple graphics
    #if RIPPLE
        fixed rippleMask  = allRippleColorMask(i.scrPos);
        finalColor = lerp(finalColor, tex2D(_GameplayRipplePalTex,fixed2(7.5/32.0, 7.5/8.0)),rippleMask);
    #endif

    // Return color
    return finalColor;

}
ENDCG
                
                
                
            }
        } 
    }
}
