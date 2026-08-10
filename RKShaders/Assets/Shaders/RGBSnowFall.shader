// Original by Cactus based on SnowFall
// Recreated by Alduris 

Shader "Futile/RGBSnowFall"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha 
		// Blend One One
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off 
		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}
		Pass
		{
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag		

			#include "UnityCG.cginc"
			#include "_ShaderFix.cginc"
			#include "_Snow.cginc"
			#include "_RippleClip.cginc"
			#include "_HSL.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float4 scrPos : TEXCOORD1;
				float4 clr : COLOR;
			};
			#if defined(SHADER_API_PSSL)
			sampler2D _GrabTexture;
			#else
			sampler2D _GrabTexture : register(s0);
			#endif
			sampler2D _PalTex;
			float4 _tileCorrection;

			sampler2D _MainTex;
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			sampler2D _WindTexRendered;
			sampler2D _LevelTex;
			sampler2D _UniNoise;

			float2 _LevelTex_TexelSize;
			float2 _screenSize;
			float4 _spriteRect;
			float _waterLevel;
			float _RAIN;
			float _fogAmount;
			sampler2D _NoiseTex;
			sampler2D _NoiseTex2;

			uniform float4 _InputColorDispSnow; // normal snow color
			uniform float _InputRGBSnowAmount; // I have no clue what this was originally supposed to be but however it worked previously I'm changing it because the old way sucks
			uniform float4 _InputEndColorDispSnow; // end snow color
			uniform float _HSLDispSnowEndLerp; // lerp amount
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex)+_tileCorrection.zw*3.333f;
				o.clr = v.color;
				o.scrPos = ComputeScreenPos(o.pos);
				return o;
			}
			float ShCubicPulse( float position, float width, float value )
			{
				value = abs(value - position);
				if( value>width ) return 0.0;
				value /= width;
				return 1.0 - value*value*(3.0-2.0*value);
			}
			float GetDepth (float a)
			{
				if (a==1.0) return 255;
				a=round(a*255);
				float shadows = (step(a,90)*-1+1)*90;
				return fmod(a-shadows-1, 30);
			}
			float2 Quantize(float2 coord,float2 res)
			{
				coord = ((ceil((coord)*res)+0.5)/res);
				return coord;
			}
			fixed Noise(float2 uv)
			{
				uv = Quantize(uv,fixed2(256,256));
				fixed noise_1 = frac(sin(dot(uv.xy ,float2(12.9898,78.23))) * 43758.5453);
				return noise_1;
			}
			fixed4 Noise4(float2 uv)
			{
				uv = Quantize(uv,fixed2(256,256));
				fixed noise_1 = frac(sin(dot(uv.xy ,float2(12.9898,78.23))) * 43758.5453);
				fixed noise_2 = frac(sin(dot(uv.xy ,float2(12.9898+.1,78.23+.1))) * 43758.5453);
				fixed noise_3 = frac(sin(dot(uv.xy ,float2(12.9898+.2,78.23+.2))) * 43758.5453);
				fixed noise_4 = frac(sin(dot(uv.xy ,float2(12.9898+.3,78.23+.3))) * 43758.5453);
				return fixed4(noise_1,noise_2,noise_3,noise_4);
			}
			fixed smoothNoise(fixed2 st) {
				fixed2 i = floor(st);
				fixed2 f = frac(st);

				fixed2 u = f*f*(3.0-2.0*f);

				return lerp( lerp( dot( Noise(i + fixed2(0.0,0.0) ), f - fixed2(0.0,0.0) ),
									dot( Noise(i + fixed2(1.0,0.0) ), f - fixed2(1.0,0.0) ), u.x),
							lerp( dot( Noise(i + fixed2(0.0,1.0) ), f - fixed2(0.0,1.0) ),
									dot( Noise(i + fixed2(1.0,1.0) ), f - fixed2(1.0,1.0) ), u.x), u.y)*.5+.5;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;

				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;
				
				half _SNOW = _RAIN*.3;
				half2 mapCoord = textCoord*half2(_tileCorrection.x,_tileCorrection.y)+half2(_tileCorrection.z,_tileCorrection.w);
                fixed4 levelCol = tex2D(_LevelTex,textCoord);
                levelCol = AddSnow(levelCol,textCoord,i.scrPos);
				half depth = GetDepth(levelCol.x)*0.0333333333333333;	
				half windmap = tex2D(_WindTexRendered,mapCoord).y;
				_waterLevel+=.14-clamp((1-smoothstep(0,.1,windmap)),0,1)*.015;
				float watermask = clamp(smoothstep(1-_waterLevel-.05,_waterLevel,i.scrPos.y),0,1);
				half snInt = i.clr.x*smoothstep(.0,.4,windmap)+clamp(1-step(depth,.99),0,1)*smoothstep(.4,0,windmap)*i.clr.x*.4;
				snInt*=watermask;
				half scale =12;
				half snow = 0;
				half speedMult = 1.5;
				half2 uv = i.uv*scale;
				half sas = 0;
				int layers = 30;
				for (int r = -1; r<8;r++)
				{
					fixed k=(r*3);
					half random =  Noise( k*.03);
					half n = (layers-k)*.03333;
					half angleDeviation = (random*2-1)*(1-n)*2;
					half bigNoiseX = tex2D(_NoiseTex,uv*(.6+random*.3)*half2(1,.4)+random+half2(_SNOW*angleDeviation,-_SNOW*(2-random)+k*.3+random*10))*2-1;
					half bigNoiseY = tex2D(_NoiseTex,uv*(.6+random*.3)*half2(.4,1)+random+half2(-_SNOW*(2-random)+k*.3+random*10,_SNOW*angleDeviation))*2-1;
					float2 coord2 = uv*(.5+k*.01)+half2(-random+k*1.33,random+k*1.33)+half2(bigNoiseX*.02+_SNOW*angleDeviation,bigNoiseY*.01+_SNOW*speedMult*(.4+n)+k*.01+random*3);
					half4 noise = tex2D(_UniNoise,coord2);
					half noise2 = noise.x*noise.y-noise.z*noise.w;
					float sn = (ShCubicPulse(.5*0.0233,snInt*.01*0.9,noise2));
					snow=max(snow,sn*n);
					
				}
				half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				if( (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)&&(1-snow*.9)>0.1666666666666667) 
				{
					return fixed4(0,0,0,0);
				}
				fixed4 fog = tex2D(_PalTex, half2(1.5/32.0, 7.5/8.0));				
				fixed4 colSnow = fixed4(tex2D(_PalTex,half2((1-snow*.8)*0.9375,0.125+0.0625)).xyz,1);

				// alduris: this is where the hsl gets mixed in my version. note: I use a custom hsl lerp whereas the original just rgb lerped.
				fixed4 hslSnow = fixed4(rgb_hsl_lerp(_InputColorDispSnow.xyz, _InputEndColorDispSnow.xyz, _HSLDispSnowEndLerp).xyz,1);
				colSnow = lerp(colSnow, hslSnow, _InputRGBSnowAmount);

				colSnow = lerp(colSnow,fog,_fogAmount*(1-snow*.9));
				if ((1-snow*.9)<depth)
				{
					if (snow==0){
						return 0;
					}
				
					half mask = 1-step(snow,0.01);
					snow = lerp(0,snow*.4+.4,mask);
					half4 result = (colSnow*snow)*fixed4(1,1,1,mask); // changed
					smoothRippleClip(result,i.scrPos);
					return result;
				}
				return 0;
			}
			ENDCG
		}

	}
}
