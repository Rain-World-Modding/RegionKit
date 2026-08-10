/*

Copyright (c) 2021 Anderson Green

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

Shader "Shaders/Lavaskin" 
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
				CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile _ RIPPLE
				#include "UnityCG.cginc"
				#include "_ShaderFix.cginc"
				#include "_Functions.cginc"

				sampler2D _MainTex;
				sampler2D _LevelTex;
				sampler2D _NoiseTex;
				sampler2D _GameplayRipplePalTex;
				sampler2D _GameplayRippleMask;
				sampler2D _UniNoise;

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

				float Lerp(float A, float B, float C, float t)
				{
					float hight = max(t * 2.0, 1.0) - 1.0;
					float lowt = min(t * 2.0, 1.0);
					float D = B + (C - B) * hight;

					return A + (D - A) * lowt;
				}

				float4 LerpCol(float4 ColA, float4 ColB, float4 ColC, float4 t)
				{
					float r = Lerp(ColA.x, ColB.x, ColC.x, t.x);
					float g = Lerp(ColA.y, ColB.y, ColC.y, t.y);
					float b = Lerp(ColA.z, ColB.z, ColC.z, t.z);
					float a = Lerp(ColA.w, ColB.w, ColC.w, t.w);
					
					return float4(r, g, b, a);
				}

				float4 frag (v2f i) : SV_Target
				{
					if(tex2D(_MainTex, i.uv).w < 0.5) return half4(0, 0, 0, 0);
					
					float2 col;
					float t = _RAIN*.1;
					float2 p = i.uv;
					float factor = 0.5;
					float2 v1;
					for(int j=0;j<12;j++)
					{
						p *= -factor/factor;
						v1 = p.yx/factor;
						p += sin(v1+col+t*10.0)/factor;
						col += float2(sin(p.x-p.y+v1.x-col.y),sin(p.y-p.x-v1.y-col.x));
					}
				  
					return LerpCol(float4(0, 0, 0, 0), float4(col.x + 4.0, col.x - col.y / 2.0, col.x / 5.0, i.clr.w), float4(1, 1, 1, 1), i.clr);
				}
				ENDCG
			}
		} 
	}
}