Shader "Shaders/ColoredOESphereBase"
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
		Blend One OneMinusSrcAlpha 
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
			sampler2D _pAngle;
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


				
				///////////////////////////////////////////////////////
				// sample the texture
				float orbI = i.clr.z;
				float flickerI = 1;
				float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
				textCoord.x -= _spriteRect.x;
				textCoord.y -= _spriteRect.y;

				textCoord.x /= _spriteRect.z - _spriteRect.x;
				textCoord.y /= _spriteRect.w - _spriteRect.y;

				//fixed4 color1 = fixed4(1, .52, .25, 1);
				//fixed4 color2 = fixed4(.5, 0, 1, 1);
				fixed4 color1 = fixed4(hsv2rgb(i.clr.x, .75, 1), 1);
				fixed4 color2 = fixed4(hsv2rgb(i.clr.y, 1, 1), 1);
				
				int depth = 30 * i.clr.w;
				if (depth > 5.0){
				half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				// return grabColor;
				if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
				return half4(0,0,0,0);
				}
				fixed depthMask = 1 - step(GetDepth(tex2D(_LevelTex, textCoord)), depth);
				float2 uv = i.uv;
				fixed pAngle2 = tex2D(_pAngle, float2(uv.x, uv.y * -1));
				fixed pAngle4 = tex2D(_pAngle, float2(uv.y, uv.x * -1));
				float time = _RAIN*.5;
				float ring1 = tex2D(_pAngle, float2(uv.x, uv.y)) - time;
				float ring2 = pAngle2 + .6 + time;
				float ring3 = tex2D(_pAngle, float2(uv.y, uv.x)) + .3 - time * .5;
				float ring4 = pAngle4 + .7 + time * .5;
				// float ring1 = atan2(uv.x,uv.y)*.1-_Time.x;
				// float ring2 = atan2(uv.x,uv.y*-1)*.1+.6+_Time.x;
				// float ring3 = atan2(uv.y,uv.x)*.1+.3-_Time.x*.5;
				// float ring4 = atan2(uv.y,uv.x*-1)*.1+.7+_Time.x*.5;
				// return fmod(ring1,1)+fixed4(0,0,0,1);
				fixed4 fog = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));
				float maskH = clamp(abs(pAngle2) * .3, 0, 1);
				float maskV = clamp(abs(pAngle4) * .3, 0, 1);
				uv = uv * 2 - 1;
				float fallOff = ShEaseInQuad(length(uv));
				float circleMask = smoothstep(1, .99, fallOff);
				fixed noise = tex2D(_NoiseTex, float2(ring1, fallOff));
				// noise = tex2D(_MainTex, float2(ring1+noise,fallOff-_Time.x));
				fixed noise2 = tex2D(_NoiseTex, float2(ring2, fallOff));
				// noise2 = tex2D(_MainTex, float2(ring2+noise2,fallOff-_Time.x));
				fixed noise3 = tex2D(_NoiseTex, float2(ring3, fallOff));
				// noise3 = tex2D(_MainTex, float2(ring3+noise3,fallOff+_Time.x));
				fixed noise4 = tex2D(_NoiseTex, float2(ring4, fallOff));
				// noise4 = tex2D(_MainTex, float2(ring4+noise4,fallOff+_Time.x));
				fixed vert = clamp(noise - abs(maskH - 1), 0, 1) + clamp(noise2 - maskH, 0, 1);
				fixed hor = clamp(noise3 - abs(maskV - 1), 0, 1) + clamp(noise4 - maskV, 0, 1);
				fixed innerVert = vert * fallOff * smoothstep(1, .7, fallOff) * circleMask;
				fixed innerHor = hor * fallOff * smoothstep(1, .7, fallOff) * circleMask;
				fixed noiseRing = innerVert + innerHor;
				fixed4 col = lerp(color1, color2, smoothstep(.6, 0, noiseRing)) * noiseRing;
				fixed4 col2 = clamp((fallOff * -1 + 1) * color1 * 2 - noiseRing, 0, 1);
				fixed4 combined = (col + col2);
				combined = fixed4(combined.x, combined.y, combined.z, 1);
				combined = lerp(combined,fog,_fogAmount*i.clr.w*.5);
				fixed4 result = combined * fixed4(1, 1, 1, circleMask * depthMask);
				return fixed4(result.xyz*result.w,result.w);
			}
			ENDCG
		}
	}
}
