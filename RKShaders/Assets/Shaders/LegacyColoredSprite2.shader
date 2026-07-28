// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "Futile/ColoredSprite2" //Unlit Transparent Vertex Colored Additive 
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

//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

//float4 _Color;
sampler2D _MainTex;
sampler2D _LevelTex;
//sampler2D _NoiseTex;
sampler2D _PalTex;
uniform float _fogAmount;
//uniform float _waterPosition;

sampler2D _GrabTexture : register(s0);

uniform float _RAIN;

uniform float4 _spriteRect;
uniform float2 _screenSize;


struct v2f {
    float4  pos : SV_POSITION;
   float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
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



half4 frag (v2f i) : COLOR
{

float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

textCoord.x -= _spriteRect.x;
textCoord.y -= _spriteRect.y;

textCoord.x /= _spriteRect.z - _spriteRect.x;
textCoord.y /= _spriteRect.w - _spriteRect.y;

//half4 texcol = tex2D(_LevelTex, textCoord);
half4 texcol = tex2D(_MainTex, float2(i.uv.x, i.uv.y));


half dpth = lerp(1.0-i.clr.w, 1.0, texcol.x);

half terrainDpth = ((uint)(tex2D(_LevelTex, textCoord).x * 255) % 30)/30.0;

if(terrainDpth > 6.0/30.0){
float4 grabTexCol = tex2D(_GrabTexture, half2(i.scrPos.x, 1-i.scrPos.y));
if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0)
terrainDpth = 6.0/30.0;
}

if(dpth > terrainDpth)
return float4(0,0,0,0);

half4 returnCol = tex2D(_PalTex, half2(lerp(0.5, 29.5, dpth)/32.0, lerp(0.5, 2.5, texcol.z)/8.0));

if(i.clr.x < 1 || i.clr.y < 1 || i.clr.z < 1)
returnCol = lerp(returnCol, i.clr, 0.25);

returnCol = lerp(returnCol, tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)), dpth*_fogAmount);
 
return float4(returnCol.xyz, texcol.w);
 

}
ENDCG
				
				
				
			}
		} 
	}
}