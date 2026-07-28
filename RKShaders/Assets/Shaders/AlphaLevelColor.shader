// From Alduris:
// This is modified from the 2015 Rain World alpha.
// I hope this is correct because the original source was lost and decompiling it is a fucking mess


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
textCoord = iLerp(_spriteRect.xy, _spriteRect.zw, textCoord); // r1.xy

half4 setColor = half4(0.0, 0.0, 0.0, 1.0);
bool checkMaskOut = false;

half2 screenPos = half2(lerp(_spriteRect.x, _spriteRect.z, i.uv.x), lerp(_spriteRect.y, _spriteRect.w, i.uv.y));

half4 texcol = tex2D(_MainTex, float2(i.uv.x, i.uv.y));
   
if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0){
    return half4(0,0,0,0);
} else {
    int red = texcol.x * 255;
    int green = texcol.y * 255;
   
    half shadow = tex2D(_NoiseTex, float2((i.uv.x*0.5) + (_RAIN*0.1) - (0.003*fmod(red - 1, 30.0)), 1-(i.uv.y*0.5) + (_RAIN*0.2) - (0.003*fmod(red - 1, 30.0)))).x;
 
    shadow = 0.5 + sin(fmod(shadow+(_RAIN*0.1)-i.uv.y, 1)*3.14*2)*0.5;
    shadow = clamp(((shadow - 0.5)*6)+0.5-(_light*4), 0,1);

    if (red > 90){
        red -= 90;
    } else {
        shadow = 1.0;
    }
    int paletteColor = floor(red/30.0);
    red = fmod(red - 1, 30.0);

    // addition: sample for depth
    if (red > tex2D(_LevelTex, textCoord).x * 255) {
        return float4(0,0,0,0);
    }

    if (shadow != 1 && red > 5) {
        float4 grabTexCol2 = tex2D(_PreLevelColorGrab, float2(screenPos.x + -_lightDirAndPixelSize.x*_lightDirAndPixelSize.z*(red-5)*1.2, 1-screenPos.y + -_lightDirAndPixelSize.y*_lightDirAndPixelSize.w*(red-5)*1.2));
        if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0){
            shadow = 1;
        }
    }
   
    setColor = lerp(tex2D(_PalTex, float2(red/32.0, (paletteColor + 3 + 0.5)/8.0)), tex2D(_PalTex, float2(red/32.0, (paletteColor + 0.5)/8.0)), shadow);


    half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(i.uv.x*2, i.uv.y*2) ).x * 4) + red/12.0) * 3.14 * 2)*0.5)+0.5;
    setColor = lerp(setColor, tex2D(_PalTex, float2((5.5 + rbcol*25)/32.0, 6.5 / 8.0) ), (green > 3 ? 0.1 : 0.0));
   
    if(green > 3){
        green -= 4;
    }
    
   
    if (green > 0 && green < 3) {
        setColor = lerp(setColor, lerp(lerp(tex2D(_PalTex, float2(30.5/32.0, (5.5-(green-1)*2)/8.0)), tex2D(_PalTex, float2(31.5/32.0, (5.5-(green-1)*2)/8.0)), shadow), lerp(tex2D(_PalTex, float2(30.5/32.0, (4.5-(green-1)*2)/8.0)), tex2D(_PalTex, float2(31.5/32.0, (4.5-(green-1)*2)/8.0)), shadow), red/30.0), texcol.z);
    } else if (green == 3) {
        setColor = lerp(setColor, half4(1, 1, 1, 1), texcol.z);
    }

    setColor = lerp(setColor, tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)), red*_fogAmount/30.0);

    if (red > 5){
        checkMaskOut = true; 	
    }
}
  
if (checkMaskOut){
    float4 grabTexCol = tex2D(_PreLevelColorGrab, float2(screenPos.x, 1-screenPos.y));
    if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0){
        setColor.w = 0;
    }
}

return setColor;


                }
                ENDCG
            }
        }
    }
}