Shader "RegionKit/RKColoredDust"  
{
    Properties 
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    
    Category 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Lighting Off
        Cull Off //we can turn backface culling off because we know nothing will be facing backwards

        BindChannels 
        {
            Bind "Vertex", vertex
            Bind "texcoord", texcoord 
            Bind "Color", color 
        }

        SubShader   
        {
            GrabPass { "_GildedWindGrab" }
                Pass 
            {
            Blend SrcAlpha OneMinusSrcAlpha 
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "_ShaderFix.cginc"
#include "_RippleClip.cginc"
#include "_Functions.cginc"
#include "_TerrainMask.cginc"

sampler2D _LevelTex;
sampler2D _PreLevelColorGrab;
sampler2D _NoiseTex;
sampler2D _CloudsTex;
sampler2D _PalTex;
sampler2D _MainTex;
sampler2D _UniNoise;
sampler2D _GildedWindGrab;
float4 _spriteRect;
float4 _MainTex_ST;

uniform float _RAIN;


struct appdata_complete {
    float4 vertex : POSITION;
    //float4 tangent : TANGENT;
    //float3 normal : NORMAL;
    fixed4 color : COLOR;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    float4 texcoord3 : TEXCOORD3;
    float4 texcoord4 : TEXCOORD4;
    float4 texcoord5 : TEXCOORD5;
    float4 texcoord6 : TEXCOORD6;
    float4 texcoord7 : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 pos    : SV_POSITION;
    float2 uv     : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 dust   : TEXCOORD2;
    float4 gold   : TEXCOORD3;
    float4 clr    : COLOR;
};

v2f vert (appdata_complete v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;

    o.dust = float4(v.texcoord2.xy, v.texcoord3.xy);
    o.gold = float4(v.texcoord4.xy, v.texcoord5.xy);

    return o;
}

fixed3 color(fixed3 grabCol, fixed3 col, fixed mask){
    return fixed3(1.0 - (1.0-grabCol.x) * (1.0-col.x*mask),
            1.0 - (1.0-grabCol.y) * (1.0-col.y*mask),
            1.0 - (1.0-grabCol.z) * (1.0-col.z*mask));
}

half4 frag (v2f i) : SV_Target
{
    // Custom data for us
    float4 dust = i.dust;
    float4 gold = i.gold;

    // Stuff from vert moved here to add space back to the struct
    float2 texCoord = iLerp(_spriteRect.xy,_spriteRect.zw,i.scrPos);
    float3 palCol = max(tex2Dlod(_PalTex, float4(20/32.0, 5/8.0,0,1)), tex2Dlod(_PalTex, float4(12/32.0, 7.5/8.0,0,1))).xyz;
    palCol = max(palCol, tex2Dlod(_PalTex,float4(0.5/32.0, 7.5/8.0,0,1)).xyz);
    palCol = lerp(palCol, dust.xyz, dust.w);

    // data array (still from vert)
    float speed = abs(i.clr.z*2-1); // data[0].x
    float timeOffset = sin(_RAIN*8*sign(i.clr.x) + i.clr.x*17) * .05 * speed; // data[0].y
    float t = _RAIN * (i.clr.z*2-1)*2 + i.clr.x*11 + timeOffset; // data[1].x
    float tDepth = i.clr.y*42 + timeOffset*2; // data[1].y

    float slightOffset = _RAIN * (saturate(1-speed*2)*.01 + .02);

    fixed2 noiseTexCoord = texCoord * fixed2(2.1,1.1) * 1.2+fixed2(0.5+slightOffset,abs(t)*-.38); // data[2]
    fixed2 uniNoiseTexCoord1 = i.uv*1.1 - fixed2(slightOffset, t*.39); // data[3]
    fixed2 uniNoiseTexCoord2 = i.uv - fixed2(0.5-slightOffset, t*.67); // data[4]
    fixed2 uniNoiseTexCoord3 = i.uv * fixed2(1+i.clr.x,.9+.5)*.54 - fixed2(i.clr.x, i.clr.y + t*1.3); // data[5]

    // Normal frag stuff now, based on GildedWind.shader
    fixed creatures = tex2D(_PreLevelColorGrab, i.scrPos) != (fixed4)0;
    fixed4 levelTex = tex2D(_LevelTex, texCoord);
    levelTex = AddTerrain(levelTex, texCoord, _spriteRect);
    fixed depth = get_depth_sat(levelTex.x);
    depth = lerp(depth,min(depth,creatures*6),creatures);

    fixed dmask = saturate(iLerp(clamp(-6+tDepth,-1,29),clamp(tDepth,0,30),depth));
    fixed dsmask =  saturate(iLerp(clamp(-6+tDepth-4,-1,29),clamp(tDepth-4,0,30),depth));//mask for sparks

    fixed n = tex2D(_UniNoise, noiseTexCoord).x*1.5;
    n += tex2D(_UniNoise, uniNoiseTexCoord1).y*2;
    n += tex2D(_UniNoise, uniNoiseTexCoord2).z*1.1;

    fixed noise = tex2D(_NoiseTex, uniNoiseTexCoord3).x;
    noise += tex2D(_CloudsTex, i.uv*fixed2(1+noise*.7,.88)*0.81-fixed2(0,t*0.6)).x;
    fixed speck = pulse(n,.01+.09*i.clr.w,.2);
    i.clr.w*=2;

    speck*= noise >(1.3-saturate(i.clr.w*4)*.4);

    float2 aCuv = abs(i.uv*2-1);
    fixed fade = smoothstep(1,0.4,aCuv.x);
    fixed middle = smoothstep(1,0.8,aCuv.x);
    fixed center = smoothstep(1,0.2,aCuv.y);
    fixed goldStreaks = pulse(noise+n*.1,.7,0.1)>1-.1*saturate(speed*8);

    float w = pulse(noise+length(i.uv*2-1),.5*i.clr.w,.7+n*.2);

    w += goldStreaks*1.0*fade;
    w *= dmask;
    w += speck*2*fade*dsmask;
    w*=center*middle;
    palCol = lerp(palCol,gold.xyz,max(speck,goldStreaks));
    fixed3 grab = tex2D(_GildedWindGrab,i.scrPos).xyz;
    fixed4 result = fixed4(color(grab,palCol,w*1.5),w);
    smoothRippleClip(result,i.scrPos);
    return result;
}

ENDCG
}

        } 
    }
}

