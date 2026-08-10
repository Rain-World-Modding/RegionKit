// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "Futile/VectorDiamond" //Unlit Transparent Vertex Colored Additive 
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

				CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "_ShaderFix.cginc"
				#include "_RippleClip.cginc"

				sampler2D _MainTex;
				sampler2D _LevelTex;
				sampler2D _NoiseTex;
				uniform float _waterPosition;

				#if defined(SHADER_API_PSSL)
					sampler2D _GrabTexture;
				#else
					sampler2D _GrabTexture : register(s0);
				#endif

				uniform float _RAIN;

				uniform float4 _spriteRect;
				uniform float2 _screenSize;


				struct v2f
				{
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

				half4 frag (v2f i) : SV_Target
				{
					rippleClip(i.scrPos);
					
					half d = (abs(i.uv.x-0.5) + abs(i.uv.y-0.5)) * 2;
					
					if (d > 1.0 && d <= i.clr.w)
					{
						return half4(0.0, 0.0, 0.0, 0.0);
					}

					return half4(i.clr.xyz, 1);
				}
				ENDCG
				
				
			}
		} 
	}
}