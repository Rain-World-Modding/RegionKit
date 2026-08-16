Shader "Shaders/ColoredOESphereLight"
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
		// Blend SrcAlpha OneMinusSrcAlpha 
		Blend One One 
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
			#include "_Functions.cginc"


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
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			sampler2D _LevelTex;
			float2 _LevelTex_TexelSize;
			// float4 _lightDirAndPixelSize;
			float2 _screenSize;
			float4 _spriteRect;
			// float4 _EffectColor;
			// float _WetTerrain;
			// float _waterLevel;
			float _RAIN;
			// float _cloudsSpeed;
			float _fogAmount;
			sampler2D _NoiseTex;
			sampler2D _NoiseTex2;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.clr = v.color;
				o.scrPos = ComputeScreenPos(o.pos);
				return o;
			}
			float ShEaseInQuad(float t) {
				return t * t;
			}
			float GetDepth(float a) {		
				if (a == 1.0) return 255;
				a = round(a * 255);
				float shadows = (step(a, 90) * -1 + 1) * 90;
				return fmod(a - shadows - 1, 30);
			}
			fixed4 frag (v2f i) : SV_Target
			{
				float falloff = smoothstep(.2,.1,distance(i.uv,.5));
				float falloff2 = smoothstep(.2,0,distance(i.uv,.5));
				falloff = ShEaseInQuad(falloff);
				// fixed4 spherecolor1 = fixed4(1, .52, .25, 1);
				fixed4 spherecolor1 = fixed4(hsv2rgb(i.clr.x, 1, 1), 1);
				// fixed4 spherecolor2 = fixed4(.5, 0, 1, 1);
				fixed4 spherecolor2 = fixed4(hsv2rgb(i.clr.y, .77, 1), 1);
				int depth = (30 * i.clr.w)-5;
				float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);

				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;
				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;

				half4 texcol = tex2D(_LevelTex, textCoord);
				
				int paletteColor = floor(((uint)round(texcol.x * 255) % 90 - depth )/30.0);
				if(texcol.y >= 16.0/255.0) paletteColor = 3;

				half dist = (fmod(round(texcol.x * 255)-1, 30.0)-depth)/30.0;
				if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) dist = 1.0;


				half2 dir = normalize(i.uv.xy - half2(0.5, 0.5)); 
				float flicker = (tex2D(_NoiseTex,float2(textCoord.x+GetDepth(texcol.x)*.2,textCoord.y+_RAIN*2)+dir)+tex2D(_NoiseTex,float2(textCoord.x+GetDepth(texcol.x)*.1,textCoord.y-_RAIN*2)+(dir)*.1))*.5;
				float flicker2 = (tex2D(_NoiseTex,float2(textCoord.x+GetDepth(texcol.x)*.3,textCoord.y-_RAIN*1.5)+dir)+tex2D(_NoiseTex,float2(textCoord.x+GetDepth(texcol.x)*.2,textCoord.y+_RAIN*1.5)+(dir)*.1))*.5;
				// return smoothstep(falloff*.3,1,flicker)+fixed4(0,0,0,1);

				half centerDist = clamp(distance(i.uv.xy, half2(0.5, 0.5)), 0, 0.5);
				half2 shadowPos = textCoord - (dir * pow(centerDist, 1.25) * pow(dist, 2) * 0.3);

				//half2 highLightPos = textCoord - (dir * lerp(0.002, 0.01, abs((6.0/30.0)-dist)) * pow(sin(centerDist*3.14*2), 0.2));
				// half2 highLightPos = textCoord - (dir * 0.006 * pow(centerDist*2, 0.25));        // original one <<<<<<<<<<<<<<<<<<<<<<<
				
				half2 highLightPos = textCoord - (dir * 0.006)*(1-distance(i.uv.xy, half2(0.5, 0.5)));  
				// highLightPos = textCoord - (dir * 0.006 * pow(centerDist*2, 0.25));

				half2 oldShadowPos = i.scrPos.xy - (dir * pow(centerDist, 1.25) * pow(dist, 1.5) * 0.3);
				oldShadowPos.y = 1-oldShadowPos.y;



				texcol = tex2D(_LevelTex, shadowPos);
				half shadowDist = (fmod(round(texcol.x * 255)-1, 30.0)- depth)/30.0;
				if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) shadowDist = 1.0;

				texcol = tex2D(_LevelTex, highLightPos);
				half highLightDist = (fmod(round(texcol.x * 255)-1, 30.0)- depth)/30.0;
				if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) highLightDist = 1.0;


				// creature mask VVVVVV

				half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
				return half4(0,0,0,0);

				// Creature mask ^^^^^^

				if (shadowDist > (5.0+ depth )/30.0){
				half4 grabColor = tex2D(_GrabTexture, oldShadowPos);
				if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
				shadowDist = (6.0+ depth )/30.0;
				}


				float shadow = dist - shadowDist - (paletteColor == 1 ? 0 : 2.0/30.0);
				shadow = pow(clamp(shadow, 0, 1), lerp(1.0-dist, 0.5, 0.5));


				float highLight = 0;
				if(highLightDist > dist + 0.05) highLight = 3.14*2;
				// return highLight;


				if(paletteColor == 0){
				half2 sd2Pos = textCoord - (dir * 0.01 * centerDist);
				float sd2 =( fmod(round(tex2D(_LevelTex, sd2Pos).x * 255)-1, 30.0)- depth)/30.0;
				if(sd2 < dist && sd2 > dist - 0.1) shadow = lerp(shadow, 1, pow(centerDist*2.0, 2.5-4.0*centerDist));
				}

				half d = dist;

				if(dist < 0.2) dist = pow(1.0-(dist * 5.0), 0.35);
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

				dist *= tex2D(_MainTex, i.uv.xy).x;
				// return falloff;
				fixed4 fog = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));
				// return fixed4(lerp(spherecolor1.xyz, spherecolor2.xyz, abs(dist-flicker2)),1);
				fixed4 light = half4(lerp(spherecolor1.xyz, spherecolor2.xyz, abs(dist-flicker2)), dist * 2 * 1 * (0.65 + highLight * 5 * 0.35));
				light = lerp(light,fog,_fogAmount*i.clr.w*.5);
				return half4(light.xyz, 1)*(dist * i.clr.w * 5 * (0.65 + highLight* (1+1*falloff)*smoothstep(0,clamp(1-falloff,0,1),flicker) * 0.35))*(1-i.clr.w)*i.clr.z;
				// return half4(light.xyz, (dist * i.clr.w * 5 * (0.65 + highLight* (5+5*falloff)*smoothstep(0,clamp(1-falloff,0,1),flicker) * 0.35))*(1-i.clr.w)*i.clr.z);
			}
			ENDCG
		}
	}
}
