Shader "Futile/MurkyWaterLightSource"
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
				#pragma target 4.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_local _ bright deepLight
				#include "UnityCG.cginc"
				#include "_ShaderFix.cginc"
				#include "_Functions.cginc"
				#include "_TerrainMask.cginc"
				#include "_RippleClip.cginc"
				#include "_Snow.cginc"
				#include "_BrainMoldClip.cginc"

				sampler2D _MainTex;
				sampler2D _LevelTex;
				sampler2D _NoiseTex;
				sampler2D _UniNoise;
				sampler2D _NoiseTex2;
				uniform float _waterPosition;

				#if defined(SHADER_API_PSSL)
				sampler2D _GrabTexture;
				#else
				sampler2D _GrabTexture : register(s0);
				#endif

				uniform float _RAIN;

				uniform float4 _spriteRect;
				uniform float2 _screenSize;

				sampler2D _MurkyWaterMask;
				uniform float _MurkyWaterAmount;

				#if deepLight
				static const float fiveLayersOffset = 0.1666666667;
				#endif

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
					#if bright
						o.clr.w *= 4;
					#endif
					return o;
				}

				half4 frag (v2f i) : SV_Target
				{
					//return float4(tex2D(_MurkyWaterMask, i.scrPos).xyz, 1);

					_BrainMoldClip(i.scrPos);
	
					float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

					textCoord.x -= _spriteRect.x;
					textCoord.y -= _spriteRect.y;
					textCoord.x /= _spriteRect.z - _spriteRect.x;
					textCoord.y /= _spriteRect.w - _spriteRect.y;

					half4 texcol = tex2D(_LevelTex, textCoord);
					fixed sparkles = 0;
					#if SNOW_ON && !HR
						sparkles = texcol.x;
					#endif 
					texcol = AddSnow(texcol,textCoord,i.scrPos);

					int paletteColor = floor(((uint)round(texcol.x * 255) % 90)/30.0);
					if(texcol.y >= 16.0/255.0) paletteColor = 3;

					bool onTerrain = false;
					half dist = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
					#if deepLight
						dist =saturate(dist - i.clr.w + fiveLayersOffset); // offset back to 0th layer and add custom depth
					#endif
					if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) dist = 1.0;
					half4 terrainInfo = TerrainAtLevelPos(textCoord, _spriteRect);
					if (terrainInfo.x < dist)
					{
						dist = terrainInfo.x;
						paletteColor = 0;
						onTerrain = true;

						// Remove light from black goo portion
						if (terrainInfo.z > 0.5)
							discard;
					}
					#if SNOW_ON && !HR
						sparkles = texcol.x!=sparkles.x;
						sparkles = Sparkles(dist,i.uv*2-1,textCoord,sparkles*texcol, _UniNoise, _RAIN);
					#endif


					half2 dir = normalize(i.uv.xy - half2(0.5, 0.5)); 

					half centerDist = clamp(distance(i.uv.xy, half2(0.5, 0.5)), 0, 0.5);
					half2 shadowPos = textCoord - (dir * pow(centerDist, 1.25) * pow(dist, 2) * 0.3);

					half2 highLightPos = textCoord - (dir * 0.003 * pow(centerDist*2, 0.25));

					half2 oldShadowPos = i.scrPos.xy - (dir * pow(centerDist, 1.25) * pow(dist, 1.5) * 0.3);


					texcol = tex2D(_LevelTex, shadowPos);
					texcol = AddSnow(texcol, shadowPos,i.scrPos);
					half shadowDist = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
					if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) shadowDist = 1.0;
					shadowDist = min(shadowDist, TerrainAtLevelPos(shadowPos, _spriteRect).x);

					texcol = tex2D(_LevelTex, highLightPos);
					texcol = AddSnow(texcol,highLightPos,i.scrPos);
					half highLightDist = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
					#if deepLight
						highLightDist =saturate(highLightDist - i.clr.w + fiveLayersOffset);
					#endif

					if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) highLightDist = 1.0;
					highLightDist = min(highLightDist, TerrainAtLevelPos(highLightPos, _spriteRect).x);


					// creature mask VVVVVV
					float tresh = 5.0;
					#if deepLight
						tresh +=5-i.clr.w*30;
					#endif
					if (dist > tresh/30.0){
						half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
						if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
							return half4(0,0,0,0);
					}
					// Creature mask ^^^^^^

					if (shadowDist > 5.0/30.0){
						half4 grabColor = tex2D(_GrabTexture, oldShadowPos);
						if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
							shadowDist = 6.0/30.0;
					}

					#if deepLight
						shadowDist =saturate(shadowDist - i.clr.w + fiveLayersOffset);
						if (shadowDist <=0.)
							shadowDist = 3;//prevents casting shadows from layers closer to camera
					#endif

					float shadow = dist - shadowDist - (paletteColor == 1 ? 0 : 2.0/30.0);
					#if deepLight // make shodows more intense the deeper you go
						shadow = pow(clamp(shadow, 0, 1), lerp(saturate(1.0-dist*(1+i.clr.w*2.0)), 0.5, 0.5));
					#else
						shadow = pow(clamp(shadow, 0, 1), lerp(1.0-dist, 0.5, 0.5));
					#endif


					float highLight = 0;
					if(highLightDist > dist + 0.05) highLight = sin(centerDist*3.14*2);

					if (paletteColor == 0) {
						half2 sd2Pos = textCoord - (dir * 0.01 * centerDist);
						texcol = tex2D(_LevelTex, sd2Pos);
						texcol = AddSnow(texcol,sd2Pos,i.scrPos);
						float sd2 = fmod(round(texcol.x * 255)-1, 30.0)/30.0;
						sd2 = min(sd2, TerrainAtLevelPos(sd2Pos, _spriteRect).x + 1.0 / 30.0);
						if(sd2 < dist && sd2 > dist - 0.1 && !onTerrain) shadow = lerp(shadow, 1, pow(centerDist*2.0, 2.5-4.0*centerDist));
					}

					half d = dist;

					if(dist < 0.2 && onTerrain) dist = 1.0 - dist / 0.2;
					else if(dist < 0.2) dist = pow(1.0-(dist * 5.0), 0.35);
					else dist = clamp((dist - 0.2) * 1.3, 0, 1);

					dist = 1.0-dist;
					dist *= pow(pow((1-pow(centerDist * 2, 2)), 3.5), lerp(0.5, 3.5, d));

					dist = clamp(lerp(dist, 0, shadow)-shadow*0.3, 0, 1);
					if(paletteColor == 0) dist *= 0.8;
					else if (paletteColor == 2) dist = pow(dist, 0.8);
					else if(paletteColor == 3){
						dist *= 0.2;
						highLight = 0;
					}

					// Slightly dim light cast on terrain
					if (onTerrain) dist *= 0.8;

					// Dim light for murky water
					dist *= lerp(1.0, 1.0 - tex2D(_MurkyWaterMask, i.scrPos).x, _MurkyWaterAmount);

					dist *= tex2D(_MainTex, i.uv.xy).x;
					#if deepLight
						i.clr.w = i.clr.z;
						i.clr.xyz = hsv2rgb(i.clr.xyz);
					#endif
					#if RIPPLE
						fixed rippleMask = allRippleColorMask(i.scrPos);
						i.clr.xyz = lerp(i.clr.xyz,_RippleGold.xyz,allRippleColorMask(i.scrPos)).xyz;
						i.clr.w *= 1 - rippleMask*.6; 
						sparkles *= 1 - rippleMask;
					#endif
					#if SNOW_ON
						return half4(i.clr.xyz+i.clr.xyz*sparkles*4*highLight,dist*sparkles*10 *i.clr.w*highLight +dist * i.clr.w * (0.65 + highLight * 0.35));
					#else
						return half4(i.clr.xyz, dist * i.clr.w * (0.65 + highLight * 0.35));
					#endif
				}
				ENDCG
			}
		} 
	}
}
