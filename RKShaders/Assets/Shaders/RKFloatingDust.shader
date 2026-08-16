Shader "RegionKit/Dust" 
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
#pragma multi_compile_local _ lightdust
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

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
    float2 texCoord : TEXCOORD3;
    fixed3 palCol : COLOR01;
    float2 data[6] : TEXCOORD4;
};

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.texCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos);
    o.clr = v.color;

    o.palCol = max(tex2Dlod(_PalTex,float4(20/32.0, 5/8.0,0,1) ),tex2Dlod(_PalTex,float4(12/32.0, 7.5/8.0,0,1) ));
    o.palCol = max(o.palCol,tex2Dlod(_PalTex,float4(0.5/32.0, 7.5/8.0,0,1) ).xyz );
#if lightdust
    o.palCol = lerp(o.palCol,fixed3(1,1,1),.5);
#else
    o.palCol = lerp(o.palCol, fixed3(0,0,0), .5);
#endif

    o.data[0].x = abs(o.clr.z*2-1);//speed
    o.data[0].y = sin(_RAIN*(8)*sign(o.clr.x)+o.clr.x*17)*.05*o.data[0].x;//timeOffset
    o.data[1].x = _RAIN*(o.clr.z*2-1)*2+o.clr.x*11+o.data[0].y;//t
    o.data[1].y = o.clr.y*42+o.data[0].y*2;//tdepth
    float slightOffset=+_RAIN*(saturate(1-o.data[0].x*2)*.01+.02);

    o.data[2] = o.texCoord*fixed2(2.1,1.1)*1.2+fixed2(0.5+slightOffset,abs(o.data[1].x)*-.38);//noise TexCoord
    o.data[3] = o.uv*1.1-fixed2(0+slightOffset,o.data[1].x*.39);// uniNoise TexCoord1
    o.data[4] = o.uv-fixed2(0.5-slightOffset,o.data[1].x*.67);// uniNoise TexCoord2
    o.data[5] = o.uv*fixed2(1+o.clr.x,.9+.5)*.54-fixed2(o.clr.x,o.clr.y+o.data[1].x*1.3);// uniNoise TexCoord3

    return o;
}

fixed3 color(fixed3 grabCol, fixed3 col, fixed mask){
    return fixed3(1.0 - (1.0-grabCol.x) * (1.0-col.x*mask),
            1.0 - (1.0-grabCol.y) * (1.0-col.y*mask),
            1.0 - (1.0-grabCol.z) * (1.0-col.z*mask));
}

half4 frag (v2f i) : SV_Target
{
    // Largely taken from GildedWind.shader
    fixed creatures = tex2D(_PreLevelColorGrab, i.scrPos) != (fixed4)0;
    fixed4 levelTex = tex2D(_LevelTex, i.texCoord);
    levelTex = AddTerrain(levelTex, i.texCoord, _spriteRect);
    fixed depth = get_depth_sat(levelTex.x);
    depth = lerp(depth,min(depth,creatures*6),creatures);

    fixed dmask = saturate(iLerp(clamp(-6+i.data[1].y,-1,29),clamp(i.data[1].y,0,30),depth));
    fixed dsmask =  saturate(iLerp(clamp(-6+i.data[1].y-4,-1,29),clamp(i.data[1].y-4,0,30),depth));//mask for sparks

    fixed n = tex2D(_UniNoise, i.data[2]).x*1.5;
    n += tex2D(_UniNoise, i.data[3]).y*2;
    n += tex2D(_UniNoise, i.data[4]).z*1.1;

    fixed noise = tex2D(_NoiseTex, i.data[5]).x;
    noise += tex2D(_CloudsTex, i.uv*fixed2(1+noise*.7,.88)*0.81-fixed2(0,i.data[1].x*0.6)).x;
    fixed speck = pulse(n,.01+.09*i.clr.w,.2);
    i.clr.w*=2;

    speck*= noise >(1.3-saturate(i.clr.w*4)*.4);

    float2 aCuv = abs(i.uv*2-1);
    fixed fade = smoothstep(1,0.4,aCuv.x);
    fixed middle = smoothstep(1,0.8,aCuv.x);
    fixed center = smoothstep(1,0.2,aCuv.y);

    float w = pulse(noise+length(i.uv*2-1),.5*i.clr.w,.7+n*.2);

    w *= dmask;
    w += speck*2*fade*dsmask;
    w*=center*middle;
    fixed3 grab = tex2D(_GildedWindGrab,i.scrPos).xyz;
    fixed4 result = fixed4(color(grab,i.palCol,w*1.5),w);
    smoothRippleClip(result,i.scrPos);
    return result;
}

ENDCG
}

		} 
	}
}

