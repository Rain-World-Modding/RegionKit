// Original by Cactus based on DisplaySnowShader
// Recreated by Alduris 

Shader "Futile/RGBDisplaySnowShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        ZWrite Off
        //AlphaTest Greater 0.8
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
        
            AlphaTest Greater 0.8
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag        
            #pragma profileoption NumInstructionSlots=4096
            #pragma profileoption NumMathInstructionSlots=4096
            #pragma multi_compile __ HR
            #pragma exclude_renderers OpenGL
            #include "UnityCG.cginc"
            #include "_ShaderFix.cginc"
            #include "_Functions.cginc"
            #include "_RippleClip.cginc"
            #include "_Snow.cginc"
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
            float _light = 0;
            sampler2D _MainTex;
            float2 _MainTex_TexelSize;
            float4 _MainTex_ST;
            sampler2D _LevelTex;
            float2 _LevelTex_TexelSize;
            float4 _lightDirAndPixelSize;
            float2 _screenSize;
            float4 _spriteRect;
            float4 _EffectColor;
            float _WetTerrain;
            float _waterLevel;
            float _RAIN;
            float _cloudsSpeed;
            float _fogAmount;
            sampler2D _NoiseTex;
            sampler2D _NoiseTex2;
            sampler2D _SnowSources;
            float2 _SnowSources_TexelSize;

			uniform float4 _InputColorDispSnow; // normal snow color
			uniform float _InputRGBSnowAmount; // I have no clue what this was originally supposed to be but however it worked previously I'm changing it because the old way sucks
			uniform float4 _InputEndColorDispSnow; // end snow color
			uniform float _HSLDispSnowEndLerp; // lerp amount

            
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.clr = v.color;
                o.scrPos = ComputeScreenPos(o.pos);
                return o;
            }
                
            float GetDepth (float a)
            {
                if (a==1.0) return 255;
                a=round(a*255);
                float shadows = (step(a,90)*-1+1)*90;
                return fmod(a-shadows-1, 30);
            }

            float GetShadows (float a)
            {
                a*=255;
                return (step(round(a),90)*-1+1);
            }
            float cubicPulse( float c, float w, float x )
            {
                x = abs(x - c);
                if( x>w ) return 0.0;
                x /= w;
                return 1.0 - x*x*(3.0-2.0*x);
            }
            float easeInExpo(float t) {
                return (t == 0.0) ? 0.0 : pow(2.0, 10.0 * (t - 1.0));
            }
            float easeOutExpo(float t) {
            return (t == 1.0) ? 1.0 : -pow(2.0, -10.0 * t) + 1.0;
            }
            float easeOutCubic(float t) {
                return (t = t - 1.0) * t * t + 1.0;
            }
            float impulse( float k, float x )
            {
                const float h = k*x;
                return h*exp(1.0-h);
            }
            float easeOutQuad(float t) {
                return -1.0 * t * (t - 2.0);
            }
            float easeInQuad(float t) {
                return t * t;
            }
            float easeInOutExpo(float t) {
                if (t == 0.0 || t == 1.0) {
                    return t;
                }
                if ((t *= 2.0) < 1.0) {
                    return 0.5 * pow(2.0, 10.0 * (t - 1.0));
                } else {
                    return 0.5 * (-pow(2.0, -10.0 * (t - 1.0)) + 2.0);
                }
            }
            float4 frag(v2f i) : SV_Target
            {
                
                ///////////////////////////////////////////////////////
                // sample the texture
                float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
                textCoord.x -= _spriteRect.x;
                textCoord.y -= _spriteRect.y;

                textCoord.x /= _spriteRect.z - _spriteRect.x;
                textCoord.y /= _spriteRect.w - _spriteRect.y;
#if RIPPLE
                fixed rippleMask = tex2D(_GameplayRippleMask,i.scrPos).x;
                textCoord += rippleDistortion(rippleMask, i.scrPos)*1;
#endif
                float2 distUV=float2(textCoord.x,textCoord.y);
                float4 texCol = tex2D(_SnowTex,distUV);
                float depth = GetDepth(texCol.x);
                float4 grabColor = tex2D(_GrabTexture, float2(i.scrPos.x, i.scrPos.y));
                clip(grabColor.a - 0.8);
                if( (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)&&depth>5.0) 
                return float4(0,0,0,0);
                float shadows = GetShadows(texCol.x);
                float shadowGradient = (texCol.x-(depth+1)/255-(shadows/255*90))*4.25;
                // float shadowGradient = texCol.x-(shadows/255*90);
                float4 fog = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));
                // float4 snow = (float4)(depth/30+shadows*.4);


                //////////////RAINWORLD SHADOWS
                float shadow = tex2D(_NoiseTex, float2((textCoord.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*(clamp(depth,0,30))), 1-(textCoord.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*(clamp(depth,0,30))))).x;
    
                shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-textCoord.y, 1)*3.14*2)*0.5;
                shadow = clamp(((shadow - 0.5)*6)+0.5-(_light*4), 0,1);

                float4 shadowFix =FixEdgeShadowStretch(i.scrPos,true);
                float2 grabPos =  float2(i.scrPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(depth-5)*shadowFix.x,
                                            i.scrPos.y +  _lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(depth-5)*shadowFix.y);
                grabPos = lerp(grabPos, ((grabPos-float2(0.5, 0.3))*(1 + (depth-5.0)/460.0))+float2(0.5, 0.3), shadowFix.zw);
                float4 grabColor2 = tex2D(_GrabTexture,grabPos);
                grabColor2 = -step(grabColor2,0.003921568627451)+1;
                if (depth < 6) {
                    grabColor2 = 0;
                }

				// figure out hsl snow color. we use hsl lerp instead of rgb lerp like the original used
				fixed4 hslSnow = fixed4(rgb_hsl_lerp(_InputColorDispSnow.xyz, _InputEndColorDispSnow.xyz, _HSLDispSnowEndLerp).xyz,1);

                //////////////CONTINUE
                #if HR
                    float4 snow = tex2D(_PalTex,float2(depth*0.03333*0.6375,0.125+(shadowGradient*0.0625)));
                    float4 snowLight = tex2D(_PalTex,float2(depth*0.03333*0.6375,0.57+(shadowGradient*0.0625)));
                    snow = lerp(snow,snowLight,(-shadow+1)*shadows*(-grabColor2+1));
                    snow = .02+snow-shadowGradient*.01;
				    snow = lerp(snow, hslSnow, _InputRGBSnowAmount); // misx hsl snow
                    snow = lerp(snow,snow+fog*.2,_fogAmount*(depth/30));

                    float4 result = float4(snow.xyz,texCol.y);
                    smoothRippleClip(result, i.scrPos);
                    return result;
                #else
                    float4 snow = tex2D(_PalTex,float2(depth*0.03333*0.9375,0.125+(shadowGradient*0.0625)));
                    float4 snowLight = tex2D(_PalTex,float2(depth*0.03333*0.9375,0.57+(shadowGradient*0.0625)));

                    snow = lerp(snow,snowLight,(-shadow+1)*shadows*(-grabColor2+1));
                    snow+=.2+shadowGradient*.1;
				    snow = lerp(snow, hslSnow, _InputRGBSnowAmount); // mix hsl snow
                    snow = lerp(snow,fog,_fogAmount*(depth/30));

                    float4 result = float4(snow.xyz,texCol.y);
                    smoothRippleClip(result, i.scrPos);
                    return result;
                #endif
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}
