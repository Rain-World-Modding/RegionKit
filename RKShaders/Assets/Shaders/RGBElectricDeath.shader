// Original by Cactus based on ElectricDeath
// Recreated by Alduris
// NOTE: switched from using uniform _InputColorElecDeath to using UV channels 1 and 2

Shader "Futile/RGBElectricDeath"
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha 
		Fog { Color(0,0,0,0) }
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
				Pass 
			{
				//SetTexture [_MainTex] 
				//{
				//	Combine texture * primary
				//}
				
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "_ShaderFix.cginc"

//sampler2D _MainTex;
sampler2D _LevelTex;
sampler2D _NoiseTex;
sampler2D _NoiseTex2;
//sampler2D _PalTex;
#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif

uniform float _RAIN;
uniform float4 _spriteRect;
uniform float2 _screenSize;
//uniform float _waterPosition;

struct v2f {
	float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 elecDeathColor : TEXCOORD2;
    float4 clr : COLOR;
    
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
	o.elecDeathColor = float4(v.texcoord1.xy, v.texcoord2.xy);
    o.clr = v.color;
    return o;
}



half4 frag (v2f i) : SV_Target
{
float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

textCoord.x -= _spriteRect.x;
textCoord.y -= _spriteRect.y;

textCoord.x /= _spriteRect.z - _spriteRect.x;
textCoord.y /= _spriteRect.w - _spriteRect.y;

half4 texcol = tex2D(_LevelTex, textCoord);
if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) return half4(0,0,0,0);

half depth = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
if(depth < 1.0/30) return half4(0,0,0,0);
if(depth > 5.0/30.0){
	half4 grabCol = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
	if(grabCol.x != 0.0 || grabCol.y != 0.0 || grabCol.z != 0.0)
		return half4(0,0,0,0);
}

half2 grabCoord = textCoord + (textCoord - half2(0.5, 0.5)) * 0.4 * depth;
grabCoord += (tex2D(_NoiseTex2, half2(grabCoord.x*0.5, grabCoord.y*0.25 + _RAIN*124.12)).xy - half2(0.5, 0.5))*0.01;//lerp(0.004, 0.014, l);

half h1 = tex2D(_NoiseTex2, half2(depth*0.055 + _RAIN*0.01, _RAIN*0.07)).x;

//half l0 = 0.5 - 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*0.1 - _RAIN*0.002, grabCoord.y*0.05 + _RAIN*0.162)).x * 3.14 * 3 + _RAIN*1.6);
half l = 0.5 - 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*0.1 - _RAIN*0.012, grabCoord.y*0.15 + _RAIN*0.32)).x * 3.14 * lerp(4, 7, h1) - h1*2 + _RAIN*9.16);
half l2 = 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*0.1 - _RAIN*0.012, grabCoord.y*0.15 + 0.5 + _RAIN*0.32)).x * 3.14 * lerp(7, 4, l) + h1*0.1 + _RAIN*9.16);
l = l*min(l2+0.5, 1);//*(0.8+0.2*i.clr.w);



half h2 = 0.5 - 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*0.8 + _RAIN*0.0421, grabCoord.y*0.6 + _RAIN*0.091)).x * 3.14 * lerp(7.2, 5.4, h1) + l*lerp(0, 4, h1) + _RAIN*14.6 + depth * 0.20);
half h3 = 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*1.2 + _RAIN*0.0621, grabCoord.y*0.8 + _RAIN*0.16)).x * 3.14 * (5 + h2) - l*lerp(3, 0, h1) + _RAIN*11.6 - depth);
half h4 = 0.5 + 0.5 * sin(tex2D(_NoiseTex, half2(grabCoord.x*2 - _RAIN*0.0121, grabCoord.y*1.2 + _RAIN*0.182)).x * 3.14 * (6 - h3) + i.clr.z*2.21 + _RAIN*8.6 + depth * 0.95);

h4 *= lerp(0.9999, 1, l*i.clr.w);
h2 = max(h2, lerp(l*l, l2*l2, h3)*i.clr.w);
h3 *= lerp(0.9999, 1, i.clr.w);

half h = 1.0 - (1.0-h2) * (1.0-h3) * (1.0-h4);// * (1.0-l);


h = pow(h, lerp(300000, 12000, l*i.clr.w*i.clr.w));

h *= pow(l, lerp(0.8, 0.2, i.clr.w));

if(h > 0.5) return half4(i.elecDeathColor.xyz, (1 - depth) * 0.6 * i.clr.w);

return half4(0,0,0,0);
}
ENDCG
				
				
				
			}
		} 
	}
}
