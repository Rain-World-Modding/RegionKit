Shader "Futile/WaterSlushFade"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite On
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
            #pragma multi_compile __ HR
			// #pragma geometry geom		
			#include "UnityCG.cginc"
			#include "_ShaderFix.cginc"

			#define MAX_AIR_POCKETS 8
			uniform float4 _airPockets[MAX_AIR_POCKETS];


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2g
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float4 scrPos : TEXCOORD1;
				float4 clr : COLOR;
				float2 windMask : COLOR1;
				float clones : COLOR2;
				float2 snowflakecoord : COLOR3;
				float2 MagmaCoord[5] : TEXCOORD2;

			};

			#if defined(SHADER_API_PSSL)
			sampler2D _GrabTexture;
			#else
			sampler2D _GrabTexture : register(s0);
			#endif
			sampler2D _PalTex;
			// float _light = 0;
			sampler2D _MainTex;
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			sampler2D _LevelTex;
			sampler2D _WindTexRendered;
			float2 _WindTexRendered_TexelSize;
			float4 _tileCorrection;
			float2 _LevelTex_TexelSize;
			sampler2D _UniNoise;

			// float4 _lightDirAndPixelSize;
			float2 _screenSize;
			float4 _spriteRect;
			uniform float _waterDepth;
			float _waterTime;
			float _windStrength;
			float _snowStrength;

			// float4 _EffectColor;
			// float _WetTerrain;
			// float _waterLevel;
			float _RAIN;
			// float _cloudsSpeed;
			// float _fogAmount;
			sampler2D _NoiseTex;
			sampler2D _NoiseTex2;
			
			v2g vert (appdata_full v)
			{
				v2g o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.clr = v.color;
				o.scrPos = ComputeScreenPos(o.pos);
				float2 textCoord = float2(floor(o.scrPos.x*_screenSize.x)/_screenSize.x, floor(o.scrPos.y*_screenSize.y)/_screenSize.y);

				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;

				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;

				half2 mapCoord = textCoord*float2(_tileCorrection.x,_tileCorrection.y)+float2(_tileCorrection.z,_tileCorrection.w);
				mapCoord-=float2(0,o.uv.y*.01);
				#if HR 
				o.MagmaCoord[0] = float2((textCoord.x+_tileCorrection.w*6)*4,o.uv.y)+_tileCorrection.xy+float2(0,_RAIN*.2)+float2(.333,.777);
				o.MagmaCoord[1] = float2((textCoord.x+_tileCorrection.w*6)*4,o.uv.y)+_tileCorrection.xy-float2(0,_RAIN*.2)+float2(.333,.666);
				o.MagmaCoord[2] = float2((textCoord.x+_tileCorrection.w*6)*4,o.uv.y)+_tileCorrection.xy+float2(_RAIN*.2,0)+float2(.2,.4);
				o.MagmaCoord[3] = float2((textCoord.x+_tileCorrection.w*6)*4,o.uv.y)+_tileCorrection.xy-float2(_RAIN*.2,0);
				o.MagmaCoord[4] = float2((textCoord.x+_tileCorrection.w*6)*8,o.uv.y)*0.5;
				o.windMask =( float2)0;
				#else
				o.MagmaCoord[0] = o.MagmaCoord[1] = o.MagmaCoord[2] = o.MagmaCoord[3] = o.MagmaCoord[4] = float2(0,0);
				float4 temp = tex2Dlod(_WindTexRendered,float4(mapCoord.x,mapCoord.y,0,0));
				temp+=tex2Dlod(_WindTexRendered,float4(mapCoord.x+_WindTexRendered_TexelSize.x,mapCoord.y,0,0));
				temp+=tex2Dlod(_WindTexRendered,float4(mapCoord.x-_WindTexRendered_TexelSize.x,mapCoord.y,0,0));
				temp+=tex2Dlod(_WindTexRendered,float4(mapCoord.x,mapCoord.y+_WindTexRendered_TexelSize.y,0,0));
				temp+=tex2Dlod(_WindTexRendered,float4(mapCoord.x,mapCoord.y-_WindTexRendered_TexelSize.y,0,0));
				o.windMask = float2(temp.yz)*.2;
				#endif
				o.clones = 0;
				o.snowflakecoord=float2(o.scrPos.x*2,o.uv.y*.08);
				return o;
			}


			float ShCubicPulse( float position, float width, float value )
				{
					value = abs(value - position);
					if( value>width ) return 0.0;
					value /= width;
					return 1.0 - value*value*(3.0-2.0*value);
				}
				
			float makeSlush(float mult,float2 scrPos, float2 uv)
			{
				
				fixed wind = _waterTime*mult;
				fixed2 slushCoord = fixed2(scrPos.x*8+wind,uv.y*2);
				// fixed slush = tex2D(_NoiseTex,slushCoord);
				fixed slush2 = tex2D(_NoiseTex,slushCoord-wind);
				fixed slush3 = tex2D(_NoiseTex,slushCoord*.5-wind*.5);
				fixed slush = tex2D(_NoiseTex,slushCoord+slush2*.1);
				slush+=slush3;
				slush*=.5;
				return slush;
			}
			float pulse(float pos, float width, float x){x = smoothstep(1.-width,1.,1.-abs((x-pos)));return x;}

			fixed MagmaMask(float2 mc1,float2 mc2,float2 mc3,float2 mc4,float2 mc5)
			{
				fixed4 noise = fixed4(tex2D(_NoiseTex,mc1).x,tex2D(_NoiseTex,mc2).x,tex2D(_NoiseTex,mc3).x,tex2D(_NoiseTex,mc4).x);
				fixed noise2 = tex2D(_NoiseTex2,mc5);
				fixed mm = (noise.x-noise.y+noise.z-noise.w)*.5+.5;
				mm = 1.-(mm-pulse(.5,.05,mm+(noise2*2.-1.)*mm*.31)*1.);
				return mm;
			}

			half4 frag (v2g i) : SV_Target
			{
				// Cut out around air pockets
				for (int j = 0; j < MAX_AIR_POCKETS && _airPockets[j].z > _airPockets[j].x; j++) {
					float4 bounds = _airPockets[j];
					if (all(i.scrPos > bounds.xy) && all(i.scrPos < bounds.zw))
						discard;
				}

				#if HR
				
				float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;

				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;
				fixed magmaMask = MagmaMask(i.MagmaCoord[0],i.MagmaCoord[1],i.MagmaCoord[2],i.MagmaCoord[3],i.MagmaCoord[4]);
				half4 texcol = tex2D(_LevelTex, textCoord);

				if(texcol.x != 1.0 || texcol.y != 1.0 || texcol.z != 1.0)
					if(fmod(round(texcol.x * 255) - 1, 30.0)/30.0 < i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0))
						return float4(0, 0, 0, 0);
				//  if(fmod(round(texcol.x * 255) - 1, 30.0)<_waterDepth*31.0) return float4(0, 0, 0, 0);
				
				if (i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0) > 6.0/30.0){
					half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
					if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
						//setColor.w = 0;
						return float4(0, 0, 0, 0);
				}
				
				return (i.clr+fixed4(.5,0,0,0))*fixed4((fixed3)(magmaMask),1);

				#else


														// ================================================================	

			float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

			textCoord.x -= _spriteRect.x;
			textCoord.y -= _spriteRect.y;

			textCoord.x /= _spriteRect.z - _spriteRect.x;
			textCoord.y /= _spriteRect.w - _spriteRect.y;
		
			half4 texcol = tex2D(_LevelTex, textCoord);



			fixed windMap = i.windMask.y;
			fixed snowMap = i.windMask.x;
			
			float4 uniNoise = tex2D(_UniNoise,i.snowflakecoord);
			float noise = tex2D(_NoiseTex,trunc(i.snowflakecoord*64)*0.015625);
			float snow = smoothstep(.04,0,fmod(uniNoise.x+uniNoise.y+noise+_RAIN*.5,1));
			float snowmask =  ShCubicPulse((sin(_RAIN*.1)+3.4)*.1,smoothstep(0,.1,snowMap)*.5*_snowStrength,fmod(uniNoise.z+uniNoise.w+noise,1))*smoothstep(0,.1,snowMap);
			snow = snow*snowmask*((1-i.uv.y)*.5+.5);

			fixed slush = makeSlush(.4,textCoord,i.uv);
			fixed slush2 = makeSlush(2,textCoord,i.uv);
			slush = lerp(slush,slush2,windMap);

			float _input = _windStrength*.3+windMap*.2*_windStrength;
			// _input*=i.clones;
			slush = (fixed)((int)(smoothstep(_input+.3,_input,slush)*5)*.25);
			//int red = round(texcol.x * 255);
			
			// if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0)
			//red = 30;
			
			// red = fmod(red - 1, 30.0);
			
			
			//half4 setColor = lerp(tex2D(_PalTex, float2(7.5/32.0, 7.5/8.0)), tex2D(_PalTex, float2(8.5/32.0, 7.5/8.0)), i.uv.y);
			//setColor = lerp(setColor, tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)),i.uv.y*_fogAmount);
			
			//setColor = i.clr;
			if(texcol.x != 1.0 || texcol.y != 1.0 || texcol.z != 1.0)
			if((fmod(round(texcol.x * 255) - 1, 30.0)/30.0)+slush*.1 < i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0)) return float4(0, 0, 0, 0);
			//  if(fmod(round(texcol.x * 255) - 1, 30.0)<_waterDepth*31.0) return float4(0, 0, 0, 0);
			if (i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth*31.0)-slush*.1 > 6.0/30.0){
			half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
			//setColor.w = 0;
			return float4(0, 0, 0, 0);
			}
				// return (float4)i.clones+float4(0,0,0,1);
				// return float4(snow,snow,snow,1);
				
				float r = lerp(i.clr.r+max(slush,snow*.7)*.5, tex2D(_PalTex, float2(7.5/32.0, 7.5/8.0)).r, i.uv);
				float g = lerp(i.clr.g+max(slush,snow*.7)*.5, tex2D(_PalTex, float2(7.5/32.0, 7.5/8.0)).g, i.uv);
				float b = lerp(i.clr.b+max(slush,snow*.7)*.5, tex2D(_PalTex, float2(7.5/32.0, 7.5/8.0)).b, i.uv);
				float a = lerp(i.clr.a+max(slush,snow*.7)*.5, tex2D(_PalTex, float2(7.5/32.0, 7.5/8.0)).a, i.uv);
				
				return half4(r, g, b, a);
				#endif
			}
			ENDCG

		}
	}
}
