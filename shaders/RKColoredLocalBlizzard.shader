Shader "RegionKit/RKColoredLocalBlizzard"
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
            #include "_RippleClip.cginc"
            #include "_TerrainMask.cginc"
            #include "_Snow.cginc"


			sampler2D _GrabTexture;
			sampler2D _PalTex;
			sampler2D _MainTex;
			sampler2D _WindTex;

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

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
                float2 scrPos : TEXCOORD1;
				float2 textCoord : TEXCOORD2;
				float4 snowColor : TEXCOORD3;
				float4 clr : COLOR;
			};

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.scrPos = ComputeScreenPos(o.pos);
                o.textCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos);
                o.clr = v.color;
				o.snowColor = float4(v.texcoord1.xy, v.texcoord2.xy);
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

			fixed4 frag (v2f i) : SV_Target
			{
				fixed dist = lerp(1.,0.,length(i.uv*2.-1.));
				
				half _SNOW = _RAIN;
				half2 mapCoord = i.textCoord*float2(_tileCorrection.x,_tileCorrection.y)+float2(_tileCorrection.z,_tileCorrection.w);
				half scale =i.clr.y;
				half snow = 0;
				half whiteout = (1+i.clr.x);
                fixed4 levelCol = tex2D(_LevelTex,i.textCoord);
                levelCol = AddTerrain(levelCol, i.textCoord, _spriteRect);
                levelCol = AddSnow(levelCol,i.textCoord,i.scrPos);
				half depth = GetDepth(levelCol.x)*0.0333333333333333;	
				depth = clamp(depth,0,1);
				half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, i.scrPos.y));
				if( (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)&&depth>0.1666666666666667) 
				depth = 0.1666666666666667 ;
				half windmap = 1;
				_waterLevel+=.18-clamp((1-smoothstep(0,.3,windmap)),0,1)*.015;
				float watermask = clamp(1-smoothstep(_waterLevel-.14,_waterLevel,1-i.scrPos.y),0,1);
				half snInt = clamp(i.clr.x*smoothstep(0,.6,windmap)+clamp(1-step(depth,.99),0,1)*smoothstep(.6,0,windmap)*i.clr.x*.7,0,1);
				snInt*=watermask*dist;
                snInt = saturate(snInt);
				
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
                fixed4 result = (fixed4)(clamp(lerp(0,snow*snInt*.5,ShGain(depth,1-snInt*.9)*ShGain(depth,1-snInt*.8)+depth*snInt*4),0,1)*.9)*fog*whiteout*fixed4(i.snowColor.xyz,snow);

                smoothRippleClip(result,i.scrPos);
				return result;


				return float4(1,1,1,1);
			}
			ENDCG
		}
	}
}
