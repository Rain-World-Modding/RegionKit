// From Alduris:
// From the looks of the decompiled code, it looks like this is just a modification of DeepWater
// The original source code for the shader was lost so this is an attempt to recreate it

Shader "Futile/WaterWarble"
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
            GrabPass { }
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

                sampler2D _NoiseTex;
                sampler2D _GrabTexture;
                
                uniform float _RAIN;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float2 textCoord : TEXCOORD2;
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
                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
                    textCoord = iLerp(_spriteRect.xy, _spriteRect.zw, textCoord);

                    half rbcol = (sin((_RAIN + (tex2D(_NoiseTex, float2(textCoord.x*1.2, textCoord.y*1.2) ).x * 3) + 0/12.0) * 3.14 * 2)*0.5)+0.5;

                    float2 distortion = float2(lerp(-0.002, 0.002, rbcol)*lerp(1, 20, pow(i.uv.y, 200)), -0.02 * pow(i.uv.y, 8));
                    distortion.x = floor(distortion.x*_screenSize.x)/_screenSize.x;
                    distortion.y = floor(distortion.y*_screenSize.y)/_screenSize.y;

                    return tex2D(_GrabTexture, i.scrPos + distortion);
                }
                ENDCG
            }
        } 
    }
}
