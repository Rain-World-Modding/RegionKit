Shader "RegionKit/RKSmoothRippleMask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
        Tags { "Queue" = "Geometry"}
		Blend SrcAlpha OneMinusSrcAlpha 
		ZWrite Off
		//Alphatest Greater 0
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
                float2 nuv : TEXCOORD2;
                float2 nuv2 : TEXCOORD3;
                float2 nuv3 : TEXCOORD4;
				float4 clr : COLOR;
                float2 texCoord : TEXCOORD5;
                fixed4 flags : TEXCOORD6;
			};
			float _RAIN;
			sampler2D _NoiseTex;
            uniform float2 _screenOffset;
			sampler2D _RippleLevelTex;
			sampler2D _LevelTex;
            sampler2D _RippleMaskSaved;
			float2 _MainTex_TexelSize;
			float4 _MainTex_ST;
			float2 _screenSize;
			float4 _spriteRect;

			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex)*2-1;
                o.scrPos = ComputeScreenPos(o.pos);
                float2 offset =_spriteRect.xy*fixed2(_screenSize.x/_screenSize.y,1); 
                float2 cuv = o.scrPos*fixed2(_screenSize.x/_screenSize.y,1);
                cuv-=offset;
				o.nuv =cuv*5+float2(0,_RAIN*2);
				o.nuv2 = cuv+float2(sin(_RAIN),cos(_RAIN))*.5;
				o.nuv3 = cuv*.5+float2(0,_RAIN*-0.25);
				o.clr = v.color;
                o.texCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos.xy);
                int a = o.clr.y*255;
                o.flags = fixed4((a&1)>0,(a&2)>0,(a&4)>0,0);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
            {
                fixed shape = i.flags.y;
                fixed intensity = i.clr.x;
                fixed inverted = i.flags.x;
                fixed fallOff = i.clr.z*.99;
                float len = length(i.uv);
                fixed tDepth = get_depth01(tex2D(_RippleLevelTex,i.texCoord).x);
                fixed depth_intensity = abs(i.clr.w*2-1);
                fixed depth_offset =lerp(tDepth,1-tDepth,step(i.clr.w,.5))*(depth_intensity); 
                fixed circle =length(i.uv)+depth_offset; 
                circle = iLerp(.99-fallOff,1,circle);
                fixed2 p = smoothstep(1-fallOff,1,abs(i.uv));
                fixed square = lerp(max(p.x,p.y),length(p),(p.x*p.y)>0)+depth_offset;
                fixed dist = 1-(shape<.5 ? circle : square);
                fixed result = dist;
                result*=intensity;
                inverted = 1-inverted;
                return fixed4(inverted,inverted*i.flags.z,inverted*i.flags.z*.5,result);
			}
			ENDCG
		}
	}
}
