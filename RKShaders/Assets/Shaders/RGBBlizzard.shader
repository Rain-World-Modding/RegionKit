// Original by Cactus based on Blizzard
// Recreated by Alduris 

Shader "Futile/RGBBlizzard"
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
			// float _light = 0;
			sampler2D _MainTex;
			sampler2D _WindTexRendered;

			float4 _tileCorrection;
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			sampler2D _UniNoise;
			sampler2D _LevelTex;

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
			float ShGain(float x, float k) 
			{
				float a = 0.5*pow(2.0*((x<0.5)?x:1.0-x), k);
				return (x<0.5)?a:1.0-a;
			}
			float GetDepth (float a)
			{
				if (a==1.0) return 255;
				a=round(a*255);
				float shadows = (step(a,90)*-1+1)*90;
				return fmod(a-shadows-1, 30);
			}
			float GetDepth2 (float a)
			{
				if (a==1.0) return 255;
				a=round(a*254);
				return fmod(a, 30);
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
			fixed4 frag (v2f i) : SV_Target
			{
				float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;

				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;

				half _SNOW = _RAIN;
				half2 mapCoord = textCoord*float2(_tileCorrection.x,_tileCorrection.y)+float2(_tileCorrection.z,_tileCorrection.w);
				half scale =1;
				half snow = 0;
				half whiteout = (1+i.clr.y);
                fixed4 levelCol = tex2D(_LevelTex,textCoord);
                levelCol = AddSnow(levelCol,textCoord, i.scrPos);
				half depth = GetDepth(levelCol.x)*0.0333333333333333;	
				depth = clamp(depth,0,1);
				half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				if( (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)&&depth>0.1666666666666667) 
				depth = 0.1666666666666667 ;
				half windmap = tex2D(_WindTexRendered,mapCoord).z;
					_waterLevel+=.18-clamp((1-smoothstep(0,.3,windmap)),0,1)*.015;
				float watermask = clamp(smoothstep(1-_waterLevel-.14,_waterLevel,i.scrPos.y),0,1);
				half snInt = clamp(i.clr.x*smoothstep(0,.6,windmap)+clamp(1-step(depth,.99),0,1)*smoothstep(.6,0,windmap)*i.clr.x*.7,0,1);
				snInt*=watermask;
				
				half2 uv = i.uv*scale+half2(_SNOW,0);
				half mediumNoise = tex2D(_NoiseTex,uv*half2(2,6.3)+half2(_SNOW*6,0));
				half smallNoise = tex2D(_NoiseTex,uv*half2(2,4.1)+half2(-_SNOW*2-+mediumNoise*.1,_SNOW+mediumNoise*.1));
				half bigNoise = tex2D(_NoiseTex,uv*half2(1,3)+half2(_SNOW*2+smallNoise*.1,_SNOW*.2-smallNoise*.1));
				half small = tex2D(_UniNoise,uv*4*half2(.5,1)+half2(_SNOW*1,mediumNoise*1)).x;
				small -= tex2D(_UniNoise,uv*8*half2(.5,1)+half2(_SNOW*3,smallNoise*.1)).y;
				small -= tex2D(_UniNoise,uv*10*half2(.5,1)+half2(_SNOW*4,0)).z;
				snow = 1-smoothstep(-0,4.5,bigNoise*(2)+smallNoise*.5+bigNoise*1.5);
				snow = snow+clamp(small,0,1)*.3;
				snow = clamp(snow,0,1);
				fixed4 fog = clamp(tex2D(_PalTex, half2(1.5/32.0, 7.5/8.0))*snow+snow,0,1);
				
				// alduris: this is where the hsl gets mixed in my version. note: I use a custom hsl lerp whereas the original just rgb lerped.
				fixed4 hslSnow = fixed4(rgb_hsl_lerp(_InputColorDispSnow.xyz, _InputEndColorDispSnow.xyz, _HSLDispSnowEndLerp).xyz,1);
				fog = lerp(fog, hslSnow, _InputRGBSnowAmount);
				
                fixed4 result = (fixed4)(clamp(lerp(0,snow*snInt*.5,ShGain(depth,1-snInt*.9)*ShGain(depth,1-snInt*.8)+depth*snInt*4),0,1)*.9)*fog*whiteout*fixed4(1,1,1,snow);
                smoothRippleClip(result, i.scrPos);
				return result;


				return float4(1,1,1,1);

			}
			ENDCG
		}
	}
}
