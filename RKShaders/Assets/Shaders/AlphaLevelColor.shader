// From Alduris:
// This is modified from the actual level shader
// I hope this is close enough because the original source for AlphaLevelColor was lost and decompiling it is a fucking mess


Shader "Futile/AlphaLevelColor"
{
    Properties 
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    
    Category 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off  // we can turn backface culling off because we know nothing will be facing backwards

        SubShader
        {
            Pass 
            {
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"
                #include "_Functions.cginc"
                #include "_RippleClip.cginc"

                float4 _MainTex_ST;
                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _PalTex;
                sampler2D _NoiseTex;
                sampler2D _PreLevelColorGrab;

                //sampler2D _GrabTexture;
                
                uniform float _RAIN;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                uniform float _cloudsSpeed;
                uniform float _light;
                uniform float _fogAmount;
                uniform float _Grime;
                uniform float4 _lightDirAndPixelSize;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float4 clr : COLOR;
                };

                
                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                half4 frag (v2f i) : SV_Target 
                {
                
float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
textCoord = iLerp(_spriteRect.xy, _spriteRect.zw, textCoord);

half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
bool checkMaskOut = false;

half4 texcol = tex2D(_MainTex, float2(i.uv.x, i.uv.y));
   
int red = round(texcol.x * 255);
int green = round(texcol.y * 255);

float effectCol = 0;

float heatEffectCol = 0;
float sky = false;

if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0){ //sky stuff
	return float4(0,0,0,0);
} 
else 
{ 
	half notFloorDark = 1;
	if(green >= 16) {
		notFloorDark = 0;
		green -= 16;
	}
	if(green >= 8) {
		effectCol = 100;
		green -= 8;
	} else
		effectCol = green;

	half shadow = tex2D(_NoiseTex, float2((i.scrPos.x*0.5) + (_RAIN*0.1*_cloudsSpeed) - (0.003*fmod(red, 30.0)),
                                        1-(i.scrPos.y*0.5) + (_RAIN*0.2*_cloudsSpeed) - (0.003*fmod(red, 30.0))
                                        )).x;
	shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1*_cloudsSpeed)-i.uv.y, 1)*3.14*2)*0.5;
	shadow = saturate(((shadow - 0.5)*6)+0.5-(_light*4));

	if (red > 90)
		red -= 90;
	else
		shadow = 1.0;
   
	int paletteColor = clamp(floor((red-1)/30.0), 0, 2); //some distant objects want to get palette color 3, so we clamp it

    // take shadow from original level (we could try to reverse it properly or use lightmap system but this is how original RK AlphaLevelColor shader does it)
    float4 origLevelCol = tex2D(_LevelTex, textCoord);
    int origLevelDepth = round(origLevelCol.x * 255);
    if (origLevelDepth <= 90) {
        shadow = 1.0;
    }
    
	red = fmod(red-1, 30.0);//depth
    
    // this actually isn't in the original RK AlphaLevelColor shader I think but it is useful anyways
	if (shadow != 1 && red >= 5) {//casting shadows from objects
        float4 shadowFix =FixEdgeShadowStretch(textCoord, false);
		half2 grabPos = float2(i.scrPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(red-5)*shadowFix.x,
                               i.scrPos.y +  _lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(red-5)*shadowFix.y);
		grabPos = lerp(grabPos,((grabPos-half2(0.5, 0.3))*(1 + (red-5.0)/460.0))+half2(0.5, 0.3),shadowFix.zw);
		float4 grabTexCol2 = tex2D(_PreLevelColorGrab, grabPos);
 		if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0) {
     		shadow = 1;
  		}
	}

    half4 fogCol = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0));
	half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(i.uv.x*2, i.uv.y*2) ).x * 4) + red/12.0) * 3.14 * 2)*0.5)+0.5;

	setColor = lerp(tex2D(_PalTex, float2((red*notFloorDark)/32.0, (paletteColor + 3 + 0.5)/8.0)),
                    tex2D(_PalTex, float2((red*notFloorDark)/32.0, (paletteColor + 0.5)/8.0)),
                    shadow);

	setColor = lerp(setColor, tex2D(_PalTex, float2((5.5 + rbcol*25)/32.0, 6.5 / 8.0) ), (green >= 4 ? 0.2 : 0.0) * _Grime);
   
    // no decal color for you!
	/*if (effectCol == 100) { //colored props
		half4 decalCol = tex2D(_MainTex, float2((255.5-round(texcol.z*255.0))/1400.0, 799.5/800.0));
		if(paletteColor == 2) decalCol = lerp(decalCol, half4(1, 1, 1, 1), 0.2 - shadow*0.1);
		decalCol = lerp(decalCol, fogCol, red/60.0);
		setColor = lerp(lerp(setColor, decalCol, 0.7), setColor*decalCol*1.5,  lerp(0.9, 0.3+0.4*shadow, saturate((red-3.5)*0.3) ) );
	}
	else*/ if (green > 0 && green < 3) {//effect colors
		setColor = lerp(setColor,
                        lerp(lerp(tex2D(_PalTex, float2(30.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                                  tex2D(_PalTex, float2(31.5/32.0, (5.5-(effectCol-1)*2)/8.0)),
                                  shadow),
                             lerp(tex2D(_PalTex, float2(30.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                                  tex2D(_PalTex, float2(31.5/32.0, (4.5-(effectCol-1)*2)/8.0)),
                                  shadow),
                             red/30.0),
                        texcol.z);

	} else if (green == 3) {//batfly nests
		setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z);
	}
   
	setColor = lerp(setColor, fogCol, saturate(red*(red < 10 ? lerp(notFloorDark, 1, 0.5) : 1)*_fogAmount/30.0));

	if (red >= 5) {
		checkMaskOut = true; 	
	}
}
  
if (checkMaskOut) {
	float4 grabTexCol = tex2D(_PreLevelColorGrab, float2(i.scrPos.x, i.scrPos.y));
 	if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0){
        setColor.w = 0;
   	}
}

smoothRippleClip(setColor,i.scrPos);
return setColor;


                }
                ENDCG
            }
        }
    }
}